namespace PlumeriaStore.Api.Common.Options;

public class CorsOptions
{
    public const string SectionName = "App:Cors";

    public string AllowedOrigin { get; set; } = "http://localhost:5173";
}
