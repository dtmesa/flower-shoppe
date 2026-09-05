using System.Text.RegularExpressions;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Options;

namespace PlumeriaStore.Api.Features.Inventory;

/// <summary>
/// Photo storage in S3 (MinIO locally). A Lambda's filesystem is ephemeral and not shared between
/// concurrent executions, so what used to be a directory of files is now a bucket - the filenames
/// recorded on an item are unchanged, they are just object keys under a prefix now.
/// </summary>
public partial class S3FileStorage : IFileStorage
{
    private static readonly Dictionary<string, string> ExtensionByContentType = new()
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    /// <summary>
    /// Filenames are ours (a GUID plus one of the extensions above), never the client's. Requests
    /// for a photo arrive as a route value, so anything not matching this shape is rejected rather
    /// than concatenated into an object key.
    /// </summary>
    [GeneratedRegex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\.(jpg|png|webp|gif)$")]
    private static partial Regex GeneratedFilename();

    private readonly IAmazonS3 _s3;
    private readonly StorageOptions _options;

    public S3FileStorage(IAmazonS3 s3, IOptions<StorageOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task<string> StoreAsync(IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file.Length == 0)
        {
            throw new BadRequestException("Uploaded file is empty");
        }
        if (file.Length > _options.MaxSizeBytes)
        {
            throw new BadRequestException($"Image exceeds maximum size of {_options.MaxSizeBytes / 1024 / 1024}MB");
        }
        if (string.IsNullOrEmpty(file.ContentType) || !ExtensionByContentType.TryGetValue(file.ContentType, out var extension))
        {
            throw new BadRequestException("Only JPEG, PNG, WEBP, or GIF images are allowed");
        }

        var filename = $"{Guid.NewGuid()}{extension}";

        await using var stream = file.OpenReadStream();
        await _s3.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = KeyFor(filename),
            InputStream = stream,
            ContentType = file.ContentType,
            // S3 wants the length up front; without it the SDK buffers the whole body to compute one.
            Headers = { ContentLength = file.Length },
        }, cancellationToken);

        return filename;
    }

    public async Task<StoredFile?> OpenAsync(string filename, CancellationToken cancellationToken = default)
    {
        if (!GeneratedFilename().IsMatch(filename))
        {
            return null;
        }

        try
        {
            var response = await _s3.GetObjectAsync(_options.BucketName, KeyFor(filename), cancellationToken);
            return new StoredFile(response.ResponseStream, response.Headers.ContentType, response.ContentLength);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task DeleteAsync(string filename, CancellationToken cancellationToken = default)
    {
        await _s3.DeleteObjectAsync(_options.BucketName, KeyFor(filename), cancellationToken);
    }

    private string KeyFor(string filename) => $"{_options.KeyPrefix}{filename}";
}
