using PlumeriaStore.Api.Common.Validation;

namespace PlumeriaStore.Api.Features.Inventory;

public static class InventoryEndpoints
{
    public static void MapInventoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/inventory");

        group.MapGet("/", (InventoryService service) => service.FindAllAsync())
            .AllowAnonymous();

        group.MapGet("/{id}", (string id, InventoryService service) => service.FindByIdAsync(id))
            .AllowAnonymous();

        group.MapPost("/", (InventoryItemCreateRequest request, InventoryService service) => service.CreateAsync(request))
            .AddEndpointFilter<ValidationFilter<InventoryItemCreateRequest>>();

        group.MapPut("/{id}", (string id, InventoryItemUpdateRequest request, InventoryService service) => service.UpdateAsync(id, request))
            .AddEndpointFilter<ValidationFilter<InventoryItemUpdateRequest>>();

        group.MapDelete("/{id}", async (string id, InventoryService service) =>
        {
            await service.DeleteAsync(id);
            return Results.NoContent();
        });

        // Minimal APIs attach antiforgery metadata to any endpoint with form binding; this app is a
        // stateless JWT Bearer API (no cookies, so no ambient-credential CSRF exposure) and doesn't
        // register antiforgery middleware, so form endpoints must opt out explicitly.
        group.MapPost("/{id}/images", (string id, IFormFile file, InventoryService service) => service.AddImageAsync(id, file))
            .DisableAntiforgery();

        group.MapDelete("/{id}/images/{imageId:int}", (string id, int imageId, InventoryService service) =>
            service.DeleteImageAsync(id, imageId));
    }
}
