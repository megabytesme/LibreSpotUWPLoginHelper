namespace LibreSpotUWPLoginHelper.Models;

internal sealed record SpotifyAuthOptions(
    string ClientId,
    string[] Scopes,
    string RedirectPath = "/login");
