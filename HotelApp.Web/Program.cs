using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.AspNetCore.Authentication.Cookies;
using HotelApp.Web.Middleware;
using HotelApp.Web.Repositories;
using HotelApp.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
    {
        options.Filters.Add<HotelApp.Web.Filters.PageAuthorizationFilter>();
        options.Filters.Add<HotelApp.Web.Filters.SessionValidationFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

builder.Services.Configure<HotelApp.Web.Models.PaymentQrOptions>(
    builder.Configuration.GetSection("PaymentQr"));

// Persist DataProtection keys so signed public links (e.g., Guest Feedback tokens)
// remain valid across app restarts after publish.
var configuredDataProtectionKeysPath =
    builder.Configuration["DataProtection:KeysPath"] ??
    Environment.GetEnvironmentVariable("HOTELAPP_DATAPROTECTION_KEYS");

var dataProtectionKeysPath = !string.IsNullOrWhiteSpace(configuredDataProtectionKeysPath)
    ? configuredDataProtectionKeysPath
    : Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtectionKeys");

Exception? dataProtectionInitError = null;

static void TryMigrateLegacyDataProtectionKeys(string newKeyRingPath)
{
    try
    {
        // If we already have keys in the new location, do nothing.
        if (Directory.Exists(newKeyRingPath) && Directory.EnumerateFiles(newKeyRingPath, "*.xml").Any())
        {
            return;
        }

        var legacyCandidates = new List<string>();

        // Common default key ring locations for ASP.NET Core.
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(home))
        {
            legacyCandidates.Add(Path.Combine(home, ".aspnet", "DataProtection-Keys"));
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrWhiteSpace(localAppData))
        {
            legacyCandidates.Add(Path.Combine(localAppData, "ASP.NET", "DataProtection-Keys"));
        }

        foreach (var legacyPath in legacyCandidates.Distinct())
        {
            if (!Directory.Exists(legacyPath))
            {
                continue;
            }

            var legacyFiles = Directory.EnumerateFiles(legacyPath, "*.xml").ToList();
            if (legacyFiles.Count == 0)
            {
                continue;
            }

            foreach (var src in legacyFiles)
            {
                var dest = Path.Combine(newKeyRingPath, Path.GetFileName(src));
                if (File.Exists(dest))
                {
                    continue;
                }

                File.Copy(src, dest);
            }

            // Migration succeeded from the first location that had keys.
            return;
        }
    }
    catch
    {
        // Best-effort only. If keys are genuinely missing, mail password must be re-saved.
    }
}

try
{
    Directory.CreateDirectory(dataProtectionKeysPath);
    TryMigrateLegacyDataProtectionKeys(dataProtectionKeysPath);

    builder.Services
        .AddDataProtection()
        .SetApplicationName("HotelApp")
        .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
}
catch (Exception ex)
{
    dataProtectionInitError = ex;

    // Fall back to default key storage. This avoids failing startup under locked-down
    // IIS/AppPool identities that can't write to the app directory.
    builder.Services
        .AddDataProtection()
        .SetApplicationName("HotelApp");
}

// Database connection factory
builder.Services.AddScoped<IDbConnection>(_ => new SqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpContextAccessor();

// Repositories & services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserBranchRepository, UserBranchRepository>();
builder.Services.AddScoped<IUserRoleRepository, UserRoleRepository>();
builder.Services.AddScoped<IUserBranchRoleRepository, UserBranchRoleRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<INavMenuRepository, NavMenuRepository>();
builder.Services.AddScoped<IRoleNavMenuRepository, RoleNavMenuRepository>();
builder.Services.AddScoped<IRoleDashboardConfigRepository, RoleDashboardConfigRepository>();
builder.Services.AddScoped<IPaymentDashboardRepository, PaymentDashboardRepository>();
builder.Services.AddScoped<IAuthorizationResourceRepository, AuthorizationResourceRepository>();
builder.Services.AddScoped<IAuthorizationPermissionRepository, AuthorizationPermissionRepository>();
builder.Services.AddScoped<IAuthorizationMatrixService, AuthorizationMatrixService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRateMasterRepository, RateMasterRepository>();
builder.Services.AddScoped<IFloorRepository, FloorRepository>();
builder.Services.AddScoped<IRoomTypeRepository, RoomTypeRepository>();
builder.Services.AddScoped<IAmenityRepository, AmenityRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IGuestRepository, GuestRepository>();
builder.Services.AddScoped<IDashboardRepository, DashboardRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IBankRepository, BankRepository>();
builder.Services.AddScoped<IHotelSettingsRepository, HotelSettingsRepository>();
builder.Services.AddScoped<ILocationRepository, LocationRepository>();
builder.Services.AddScoped<IReportsRepository, ReportsRepository>();
builder.Services.AddScoped<ICancellationPolicyRepository, CancellationPolicyRepository>();
builder.Services.AddScoped<IOtherChargeRepository, OtherChargeRepository>();
builder.Services.AddScoped<IB2BClientRepository, B2BClientRepository>();
builder.Services.AddScoped<IB2BAgreementRepository, B2BAgreementRepository>();
builder.Services.AddScoped<IB2BTermsConditionRepository, B2BTermsConditionRepository>();
builder.Services.AddScoped<IGstSlabRepository, GstSlabRepository>();
builder.Services.AddScoped<IBookingOtherChargeRepository, BookingOtherChargeRepository>();
builder.Services.AddScoped<IRoomServiceRepository, RoomServiceRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IBillingHeadRepository, BillingHeadRepository>();
builder.Services.AddScoped<IUpiSettingsRepository, UpiSettingsRepository>();
builder.Services.AddScoped<IMailConfigurationRepository, MailConfigurationRepository>();
builder.Services.AddScoped<IBookingReceiptTemplateRepository, BookingReceiptTemplateRepository>();
builder.Services.AddScoped<IGuestFeedbackRepository, GuestFeedbackRepository>();
builder.Services.AddScoped<IAssetManagementRepository, AssetManagementRepository>();
builder.Services.AddScoped<IRefundRepository, RefundRepository>();

// Licensing
builder.Services.AddMemoryCache();   // used by LicenseMiddleware for midnight-reset daily validation cache
builder.Services.AddHttpClient();    // used by PublicIpService for outbound public-IP echo calls
builder.Services.AddScoped<ILicenseRepository, LicenseRepository>();
builder.Services.AddScoped<IRemoteLicenseRepository, RemoteLicenseRepository>();
builder.Services.AddSingleton<IHardwareInfoService, HardwareInfoService>();
builder.Services.AddSingleton<IPublicIpService, PublicIpService>();
builder.Services.AddScoped<ILicenseOtpService, LicenseOtpService>();

builder.Services.AddScoped<IMailPasswordProtector, MailPasswordProtector>();
builder.Services.AddScoped<IMailSender, MailSender>();
builder.Services.AddScoped<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
builder.Services.AddScoped<IGuestFeedbackLinkService, GuestFeedbackLinkService>();

// Session configuration for BranchID storage
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(2);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // For JSON polling endpoints, avoid HTML redirects.
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/Notifications"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            },
            OnRedirectToAccessDenied = ctx =>
            {
                if (ctx.Request.Path.StartsWithSegments("/Notifications"))
                {
                    ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                ctx.Response.Redirect(ctx.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

var app = builder.Build();

if (dataProtectionInitError is not null)
{
    app.Logger.LogWarning(
        dataProtectionInitError,
        "DataProtection keys couldn't be persisted to '{KeysPath}'. Falling back to default key storage.",
        dataProtectionKeysPath);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Database connectivity gate — serves maintenance page if DB is unreachable
app.UseMiddleware<DatabaseHealthCheckMiddleware>();

app.UseRouting();

app.UseSession();

// License gate — runs after static files and session, before auth
app.UseMiddleware<LicenseMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboard}/{action=Index}/{id?}")
    ;


app.Run();
