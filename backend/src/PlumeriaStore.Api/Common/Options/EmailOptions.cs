namespace PlumeriaStore.Api.Common.Options;

/// <summary>
/// Bound by hand in Program.cs rather than via GetSection(), since the values come from flat
/// EMAIL_*/AWS_* keys (backend/.env) rather than a nested App: config section.
/// </summary>
public class EmailOptions
{
    public string FromAddress { get; set; } = string.Empty;
    public string Region { get; set; } = "us-west-2";
}
