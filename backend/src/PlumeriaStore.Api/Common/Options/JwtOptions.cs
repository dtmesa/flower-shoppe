namespace PlumeriaStore.Api.Common.Options;

public class JwtOptions
{
    public const string SectionName = "App:Jwt";

    public string Secret { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; } = 720;
}
