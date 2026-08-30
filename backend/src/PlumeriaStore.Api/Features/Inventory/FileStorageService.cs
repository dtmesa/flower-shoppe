using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Options;

namespace PlumeriaStore.Api.Features.Inventory;

public class FileStorageService
{
    private static readonly HashSet<string> AllowedContentTypes = new()
    {
        "image/jpeg", "image/png", "image/webp", "image/gif",
    };

    private readonly string _uploadDir;
    private readonly long _maxBytes;

    public FileStorageService(IOptions<UploadOptions> options)
    {
        _uploadDir = Path.GetFullPath(options.Value.Directory);
        _maxBytes = options.Value.MaxSizeBytes;
        Directory.CreateDirectory(_uploadDir);
    }

    public async Task<string> StoreAsync(IFormFile file)
    {
        if (file.Length == 0)
        {
            throw new BadRequestException("Uploaded file is empty");
        }
        if (file.Length > _maxBytes)
        {
            throw new BadRequestException($"Image exceeds maximum size of {_maxBytes / 1024 / 1024}MB");
        }
        if (string.IsNullOrEmpty(file.ContentType) || !AllowedContentTypes.Contains(file.ContentType))
        {
            throw new BadRequestException("Only JPEG, PNG, WEBP, or GIF images are allowed");
        }

        var extension = file.ContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => string.Empty,
        };
        var filename = $"{Guid.NewGuid()}{extension}";
        var targetPath = Path.Combine(_uploadDir, filename);

        await using var stream = new FileStream(targetPath, FileMode.Create);
        await file.CopyToAsync(stream);

        return filename;
    }

    public void Delete(string filename)
    {
        var path = Path.Combine(_uploadDir, filename);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
