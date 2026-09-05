namespace PlumeriaStore.Api.Common.Options;

public class DynamoOptions
{
    public const string SectionName = "App:Dynamo";

    public string TableName { get; set; } = "PlumeriaStore";

    /// <summary>
    /// Set to point at a DynamoDB Local container (e.g. "http://dynamodb:8000"); blank means the
    /// real service. When set, <see cref="AccessKey"/>/<see cref="SecretKey"/> are used instead of
    /// the ambient credential chain, so a local emulator can't accidentally pick up real keys from
    /// backend/.env (which SES still reads from).
    /// </summary>
    public string ServiceUrl { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Creates the table on startup if it isn't there. For local development only - a deployed
    /// environment gets its table from the CloudFormation stack (backend/template.yaml), and the
    /// function's IAM role deliberately has no CreateTable permission.
    /// </summary>
    public bool CreateTableIfMissing { get; set; }
}
