using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PlumeriaStore.Api.Tests.TestSupport;

/// <summary>
/// Boots the real app (Program.cs) end-to-end against an isolated DynamoDB table and S3 bucket in
/// the local emulators, so endpoint tests exercise routing, the validation filter, auth, and the
/// exception handler exactly as they run in production — not just the service layer.
/// </summary>
public sealed class PlumeriaApiFactory : WebApplicationFactory<Program>
{
    private readonly TestTable _table = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("App:Aws:Region", LocalAws.Region);

        builder.UseSetting("App:Dynamo:TableName", _table.TableName);
        builder.UseSetting("App:Dynamo:ServiceUrl", LocalAws.DynamoUrl);
        builder.UseSetting("App:Dynamo:AccessKey", LocalAws.AccessKey);
        builder.UseSetting("App:Dynamo:SecretKey", LocalAws.SecretKey);
        // The table is created by the fixture above, before the app starts.
        builder.UseSetting("App:Dynamo:CreateTableIfMissing", "false");

        builder.UseSetting("App:Storage:BucketName", _table.BucketName);
        builder.UseSetting("App:Storage:ServiceUrl", LocalAws.S3Url);
        builder.UseSetting("App:Storage:AccessKey", LocalAws.AccessKey);
        builder.UseSetting("App:Storage:SecretKey", LocalAws.SecretKey);
        builder.UseSetting("App:Storage:ForcePathStyle", "true");
        builder.UseSetting("App:Storage:CreateBucketIfMissing", "false");

        // Overrides whatever backend/.env provides (Program.cs loads it outside Lambda) so
        // integration tests never attempt a real SES call - EmailNotificationService already
        // treats a blank FromAddress as "notifications disabled" and skips without erroring.
        builder.UseSetting("EMAIL_FROM_ADDRESS", "");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _table.Dispose();
    }
}
