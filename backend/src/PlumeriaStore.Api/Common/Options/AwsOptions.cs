namespace PlumeriaStore.Api.Common.Options;

public class AwsOptions
{
    public const string SectionName = "App:Aws";

    /// <summary>
    /// Region for the DynamoDB and S3 clients. Blank defers to the SDK's own resolution (the
    /// AWS_REGION Lambda sets for us), which is what a deployed environment wants.
    /// </summary>
    public string Region { get; set; } = string.Empty;
}
