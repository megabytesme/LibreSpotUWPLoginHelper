using System;
using System.Collections.Generic;

namespace LibreSpotUWPLoginHelper.Services;

internal static class QueryStringUtility
{
    public static IReadOnlyDictionary<string, string> Parse(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trimmed = query.TrimStart('?');

        if (string.IsNullOrWhiteSpace(trimmed))
            return result;

        var pairs = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }
}
