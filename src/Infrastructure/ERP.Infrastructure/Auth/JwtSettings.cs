namespace ERP.Infrastructure.Auth;

/// <summary>JWT konfiqurasiyası (appsettings-dən). Serverdə açar secret store-dan gəlməlidir (TDD §39).</summary>
public sealed class JwtSettings
{
    public string Issuer { get; set; } = "ERP";
    public string Audience { get; set; } = "ERP.Clients";
    public string Key { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
}
