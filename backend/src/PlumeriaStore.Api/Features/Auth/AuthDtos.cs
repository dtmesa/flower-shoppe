using System.ComponentModel.DataAnnotations;

namespace PlumeriaStore.Api.Features.Auth;

public record LoginRequest(
    [property: Required] string Username,
    [property: Required] string Password);

public record LoginResponse(string Token, string Username);

public record AdminProfileResponse(string Username);

public record UpdateCredentialsRequest(
    [property: Required] string CurrentPassword,
    [property: Required] string NewUsername,
    // Optional - omit/leave blank to keep the current password and only change the username.
    string? NewPassword);
