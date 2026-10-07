namespace UniversityLostFound.Api.Auth;

internal static class StaffCookie
{
    public const string Name = "universitylostfound_staff";

    // Lax: browsers reach the api through the frontend's same-origin rewrite, so the cookie is first-party.
    // None would also send it on cross-site requests and widen the CSRF surface for no benefit.
    public static CookieOptions Options(DateTimeOffset expiresAt) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        Expires = expiresAt,
    };

    public static CookieOptions Expired() => Options(DateTimeOffset.UnixEpoch);
}
