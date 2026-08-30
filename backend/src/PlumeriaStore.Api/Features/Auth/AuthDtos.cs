using System.ComponentModel.DataAnnotations;

namespace PlumeriaStore.Api.Features.Auth;

public record LoginRequest(
    [property: Required] string Username,
    [property: Required] string Password);

public record LoginResponse(string Token, string Username);
