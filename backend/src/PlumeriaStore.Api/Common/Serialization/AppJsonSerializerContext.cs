using System.Text.Json.Serialization;
using PlumeriaStore.Api.Features.Auth;
using PlumeriaStore.Api.Features.Inventory;
using PlumeriaStore.Api.Features.Reservations;

namespace PlumeriaStore.Api.Common.Serialization;

/// <summary>
/// Every request and response shape the API binds, generated at compile time. Native AOT trims the
/// reflection System.Text.Json would otherwise use to discover them, so anything crossing the wire
/// has to be listed here - a type that isn't will fail to serialize at runtime, not at build time.
///
/// camelCase and by-name enums are set here rather than on the serializer options, because a
/// source-generated contract bakes those choices in when it is generated.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    UseStringEnumConverter = true)]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(AdminProfileResponse))]
[JsonSerializable(typeof(UpdateCredentialsRequest))]
[JsonSerializable(typeof(InventoryItemCreateRequest))]
[JsonSerializable(typeof(InventoryItemUpdateRequest))]
[JsonSerializable(typeof(InventoryItemResponse))]
[JsonSerializable(typeof(List<InventoryItemResponse>))]
[JsonSerializable(typeof(InventoryImageResponse))]
[JsonSerializable(typeof(CategoryCreateRequest))]
[JsonSerializable(typeof(CategoryUpdateRequest))]
[JsonSerializable(typeof(CategoryResponse))]
[JsonSerializable(typeof(List<CategoryResponse>))]
[JsonSerializable(typeof(PickupRequestCreateRequest))]
[JsonSerializable(typeof(PickupRequestLineItemInput))]
[JsonSerializable(typeof(ReservationStatusUpdateRequest))]
[JsonSerializable(typeof(ReservationCompleteRequest))]
[JsonSerializable(typeof(PickupRequestResponse))]
[JsonSerializable(typeof(List<PickupRequestResponse>))]
[JsonSerializable(typeof(ReservationLineResponse))]
[JsonSerializable(typeof(HealthResponse))]
public partial class AppJsonSerializerContext : JsonSerializerContext
{
}
