namespace PlumeriaStore.Api.Features.Inventory;

/// <summary>A stored photo, ready to write back to the client.</summary>
public sealed record StoredFile(Stream Content, string ContentType, long? Length);

public interface IFileStorage
{
    /// <summary>Validates and stores the upload, returning the generated filename to record on the item.</summary>
    Task<string> StoreAsync(IFormFile file, CancellationToken cancellationToken = default);

    /// <summary>Null when the file isn't there - a photo removed out of band shouldn't 500 the catalog.</summary>
    Task<StoredFile?> OpenAsync(string filename, CancellationToken cancellationToken = default);

    Task DeleteAsync(string filename, CancellationToken cancellationToken = default);
}
