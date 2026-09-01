using PlumeriaStore.Api.Common.Validation;

namespace PlumeriaStore.Api.Features.Inventory;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/categories");

        group.MapGet("/", (CategoryService service) => service.FindAllAsync())
            .AllowAnonymous();

        group.MapPost("/", (CategoryCreateRequest request, CategoryService service) => service.CreateAsync(request))
            .AddEndpointFilter<ValidationFilter<CategoryCreateRequest>>();

        group.MapPut("/{id:int}", (int id, CategoryUpdateRequest request, CategoryService service) =>
                service.UpdateAsync(id, request))
            .AddEndpointFilter<ValidationFilter<CategoryUpdateRequest>>();

        group.MapDelete("/{id:int}", async (int id, CategoryService service) =>
        {
            await service.DeleteAsync(id);
            return Results.NoContent();
        });
    }
}
