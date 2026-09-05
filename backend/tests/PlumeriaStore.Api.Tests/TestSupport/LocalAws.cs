using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;

namespace PlumeriaStore.Api.Tests.TestSupport;

/// <summary>
/// The emulated AWS services the test suite runs against - DynamoDB Local and MinIO, both from
/// the repository's docker-compose.yml. There is no in-memory stand-in for DynamoDB: conditional
/// writes and transactions are what this data layer is built out of, and a hand-rolled fake of
/// them would be testing the fake.
///
/// Point <c>PLUMERIA_TEST_DYNAMODB_URL</c> / <c>PLUMERIA_TEST_S3_URL</c> elsewhere to override.
/// </summary>
public static class LocalAws
{
    public const string AccessKey = "plumeria";
    public const string SecretKey = "plumeria-local";
    public const string Region = "us-west-2";

    public static string DynamoUrl =>
        Environment.GetEnvironmentVariable("PLUMERIA_TEST_DYNAMODB_URL") ?? "http://localhost:8000";

    public static string S3Url =>
        Environment.GetEnvironmentVariable("PLUMERIA_TEST_S3_URL") ?? "http://localhost:9000";

    public static AWSCredentials Credentials => new BasicAWSCredentials(AccessKey, SecretKey);

    public static IAmazonDynamoDB CreateDynamoClient() =>
        new AmazonDynamoDBClient(Credentials, new AmazonDynamoDBConfig
        {
            ServiceURL = DynamoUrl,
            AuthenticationRegion = Region,
        });

    public static IAmazonS3 CreateS3Client() =>
        new AmazonS3Client(Credentials, new AmazonS3Config
        {
            ServiceURL = S3Url,
            AuthenticationRegion = Region,
            ForcePathStyle = true,
        });

    /// <summary>
    /// Turns "connection refused" into an instruction, since a first run with the containers down
    /// otherwise fails with an SDK error that says nothing about what to do.
    /// </summary>
    public static Exception NotRunning(string service, Exception inner) =>
        new InvalidOperationException(
            $"Could not reach {service} for the test suite. Start the emulators first:\n" +
            "    docker compose up -d dynamodb minio",
            inner);
}
