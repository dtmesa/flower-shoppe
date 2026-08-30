namespace PlumeriaStore.Api.Common.Options;

public class UploadOptions
{
    public const string SectionName = "App:Upload";

    public string Directory { get; set; } = "./uploads";
    public long MaxSizeBytes { get; set; } = 5 * 1024 * 1024;
}
