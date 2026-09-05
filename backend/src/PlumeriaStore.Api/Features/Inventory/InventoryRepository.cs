using Amazon.DynamoDBv2.Model;
using PlumeriaStore.Api.Common.Data;

namespace PlumeriaStore.Api.Features.Inventory;

public sealed class InventoryRepository
{
    private readonly DynamoTable _table;

    public InventoryRepository(DynamoTable table)
    {
        _table = table;
    }

    public async Task<List<InventoryItem>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _table.QueryPartitionAsync(DynamoKeys.Item, cancellationToken);
        return items.Select(FromItem).ToList();
    }

    public async Task<InventoryItem?> FindByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await _table.GetAsync(DynamoKeys.Item, id, cancellationToken);
        return item is null ? null : FromItem(item);
    }

    public Task SaveAsync(InventoryItem item, CancellationToken cancellationToken = default) =>
        _table.PutAsync(ToItem(item), cancellationToken: cancellationToken);

    /// <summary>
    /// Returns false when an item with this ID already exists. The ID is derived from
    /// type+color+size, so that condition is exactly the "one row per combination" rule - checked
    /// by the write itself rather than by a read beforehand that another write could slip past.
    /// </summary>
    public async Task<bool> TryCreateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        try
        {
            await _table.PutAsync(ToItem(item), "attribute_not_exists(SK)", cancellationToken);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    public Task DeleteAsync(string id, CancellationToken cancellationToken = default) =>
        _table.DeleteAsync(DynamoKeys.Item, id, cancellationToken);

    public static Dictionary<string, AttributeValue> ToItem(InventoryItem item) => new()
    {
        [DynamoKeys.PartitionKey] = Attr.S(DynamoKeys.Item),
        [DynamoKeys.SortKey] = Attr.S(item.Id),
        ["Id"] = Attr.S(item.Id),
        ["Type"] = Attr.S(item.Type),
        ["Color"] = Attr.SOrNull(item.Color),
        ["Size"] = Attr.SOrNull(item.Size),
        ["Price"] = Attr.N(item.Price),
        ["QuantityTotal"] = Attr.N(item.QuantityTotal),
        ["QuantityReserved"] = Attr.N(item.QuantityReserved),
        ["Description"] = Attr.SOrNull(item.Description),
        ["NextImageId"] = Attr.N(item.NextImageId),
        ["Images"] = Attr.List(item.Images.Select(image => new Dictionary<string, AttributeValue>
        {
            ["Id"] = Attr.N(image.Id),
            ["Filename"] = Attr.S(image.Filename),
            ["SortOrder"] = Attr.N(image.SortOrder),
            ["IsPrimary"] = Attr.Bool(image.IsPrimary),
        })),
        ["CreatedAt"] = Attr.Time(item.CreatedAt),
        ["UpdatedAt"] = Attr.Time(item.UpdatedAt),
    };

    private static InventoryItem FromItem(Dictionary<string, AttributeValue> item) => new()
    {
        Id = Attr.GetString(item, "Id"),
        Type = Attr.GetString(item, "Type"),
        Color = Attr.GetStringOrNull(item, "Color"),
        Size = Attr.GetStringOrNull(item, "Size"),
        Price = Attr.GetDecimal(item, "Price"),
        QuantityTotal = Attr.GetInt(item, "QuantityTotal"),
        QuantityReserved = Attr.GetInt(item, "QuantityReserved"),
        Description = Attr.GetStringOrNull(item, "Description"),
        NextImageId = Attr.GetInt(item, "NextImageId", 1),
        Images = Attr.GetList(item, "Images").Select(image => new InventoryImage
        {
            Id = Attr.GetInt(image, "Id"),
            Filename = Attr.GetString(image, "Filename"),
            SortOrder = Attr.GetInt(image, "SortOrder"),
            IsPrimary = Attr.GetBool(image, "IsPrimary"),
        }).ToList(),
        CreatedAt = Attr.GetTime(item, "CreatedAt"),
        UpdatedAt = Attr.GetTime(item, "UpdatedAt"),
    };
}
