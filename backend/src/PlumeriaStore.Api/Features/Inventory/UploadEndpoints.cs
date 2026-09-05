namespace PlumeriaStore.Api.Features.Inventory;

public static class UploadEndpoints
{
    public static void MapUploadEndpoints(this IEndpointRouteBuilder app)
    {
        // Photos used to be served straight off disk by the static-file middleware. They now live
        // in S3, and this streams them back under the same "/uploads/<filename>" path so nothing
        // on the frontend has to know where they moved to. Filenames are validated against the
        // shape this app generates before they ever reach an object key (see S3FileStorage).
        app.MapGet("/uploads/{filename}", async (string filename, IFileStorage storage, HttpContext context) =>
        {
            var file = await storage.OpenAsync(filename, context.RequestAborted);
            if (file is null)
            {
                return Results.NotFound();
            }

            // The filename contains a GUID and content never changes under it, so this is safe to
            // cache hard - which also keeps repeat catalog views off the function entirely.
            context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";

            return Results.Stream(file.Content, file.ContentType);
        })
        .AllowAnonymous();
    }
}
