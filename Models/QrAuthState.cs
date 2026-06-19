using System;

namespace LibreSpotUWPLoginHelper.Models;

internal sealed class QrAuthState
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? LastTokenRefreshAt { get; set; }
    public DateTimeOffset? RefreshTokenExpiresAt { get; set; }
    public int ScopeVersion { get; set; }
}
