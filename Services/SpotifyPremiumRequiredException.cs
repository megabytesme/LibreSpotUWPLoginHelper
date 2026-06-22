namespace LibreSpotUWPLoginHelper.Services;

internal sealed class SpotifyPremiumRequiredException : System.Exception
{
    public const string PremiumUrl = "https://www.spotify.com/premium/";

    public string? Product { get; }

    public SpotifyPremiumRequiredException(string? product)
        : base(BuildMessage(product))
    {
        Product = product;
    }

    private static string BuildMessage(string? product)
    {
        var accountType = string.IsNullOrWhiteSpace(product)
            ? "not reported as Premium"
            : "reported as Spotify " + product;

        return "LibreSpotUWP requires a Spotify Premium account. This account is " +
            accountType +
            ", so it cannot be used with LibreSpotUWP. Please upgrade to Spotify Premium and try signing in again.";
    }
}
