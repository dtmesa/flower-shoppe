namespace PlumeriaStore.Api.Features.Auth;

/// <summary>
/// The single admin account. There is one row for it in DynamoDB rather than a table of users -
/// the app has only ever created one, and login/rename both go through this record.
/// </summary>
public class AdminUser
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}
