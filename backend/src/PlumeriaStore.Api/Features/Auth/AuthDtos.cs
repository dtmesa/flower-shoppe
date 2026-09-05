using PlumeriaStore.Api.Common.Validation;

namespace PlumeriaStore.Api.Features.Auth;

public record LoginRequest(string Username, string Password) : IValidatableRequest
{
    public void Validate(ValidationErrors errors)
    {
        errors.Required(nameof(Username), Username);
        errors.Required(nameof(Password), Password);
    }
}

public record LoginResponse(string Token, string Username);

public record AdminProfileResponse(string Username);

public record UpdateCredentialsRequest(
    string CurrentPassword,
    string NewUsername,
    // Optional - omit/leave blank to keep the current password and only change the username.
    string? NewPassword) : IValidatableRequest
{
    public void Validate(ValidationErrors errors)
    {
        errors.Required(nameof(CurrentPassword), CurrentPassword);
        errors.Required(nameof(NewUsername), NewUsername);
    }
}
