using Amazon;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.SimpleEmailV2;
using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Options;

namespace PlumeriaStore.Api.Common.Data;

public static class AwsClientRegistration
{
    /// <summary>
    /// Registers the DynamoDB, S3, and SES clients.
    ///
    /// Deployed, all three take their region and credentials from the environment Lambda sets up.
    /// Locally, DynamoDB and S3 point at emulator containers and use the keys those containers
    /// were started with - which is why they are configured separately from SES rather than all
    /// sharing one credential chain: SES is the one that still talks to real AWS in development,
    /// with real keys out of backend/.env.
    /// </summary>
    public static IServiceCollection AddAwsClients(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonDynamoDB>(provider =>
        {
            var aws = provider.GetRequiredService<IOptions<AwsOptions>>().Value;
            var dynamo = provider.GetRequiredService<IOptions<DynamoOptions>>().Value;

            var config = new AmazonDynamoDBConfig();
            ApplyEndpoint(config, aws.Region, dynamo.ServiceUrl);

            return Credentials(dynamo.AccessKey, dynamo.SecretKey) is { } credentials
                ? new AmazonDynamoDBClient(credentials, config)
                : new AmazonDynamoDBClient(config);
        });

        services.AddSingleton<IAmazonS3>(provider =>
        {
            var aws = provider.GetRequiredService<IOptions<AwsOptions>>().Value;
            var storage = provider.GetRequiredService<IOptions<StorageOptions>>().Value;

            var config = new AmazonS3Config { ForcePathStyle = storage.ForcePathStyle };
            ApplyEndpoint(config, aws.Region, storage.ServiceUrl);

            return Credentials(storage.AccessKey, storage.SecretKey) is { } credentials
                ? new AmazonS3Client(credentials, config)
                : new AmazonS3Client(config);
        });

        // Region is passed explicitly so startup doesn't depend on AWS_REGION or ~/.aws/config
        // existing; credentials come from AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY (loaded from
        // backend/.env in development) via the SDK's own resolution chain.
        services.AddSingleton<IAmazonSimpleEmailServiceV2>(provider =>
        {
            var region = provider.GetRequiredService<IOptions<EmailOptions>>().Value.Region;
            return new AmazonSimpleEmailServiceV2Client(RegionEndpoint.GetBySystemName(region));
        });

        return services;
    }

    private static void ApplyEndpoint(ClientConfig config, string region, string serviceUrl)
    {
        if (!string.IsNullOrWhiteSpace(serviceUrl))
        {
            config.ServiceURL = serviceUrl;
            // An emulator ignores the region but the SDK still insists on signing with one.
            config.AuthenticationRegion = string.IsNullOrWhiteSpace(region) ? "us-east-1" : region;
            return;
        }

        if (!string.IsNullOrWhiteSpace(region))
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(region);
        }
    }

    private static AWSCredentials? Credentials(string accessKey, string secretKey) =>
        string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey)
            ? null
            : new BasicAWSCredentials(accessKey, secretKey);
}
