using System;

namespace LibreSpotUWPLoginHelper.Models;

internal sealed class LoginPackage
{
    public const string CurrentFormat = "LibreSpotUWP.Login";
    public const int CurrentVersion = 2;

    public string Format { get; set; } = CurrentFormat;
    public int Version { get; set; } = CurrentVersion;
    public string MinimumAppVersion { get; set; } = "1.0.5.0";
    public string AccountId { get; set; } = string.Empty;
    public QrAuthState Web { get; set; } = new();
    public PlaybackAuthorizationPackage Playback { get; set; } = new();
}

internal sealed class PlaybackAuthorizationPackage
{
    public int AuthVersion { get; set; } = 1;
    public string Kind { get; set; } = "bootstrapToken";
    public string AccessToken { get; set; } = string.Empty;
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? StoredCredentials { get; set; }
}
