using System.Data;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Dapper;
using HotelApp.Web.Models;
using Microsoft.Data.SqlClient;

namespace HotelApp.Web.Services
{
    /// <summary>
    /// Wrapper service for the NIC Invoice Registration Portal (IRP) API.
    /// Handles authentication, token caching, and IRN generation.
    ///
    /// Real-portal integration is wired end-to-end; the sandbox URLs from
    /// HotelSettings are used.  When the production GSTN portal credentials
    /// are available simply update HotelSettings → the code requires no changes.
    /// </summary>
    public class IrpApiService : IIrpApiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IEInvoiceProtector _protector;
        private readonly ILogger<IrpApiService> _logger;

        public IrpApiService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IEInvoiceProtector protector,
            ILogger<IrpApiService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _protector = protector;
            _logger = logger;
        }

        // ── Token management ──────────────────────────────────────────────────

        public async Task<string?> GetValidTokenAsync(HotelSettings settings, int branchId, int? userId)
        {
            // 1. Check DB cache for a non-expired token for this branch
            using var conn = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection")!);

            const string selectSql = @"
                SELECT TOP 1 AccessToken, ExpiresAt
                FROM dbo.EInvoiceIrpTokens
                WHERE BranchID = @BranchID
                  AND ExpiresAt > SYSUTCDATETIME()
                ORDER BY CreatedAt DESC;";

            var cached = await conn.QueryFirstOrDefaultAsync(selectSql, new { BranchID = branchId });

            if (cached != null)
            {
                _logger.LogInformation("IRP: Using cached token for branch {BranchID}", branchId);
                return (string)cached.AccessToken;
            }

            // 2. Re-authenticate with IRP
            return await AuthenticateAndStoreAsync(settings, branchId, userId, conn);
        }

        private async Task<string?> AuthenticateAndStoreAsync(
            HotelSettings settings,
            int branchId,
            int? userId,
            SqlConnection conn)
        {
            if (string.IsNullOrWhiteSpace(settings.EInvoiceAuthUrl))
            {
                _logger.LogWarning("IRP: EInvoiceAuthUrl is not configured for branch {BranchID}", branchId);
                return null;
            }

            try
            {
                var plainSecret  = DecryptIfNeeded(settings.EInvoiceClientSecret);
                var plainPassword = DecryptIfNeeded(settings.EInvoicePassword);

                var client = _httpClientFactory.CreateClient("IrpClient");

                var requestBody = new
                {
                    username = settings.EInvoiceUsername,
                    password = plainPassword
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                // IRP auth endpoint requires client credentials in headers
                client.DefaultRequestHeaders.TryAddWithoutValidation("client_id",     settings.EInvoiceClientId);
                client.DefaultRequestHeaders.TryAddWithoutValidation("client_secret", plainSecret);
                client.DefaultRequestHeaders.TryAddWithoutValidation("gstin",         settings.GSTCode);

                _logger.LogInformation("IRP: Authenticating at {AuthUrl}", settings.EInvoiceAuthUrl);

                var response = await client.PostAsync(settings.EInvoiceAuthUrl, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("IRP Auth response ({Status}): {Body}", (int)response.StatusCode, responseBody[..Math.Min(500, responseBody.Length)]);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("IRP authentication failed: {Status} - {Body}", response.StatusCode, responseBody);
                    return null;
                }

                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                var token    = root.TryGetProperty("access_token", out var t) ? t.GetString() : null;
                var expiresIn = root.TryGetProperty("expires_in",  out var e) ? e.GetInt32() : 3600;

                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogError("IRP: access_token missing in auth response");
                    return null;
                }

                var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60); // 60-second safety margin

                // Persist token to DB
                const string insertSql = @"
                    INSERT INTO dbo.EInvoiceIrpTokens (BranchID, SessionUserId, AccessToken, ExpiresAt, CreatedBy)
                    VALUES (@BranchID, @SessionUserId, @AccessToken, @ExpiresAt, @CreatedBy);";

                await conn.ExecuteAsync(insertSql, new
                {
                    BranchID      = branchId,
                    SessionUserId = userId,
                    AccessToken   = token,
                    ExpiresAt     = expiresAt,
                    CreatedBy     = userId
                });

                _logger.LogInformation("IRP: Token obtained and cached for branch {BranchID}, expires {ExpiresAt}", branchId, expiresAt);
                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IRP: Authentication threw an exception for branch {BranchID}", branchId);
                return null;
            }
        }

        // ── IRN Generation ────────────────────────────────────────────────────

        public async Task<IrpIrnResponse> GenerateIrnAsync(
            HotelSettings settings,
            string accessToken,
            string invoiceJson)
        {
            if (string.IsNullOrWhiteSpace(settings.EInvoiceIrnEndpoint))
            {
                return Fail("EInvoiceIrnEndpoint is not configured.", invoiceJson);
            }

            try
            {
                var client = _httpClientFactory.CreateClient("IrpClient");

                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Bearer", accessToken);
                client.DefaultRequestHeaders.TryAddWithoutValidation("gstin", settings.GSTCode);

                var content = new StringContent(invoiceJson, Encoding.UTF8, "application/json");

                _logger.LogInformation("IRP: Calling IRN endpoint {Endpoint}", settings.EInvoiceIrnEndpoint);

                var response = await client.PostAsync(settings.EInvoiceIrnEndpoint, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("IRP IRN response ({Status}): {Body}",
                    (int)response.StatusCode,
                    responseBody[..Math.Min(1000, responseBody.Length)]);

                if (!response.IsSuccessStatusCode)
                {
                    return new IrpIrnResponse
                    {
                        Success      = false,
                        RawRequest   = invoiceJson,
                        RawResponse  = responseBody,
                        ErrorMessage = $"HTTP {(int)response.StatusCode}: {responseBody}"
                    };
                }

                // Parse success response
                using var doc = JsonDocument.Parse(responseBody);
                var root = doc.RootElement;

                // NIC sandbox wraps in "result" object; handle both flat and nested
                JsonElement data = root;
                if (root.TryGetProperty("result", out var resultNode))
                    data = resultNode;

                return new IrpIrnResponse
                {
                    Success      = true,
                    Irn          = GetString(data, "Irn"),
                    AckNo        = GetString(data, "AckNo"),
                    AckDt        = GetString(data, "AckDt"),
                    SignedQRCode = GetString(data, "SignedQRCode"),
                    RawRequest   = invoiceJson,
                    RawResponse  = responseBody
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IRP: GenerateIrn threw an exception");
                return new IrpIrnResponse
                {
                    Success      = false,
                    RawRequest   = invoiceJson,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private string DecryptIfNeeded(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            try { return _protector.Unprotect(value); }
            catch { return value; } // already plaintext (during dev/sandbox)
        }

        private static string? GetString(JsonElement el, string property)
            => el.TryGetProperty(property, out var p) ? p.GetString() : null;

        private static IrpIrnResponse Fail(string message, string? request = null)
            => new IrpIrnResponse { Success = false, ErrorMessage = message, RawRequest = request };
    }
}
