using Amazon.DynamoDBv2.Model;

namespace PlumeriaStore.Api.Common.Data;

/// <summary>
/// Construction and reading of DynamoDB <see cref="AttributeValue"/> maps. The low-level API is
/// used throughout rather than the object-persistence model, because that model resolves its
/// mappings by reflection and would not survive Native AOT trimming.
/// </summary>
public static class Attr
{
    public static AttributeValue S(string value) => new() { S = value };

    /// <summary>DynamoDB rejects an empty string in a key, and treats a missing attribute as absent, so null wins.</summary>
    public static AttributeValue SOrNull(string? value) =>
        string.IsNullOrEmpty(value) ? new AttributeValue { NULL = true } : S(value);

    public static AttributeValue N(long value) => new() { N = value.ToString(System.Globalization.CultureInfo.InvariantCulture) };

    public static AttributeValue N(decimal value) => new() { N = value.ToString(System.Globalization.CultureInfo.InvariantCulture) };

    public static AttributeValue Bool(bool value) => new() { BOOL = value };

    /// <summary>Round-trip ("O") format, so the stored text sorts chronologically and parses back as UTC.</summary>
    public static AttributeValue Time(DateTime value) =>
        S((value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime())
            .ToString("O", System.Globalization.CultureInfo.InvariantCulture));

    public static AttributeValue List(IEnumerable<Dictionary<string, AttributeValue>> maps) =>
        new() { L = maps.Select(map => new AttributeValue { M = map }).ToList() };

    public static string GetString(Dictionary<string, AttributeValue> item, string name) =>
        GetStringOrNull(item, name) ?? string.Empty;

    public static string? GetStringOrNull(Dictionary<string, AttributeValue> item, string name) =>
        item.TryGetValue(name, out var value) ? value.S : null;

    public static int GetInt(Dictionary<string, AttributeValue> item, string name, int fallback = 0) =>
        item.TryGetValue(name, out var value) && value.N is not null
            ? int.Parse(value.N, System.Globalization.CultureInfo.InvariantCulture)
            : fallback;

    public static decimal GetDecimal(Dictionary<string, AttributeValue> item, string name) =>
        item.TryGetValue(name, out var value) && value.N is not null
            ? decimal.Parse(value.N, System.Globalization.CultureInfo.InvariantCulture)
            : 0m;

    public static bool GetBool(Dictionary<string, AttributeValue> item, string name) =>
        item.TryGetValue(name, out var value) && value.IsBOOLSet && value.BOOL == true;

    public static DateTime GetTime(Dictionary<string, AttributeValue> item, string name) =>
        GetStringOrNull(item, name) is { } text
            ? DateTime.Parse(text, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.RoundtripKind).ToUniversalTime()
            : default;

    public static List<Dictionary<string, AttributeValue>> GetList(Dictionary<string, AttributeValue> item, string name) =>
        item.TryGetValue(name, out var value) && value.IsLSet
            ? value.L.Where(entry => entry.IsMSet).Select(entry => entry.M).ToList()
            : [];

    public static TEnum GetEnum<TEnum>(Dictionary<string, AttributeValue> item, string name) where TEnum : struct, Enum =>
        Enum.TryParse<TEnum>(GetStringOrNull(item, name), out var parsed) ? parsed : default;
}
