namespace PlumeriaStore.Api.Common.Options;

public class StorageOptions
{
    public const string SectionName = "App:Storage";

    public string BucketName { get; set; } = "plumeria-store-uploads";

    /// <summary>Prefix every object key gets, so the bucket can hold other things later.</summary>
    public string KeyPrefix { get; set; } = "uploads/";

    /// <summary>
    /// 4MB rather than the 5MB this used to allow: an upload reaches the function as a
    /// base64-encoded body, which inflates by ~4/3, and Lambda caps a request (and a response, so
    /// the same ceiling applies to serving the photo back) at 6MB.
    /// </summary>
    public long MaxSizeBytes { get; set; } = 4 * 1024 * 1024;

    /// <summary>Set to point at a MinIO container locally; blank means real S3. See <see cref="DynamoOptions.ServiceUrl"/>.</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>MinIO serves bucket-in-path rather than bucket-as-subdomain.</summary>
    public bool ForcePathStyle { get; set; }

    /// <summary>Local development only, same reasoning as <see cref="DynamoOptions.CreateTableIfMissing"/>.</summary>
    public bool CreateBucketIfMissing { get; set; }
}
