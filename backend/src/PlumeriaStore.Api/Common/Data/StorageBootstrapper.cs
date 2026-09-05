using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Options;

namespace PlumeriaStore.Api.Common.Data;

/// <summary>
/// Creates the DynamoDB table and S3 bucket when the corresponding
/// <c>CreateTableIfMissing</c>/<c>CreateBucketIfMissing</c> flags are on. That is a local-development
/// convenience for the emulators in docker-compose - deployed, both come from the CloudFormation
/// stack and the function's role has no permission to create either.
/// </summary>
public static class StorageBootstrapper
{
    public static async Task EnsureStorageAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var provider = scope.ServiceProvider;
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(StorageBootstrapper));

        var dynamoOptions = provider.GetRequiredService<IOptions<DynamoOptions>>().Value;
        if (dynamoOptions.CreateTableIfMissing)
        {
            await EnsureTableAsync(provider.GetRequiredService<IAmazonDynamoDB>(), dynamoOptions.TableName, logger);
        }

        var storageOptions = provider.GetRequiredService<IOptions<StorageOptions>>().Value;
        if (storageOptions.CreateBucketIfMissing)
        {
            await EnsureBucketAsync(provider.GetRequiredService<IAmazonS3>(), storageOptions.BucketName, logger);
        }
    }

    private static async Task EnsureTableAsync(IAmazonDynamoDB dynamo, string tableName, ILogger logger)
    {
        try
        {
            await dynamo.DescribeTableAsync(tableName);
            return;
        }
        catch (ResourceNotFoundException)
        {
            logger.LogInformation("Creating DynamoDB table {TableName}", tableName);
        }

        await dynamo.CreateTableAsync(new CreateTableRequest
        {
            TableName = tableName,
            BillingMode = BillingMode.PAY_PER_REQUEST,
            AttributeDefinitions =
            [
                new AttributeDefinition(DynamoKeys.PartitionKey, ScalarAttributeType.S),
                new AttributeDefinition(DynamoKeys.SortKey, ScalarAttributeType.S),
            ],
            KeySchema =
            [
                new KeySchemaElement(DynamoKeys.PartitionKey, KeyType.HASH),
                new KeySchemaElement(DynamoKeys.SortKey, KeyType.RANGE),
            ],
        });

        // DynamoDB Local is effectively instant, but a real table takes a few seconds and every
        // read that follows would fail until it is ACTIVE.
        for (var attempt = 0; attempt < 30; attempt++)
        {
            var described = await dynamo.DescribeTableAsync(tableName);
            if (described.Table.TableStatus == TableStatus.ACTIVE)
            {
                return;
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        throw new InvalidOperationException($"DynamoDB table {tableName} did not become ACTIVE in time");
    }

    private static async Task EnsureBucketAsync(IAmazonS3 s3, string bucketName, ILogger logger)
    {
        try
        {
            await s3.GetBucketLocationAsync(bucketName);
            return;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound || ex.ErrorCode == "NoSuchBucket")
        {
            logger.LogInformation("Creating S3 bucket {BucketName}", bucketName);
        }

        await s3.PutBucketAsync(new PutBucketRequest { BucketName = bucketName });
    }
}
