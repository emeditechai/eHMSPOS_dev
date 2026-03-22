namespace HotelApp.Web.Services;

public interface IPublicIpService
{
    /// <summary>
    /// Resolves the real public IP address of the calling client.
    ///
    /// Resolution order:
    ///   1. X-Forwarded-For header (first entry in the chain)
    ///   2. X-Real-IP header
    ///   3. TCP RemoteIpAddress (mapped to IPv4)
    ///   4. If the result is a loopback (::1 / 127.x.x.x) — i.e. the browser and
    ///      server are on the same machine — fall back to fetching the server's own
    ///      actual outbound public IP from an external echo service.
    ///      The external result is cached for 6 hours so the outbound call is made
    ///      at most once per application lifetime per cache period.
    /// </summary>
    Task<string> ResolveAsync(HttpContext context);
}
