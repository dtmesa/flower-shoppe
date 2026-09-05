using System.Text.Json.Serialization;
using Amazon.Lambda.APIGatewayEvents;

namespace PlumeriaStore.Api.Common.Serialization;

/// <summary>
/// The API Gateway HTTP API envelope the Lambda runtime hands the function, and the one it hands
/// back. Separate from <see cref="AppJsonSerializerContext"/> because it belongs to the transport
/// rather than the API, and because these need the property names AWS sends, not camelCase ones.
/// </summary>
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyRequest))]
[JsonSerializable(typeof(APIGatewayHttpApiV2ProxyResponse))]
public partial class LambdaJsonSerializerContext : JsonSerializerContext
{
}
