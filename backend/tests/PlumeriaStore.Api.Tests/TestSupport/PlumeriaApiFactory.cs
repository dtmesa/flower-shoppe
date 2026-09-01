using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace PlumeriaStore.Api.Tests.TestSupport;

/// <summary>
/// Boots the real app (Program.cs) end-to-end against an isolated temp SQLite file and temp upload
/// directory, so endpoint tests exercise routing, the validation filter, auth, and the exception
/// handler exactly as they run in production — not just the service layer.
/// </summary>
public sealed class PlumeriaApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"plumeria-tests-{Guid.NewGuid()}.db");
    private readonly string _uploadDir = Path.Combine(Path.GetTempPath(), $"plumeria-tests-uploads-{Guid.NewGuid()}");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:Default", $"Data Source={_dbPath}");
        builder.UseSetting("App:Upload:Directory", _uploadDir);

        // Overrides whatever backend/.env provides (Program.cs loads it unconditionally) so
        // integration tests never attempt a real SES call - EmailNotificationService already
        // treats a blank FromAddress as "notifications disabled" and skips without erroring.
        builder.UseSetting("EMAIL_FROM_ADDRESS", "");
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Microsoft.Data.Sqlite pools native connection handles past DbContext disposal, so the
        // file stays locked until the pool is explicitly cleared.
        SqliteConnection.ClearAllPools();
        File.Delete(_dbPath);

        if (Directory.Exists(_uploadDir))
        {
            Directory.Delete(_uploadDir, recursive: true);
        }
    }
}
