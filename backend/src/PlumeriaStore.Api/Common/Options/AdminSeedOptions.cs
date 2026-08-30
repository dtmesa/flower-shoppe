namespace PlumeriaStore.Api.Common.Options;

public class AdminSeedOptions
{
    public const string SectionName = "App:Admin";

    public string Username { get; set; } = "admin";
    public string Password { get; set; } = "admin";
}
