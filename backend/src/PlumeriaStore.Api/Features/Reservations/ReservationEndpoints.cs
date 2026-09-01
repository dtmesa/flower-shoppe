using PlumeriaStore.Api.Common.Validation;

namespace PlumeriaStore.Api.Features.Reservations;

public static class ReservationEndpoints
{
    public static void MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reservations");

        group.MapPost("/", (PickupRequestCreateRequest request, ReservationService service) => service.CreateAsync(request))
            .AddEndpointFilter<ValidationFilter<PickupRequestCreateRequest>>()
            .AllowAnonymous();

        group.MapGet("/", (ReservationService service) => service.FindAllAsync());

        group.MapPatch("/{id:int}/status", (int id, ReservationStatusUpdateRequest request, ReservationService service) =>
                service.UpdateStatusAsync(id, request.Status))
            .AddEndpointFilter<ValidationFilter<ReservationStatusUpdateRequest>>();

        group.MapPost("/{id:int}/complete", (int id, ReservationCompleteRequest request, ReservationService service) =>
            service.CompleteAsync(id, request.PermanentlyClear));

        group.MapDelete("/{id:int}", async (int id, ReservationService service) =>
        {
            await service.DeleteAsync(id);
            return Results.NoContent();
        });
    }
}
