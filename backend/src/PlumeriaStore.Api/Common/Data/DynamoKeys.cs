namespace PlumeriaStore.Api.Common.Data;

/// <summary>
/// Partition keys for the single-table layout. Each entity kind lives in one partition and is
/// separated by sort key, which for a shop this size (tens of items, a few hundred requests) keeps
/// every "list them all" a single strongly-consistent Query - no secondary index, and none of the
/// read-your-own-write surprises an eventually-consistent GSI would introduce.
/// </summary>
public static class DynamoKeys
{
    public const string PartitionKey = "PK";
    public const string SortKey = "SK";

    /// <summary>The one admin account. Partition and sort key both, since there is exactly one.</summary>
    public const string Admin = "ADMIN";

    /// <summary>Sort key is "KIND#Name", which is what makes (kind, name) unique without a second index.</summary>
    public const string Category = "CATEGORY";

    /// <summary>Sort key is the item's own ID tag (e.g. "RYM").</summary>
    public const string Item = "ITEM";

    /// <summary>Sort key is <see cref="RequestSortKey"/> - zero-padded so string order matches numeric order.</summary>
    public const string Request = "REQUEST";

    /// <summary>Sort key is the counter's name; see <c>DynamoTable.NextIdAsync</c>.</summary>
    public const string Counter = "COUNTER";

    public static string CategorySortKey(Features.Inventory.CategoryKind kind, string name) => $"{kind}#{name}";

    public static string RequestSortKey(int id) => id.ToString("D10");
}
