namespace LibreSpotUWPLoginHelper.Models;

internal sealed record SpotifyAuthOptions(
    string ClientId,
    string[] Scopes,
    int LoopbackPort,
    string RedirectPath = "/login");
