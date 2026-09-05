using Amazon.DynamoDBv2.Model;
using PlumeriaStore.Api.Common.Data;

namespace PlumeriaStore.Api.Features.Inventory;

public sealed class CategoryRepository
{
    /// <summary>Counter name behind <see cref="InventoryCategory.Id"/>, which used to be a SQLite identity column.</summary>
    private const string IdCounter = "category";

    private readonly DynamoTable _table;

    public CategoryRepository(DynamoTable table)
    {
        _table = table;
    }

    /// <summary>Every category, in sort-key ("KIND#Name") order - which is the order the API returns them in.</summary>
    public async Task<List<InventoryCategory>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _table.QueryPartitionAsync(DynamoKeys.Category, cancellationToken);
        return items.Select(FromItem).ToList();
    }

    /// <summary>
    /// Categories are keyed by kind+name (that's what makes the pair unique), so a lookup by the
    /// numeric ID the API exposes reads the partition and filters. It holds a handful of rows.
    /// </summary>
    public async Task<InventoryCategory?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var categories = await FindAllAsync(cancellationToken);
        return categories.FirstOrDefault(category => category.Id == id);
    }

    public Task<int> NextIdAsync(CancellationToken cancellationToken = default) =>
        _table.NextIdAsync(IdCounter, cancellationToken);

    /// <summary>Returns false when a category of the same kind and name already exists.</summary>
    public async Task<bool> TryCreateAsync(InventoryCategory category, CancellationToken cancellationToken = default)
    {
        try
        {
            await _table.PutAsync(ToItem(category), "attribute_not_exists(SK)", cancellationToken);
            return true;
        }
        catch (ConditionalCheckFailedException)
        {
            return false;
        }
    }

    /// <summary>
    /// Applies an edit, returning false if it renames the category onto one that already exists.
    /// A rename moves the row, because the name is part of the key, so delete and put go in one
    /// transaction - a failure can't leave the category listed under both names or neither.
    /// </summary>
    public async Task<bool> ReplaceAsync(InventoryCategory original, InventoryCategory updated, CancellationToken cancellationToken = default)
    {
        var originalKey = DynamoKeys.CategorySortKey(original.Kind, original.Name);
        var updatedKey = DynamoKeys.CategorySortKey(updated.Kind, updated.Name);

        if (originalKey == updatedKey)
        {
            await _table.PutAsync(ToItem(updated), cancellationToken: cancellationToken);
            return true;
        }

        var writes = new List<TransactWriteItem>
        {
            new TransactWriteItem
            {
                Delete = new Delete
                {
                    TableName = _table.TableName,
                    Key = DynamoTable.Key(DynamoKeys.Category, originalKey),
                },
            },
            new TransactWriteItem
            {
                Put = new Put
                {
                    TableName = _table.TableName,
                    Item = ToItem(updated),
                    ConditionExpression = "attribute_not_exists(SK)",
                },
            },
        };

        try
        {
            await _table.TransactWriteAsync(writes, cancellationToken);
            return true;
        }
        catch (TransactionCanceledException)
        {
            return false;
        }
    }

    public Task DeleteAsync(InventoryCategory category, CancellationToken cancellationToken = default) =>
        _table.DeleteAsync(DynamoKeys.Category, DynamoKeys.CategorySortKey(category.Kind, category.Name), cancellationToken);

    private static Dictionary<string, AttributeValue> ToItem(InventoryCategory category) => new()
    {
        [DynamoKeys.PartitionKey] = Attr.S(DynamoKeys.Category),
        [DynamoKeys.SortKey] = Attr.S(DynamoKeys.CategorySortKey(category.Kind, category.Name)),
        ["Id"] = Attr.N(category.Id),
        ["Kind"] = Attr.S(category.Kind.ToString()),
        ["Name"] = Attr.S(category.Name),
        ["Code"] = Attr.S(category.Code),
    };

    private static InventoryCategory FromItem(Dictionary<string, AttributeValue> item) => new()
    {
        Id = Attr.GetInt(item, "Id"),
        Kind = Attr.GetEnum<CategoryKind>(item, "Kind"),
        Name = Attr.GetString(item, "Name"),
        Code = Attr.GetString(item, "Code"),
    };
}
