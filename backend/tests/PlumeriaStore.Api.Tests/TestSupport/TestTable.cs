using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using PlumeriaStore.Api.Common.Data;

namespace PlumeriaStore.Api.Tests.TestSupport;

/// <summary>
/// A DynamoDB table and an S3 bucket named uniquely for one test class, created on construction
/// and torn down on dispose - so classes running side by side never see each other's rows.
/// </summary>
public sealed class TestTable : IDisposable
{
    private readonly IAmazonDynamoDB _dynamo;
    private readonly IAmazonS3 _s3;

    public string TableName { get; }
    public string BucketName { get; }

    public TestTable()
    {
        var suffix = Guid.NewGuid().ToString("N");
        TableName = $"plumeria-tests-{suffix}";
        BucketName = $"plumeria-tests-{suffix}";

        _dynamo = LocalAws.CreateDynamoClient();
        _s3 = LocalAws.CreateS3Client();

        try
        {
            CreateTable();
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw LocalAws.NotRunning("DynamoDB Local", ex);
        }

        try
        {
            _s3.PutBucketAsync(new PutBucketRequest { BucketName = BucketName }).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            throw LocalAws.NotRunning("MinIO", ex);
        }
    }

    private void CreateTable()
    {
        _dynamo.CreateTableAsync(new CreateTableRequest
        {
            TableName = TableName,
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
        }).GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        TryDelete(() => _dynamo.DeleteTableAsync(TableName).GetAwaiter().GetResult());
        TryDelete(() =>
        {
            // A bucket has to be empty before it will delete, and photo tests leave objects behind.
            var objects = _s3.ListObjectsV2Async(new ListObjectsV2Request { BucketName = BucketName })
                .GetAwaiter().GetResult();

            foreach (var entry in objects.S3Objects ?? [])
            {
                _s3.DeleteObjectAsync(BucketName, entry.Key).GetAwaiter().GetResult();
            }

            _s3.DeleteBucketAsync(BucketName).GetAwaiter().GetResult();
        });

        _dynamo.Dispose();
        _s3.Dispose();
    }

    /// <summary>Cleanup failures shouldn't mask the real result of a test run.</summary>
    private static void TryDelete(Action delete)
    {
        try
        {
            delete();
        }
        catch
        {
            // Leftover test table/bucket in the local emulator; harmless.
        }
    }
}
