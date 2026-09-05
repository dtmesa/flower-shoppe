using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Options;

namespace PlumeriaStore.Api.Common.Data;

/// <summary>
/// The application's one DynamoDB table, wrapped so repositories express intent (get this row,
/// list this partition, hand me the next ID) instead of restating request plumbing each time.
/// Every read here is strongly consistent: this replaced a SQL database, and the services above
/// it - create-then-list, confirm-then-read-stock - assume a write is visible to the next read.
/// </summary>
public sealed class DynamoTable
{
    private readonly IAmazonDynamoDB _client;

    public string TableName { get; }

    public DynamoTable(IAmazonDynamoDB client, IOptions<DynamoOptions> options)
    {
        _client = client;
        TableName = options.Value.TableName;
    }

    public static Dictionary<string, AttributeValue> Key(string partitionKey, string sortKey) => new()
    {
        [DynamoKeys.PartitionKey] = Attr.S(partitionKey),
        [DynamoKeys.SortKey] = Attr.S(sortKey),
    };

    public async Task<Dictionary<string, AttributeValue>?> GetAsync(string partitionKey, string sortKey, CancellationToken cancellationToken = default)
    {
        var response = await _client.GetItemAsync(new GetItemRequest
        {
            TableName = TableName,
            Key = Key(partitionKey, sortKey),
            ConsistentRead = true,
        }, cancellationToken);

        // The SDK returns an empty (not null) map when nothing matched.
        return response.Item is { Count: > 0 } item ? item : null;
    }

    /// <summary>Every row in one partition, in sort-key order, paging until DynamoDB stops offering more.</summary>
    public async Task<List<Dictionary<string, AttributeValue>>> QueryPartitionAsync(string partitionKey, CancellationToken cancellationToken = default)
    {
        var results = new List<Dictionary<string, AttributeValue>>();
        Dictionary<string, AttributeValue>? startKey = null;

        do
        {
            var response = await _client.QueryAsync(new QueryRequest
            {
                TableName = TableName,
                KeyConditionExpression = "#pk = :pk",
                ExpressionAttributeNames = new Dictionary<string, string> { ["#pk"] = DynamoKeys.PartitionKey },
                ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":pk"] = Attr.S(partitionKey) },
                ConsistentRead = true,
                ExclusiveStartKey = startKey,
            }, cancellationToken);

            results.AddRange(response.Items ?? []);
            startKey = response.LastEvaluatedKey is { Count: > 0 } last ? last : null;
        }
        while (startKey is not null);

        return results;
    }

    public Task PutAsync(Dictionary<string, AttributeValue> item, string? conditionExpression = null, CancellationToken cancellationToken = default) =>
        _client.PutItemAsync(new PutItemRequest
        {
            TableName = TableName,
            Item = item,
            ConditionExpression = conditionExpression,
        }, cancellationToken);

    public Task DeleteAsync(string partitionKey, string sortKey, CancellationToken cancellationToken = default) =>
        _client.DeleteItemAsync(new DeleteItemRequest
        {
            TableName = TableName,
            Key = Key(partitionKey, sortKey),
        }, cancellationToken);

    public Task TransactWriteAsync(List<TransactWriteItem> writes, CancellationToken cancellationToken = default) =>
        _client.TransactWriteItemsAsync(new TransactWriteItemsRequest { TransactItems = writes }, cancellationToken);

    /// <summary>
    /// The next value of a named counter. Replaces the identity columns categories and pickup
    /// requests used to get from SQLite; ADD on a missing row starts from zero, so the first
    /// caller gets 1 without the row needing to be seeded.
    /// </summary>
    public async Task<int> NextIdAsync(string counterName, CancellationToken cancellationToken = default)
    {
        var response = await _client.UpdateItemAsync(new UpdateItemRequest
        {
            TableName = TableName,
            Key = Key(DynamoKeys.Counter, counterName),
            UpdateExpression = "ADD #value :one",
            ExpressionAttributeNames = new Dictionary<string, string> { ["#value"] = "Value" },
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { [":one"] = Attr.N(1) },
            ReturnValues = ReturnValue.UPDATED_NEW,
        }, cancellationToken);

        return Attr.GetInt(response.Attributes, "Value");
    }
}
