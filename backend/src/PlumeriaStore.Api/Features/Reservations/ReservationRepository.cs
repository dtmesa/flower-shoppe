using Amazon.DynamoDBv2.Model;
using PlumeriaStore.Api.Common.Data;

namespace PlumeriaStore.Api.Features.Reservations;

/// <summary>
/// A stock change to apply to one inventory item alongside a pickup request write. The values it
/// was read at are carried too, so the write can be conditioned on them: the request and the
/// counts it moves land together or not at all.
/// </summary>
public sealed record StockAdjustment(
    string ItemId,
    int FromTotal,
    int FromReserved,
    int ToTotal,
    int ToReserved);

public sealed class ReservationRepository
{
    /// <summary>Counter name behind <see cref="PickupRequest.Id"/>, which used to be a SQLite identity column.</summary>
    private const string IdCounter = "request";

    // TransactWriteItems takes at most 100 actions, one of which is the request itself. A cart
    // that large isn't reachable through the UI, but failing clearly beats a raw SDK error.
    private const int MaxItemsPerTransaction = 99;

    private readonly DynamoTable _table;

    public ReservationRepository(DynamoTable table)
    {
        _table = table;
    }

    public async Task<List<PickupRequest>> FindAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _table.QueryPartitionAsync(DynamoKeys.Request, cancellationToken);
        return items.Select(FromItem).ToList();
    }

    public async Task<PickupRequest?> FindByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var item = await _table.GetAsync(DynamoKeys.Request, DynamoKeys.RequestSortKey(id), cancellationToken);
        return item is null ? null : FromItem(item);
    }

    public Task<int> NextIdAsync(CancellationToken cancellationToken = default) =>
        _table.NextIdAsync(IdCounter, cancellationToken);

    /// <summary>
    /// Writes the request and, in the same transaction, the stock counts its holds move. With no
    /// adjustments this is a plain put.
    /// </summary>
    public Task SaveAsync(
        PickupRequest request,
        IReadOnlyList<StockAdjustment>? stockAdjustments = null,
        CancellationToken cancellationToken = default) =>
        WriteAsync(
            () => new TransactWriteItem { Put = new Put { TableName = _table.TableName, Item = ToItem(request) } },
            () => _table.PutAsync(ToItem(request), cancellationToken: cancellationToken),
            stockAdjustments,
            cancellationToken);

    /// <summary>
    /// Removes the request, giving back any stock it was still holding in the same transaction -
    /// otherwise those units stay reserved against an item with nothing left pointing at them.
    /// </summary>
    public Task DeleteAsync(
        int id,
        IReadOnlyList<StockAdjustment>? stockAdjustments = null,
        CancellationToken cancellationToken = default)
    {
        var sortKey = DynamoKeys.RequestSortKey(id);

        return WriteAsync(
            () => new TransactWriteItem
            {
                Delete = new Delete
                {
                    TableName = _table.TableName,
                    Key = DynamoTable.Key(DynamoKeys.Request, sortKey),
                },
            },
            () => _table.DeleteAsync(DynamoKeys.Request, sortKey, cancellationToken),
            stockAdjustments,
            cancellationToken);
    }

    /// <summary>
    /// Applies the request write on its own, or inside a transaction with the stock updates it
    /// carries. Both forms are passed lazily so only the one that runs is built.
    /// </summary>
    private async Task WriteAsync(
        Func<TransactWriteItem> requestWrite,
        Func<Task> writeAlone,
        IReadOnlyList<StockAdjustment>? stockAdjustments,
        CancellationToken cancellationToken)
    {
        var adjustments = (stockAdjustments ?? [])
            .Where(adjustment => adjustment.ToTotal != adjustment.FromTotal || adjustment.ToReserved != adjustment.FromReserved)
            .ToList();

        // A transaction of one costs more than the plain call it would wrap, and buys nothing.
        if (adjustments.Count == 0)
        {
            await writeAlone();
            return;
        }

        if (adjustments.Count > MaxItemsPerTransaction)
        {
            throw new BadRequestException($"A pickup request can hold stock for at most {MaxItemsPerTransaction} different items");
        }

        var writes = new List<TransactWriteItem> { requestWrite() };

        writes.AddRange(adjustments.Select(adjustment => new TransactWriteItem
        {
            Update = new Update
            {
                TableName = _table.TableName,
                Key = DynamoTable.Key(DynamoKeys.Item, adjustment.ItemId),
                UpdateExpression = "SET QuantityTotal = :toTotal, QuantityReserved = :toReserved",
                // Without attribute_exists an update would conjure a row for an item deleted since
                // it was read; the count comparisons make the write fail rather than clobber a
                // change someone else made in between.
                ConditionExpression = "attribute_exists(SK) AND QuantityTotal = :fromTotal AND QuantityReserved = :fromReserved",
                ExpressionAttributeValues = new Dictionary<string, AttributeValue>
                {
                    [":toTotal"] = Attr.N(adjustment.ToTotal),
                    [":toReserved"] = Attr.N(adjustment.ToReserved),
                    [":fromTotal"] = Attr.N(adjustment.FromTotal),
                    [":fromReserved"] = Attr.N(adjustment.FromReserved),
                },
            },
        }));

        try
        {
            await _table.TransactWriteAsync(writes, cancellationToken);
        }
        catch (TransactionCanceledException)
        {
            throw new BadRequestException("This request's stock changed while you were working on it. Reload and try again.");
        }
    }

    /// <summary>
    /// Drops every line's reference to a now-deleted inventory item, keeping the snapshot so the
    /// request still reads correctly. SQLite did this with ON DELETE SET NULL; here the lines live
    /// inside their request, so the requests that mention the item are rewritten instead.
    /// </summary>
    public async Task ClearInventoryItemReferencesAsync(string itemId, CancellationToken cancellationToken = default)
    {
        var requests = await FindAllAsync(cancellationToken);

        foreach (var request in requests.Where(request => request.Items.Any(line => line.InventoryItemId == itemId)))
        {
            foreach (var line in request.Items.Where(line => line.InventoryItemId == itemId))
            {
                line.InventoryItemId = null;
            }

            await _table.PutAsync(ToItem(request), cancellationToken: cancellationToken);
        }
    }

    private static Dictionary<string, AttributeValue> ToItem(PickupRequest request) => new()
    {
        [DynamoKeys.PartitionKey] = Attr.S(DynamoKeys.Request),
        [DynamoKeys.SortKey] = Attr.S(DynamoKeys.RequestSortKey(request.Id)),
        ["Id"] = Attr.N(request.Id),
        ["CustomerName"] = Attr.S(request.CustomerName),
        ["CustomerPhone"] = Attr.SOrNull(request.CustomerPhone),
        ["CustomerEmail"] = Attr.SOrNull(request.CustomerEmail),
        ["Notes"] = Attr.SOrNull(request.Notes),
        ["Status"] = Attr.S(request.Status.ToString()),
        ["CreatedAt"] = Attr.Time(request.CreatedAt),
        ["Items"] = Attr.List(request.Items.Select(line => new Dictionary<string, AttributeValue>
        {
            ["Id"] = Attr.N(line.Id),
            ["InventoryItemId"] = Attr.SOrNull(line.InventoryItemId),
            ["ItemSnapshot"] = Attr.S(line.ItemSnapshot),
            ["QuantityRequested"] = Attr.N(line.QuantityRequested),
            ["StockReserved"] = Attr.Bool(line.StockReserved),
        })),
    };

    private static PickupRequest FromItem(Dictionary<string, AttributeValue> item) => new()
    {
        Id = Attr.GetInt(item, "Id"),
        CustomerName = Attr.GetString(item, "CustomerName"),
        CustomerPhone = Attr.GetStringOrNull(item, "CustomerPhone"),
        CustomerEmail = Attr.GetStringOrNull(item, "CustomerEmail"),
        Notes = Attr.GetStringOrNull(item, "Notes"),
        Status = Attr.GetEnum<ReservationStatus>(item, "Status"),
        CreatedAt = Attr.GetTime(item, "CreatedAt"),
        Items = Attr.GetList(item, "Items").Select(line => new Reservation
        {
            Id = Attr.GetInt(line, "Id"),
            InventoryItemId = Attr.GetStringOrNull(line, "InventoryItemId"),
            ItemSnapshot = Attr.GetString(line, "ItemSnapshot"),
            QuantityRequested = Attr.GetInt(line, "QuantityRequested"),
            StockReserved = Attr.GetBool(line, "StockReserved"),
        }).ToList(),
    };
}
