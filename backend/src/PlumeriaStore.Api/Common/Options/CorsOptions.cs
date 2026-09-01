namespace PlumeriaStore.Api.Common.Options;

public class CorsOptions
{
    public const string SectionName = "App:Cors";

    /// <summary>
    /// Comma-separated list, e.g. "http://localhost:5173,https://flower-shoppe.pages.dev" - lets
    /// local dev and the deployed frontend both reach the API without swapping config per environment.
    /// </summary>
    public string AllowedOrigins { get; set; } = "http://localhost:5173";

    public string[] Origins => AllowedOrigins
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
