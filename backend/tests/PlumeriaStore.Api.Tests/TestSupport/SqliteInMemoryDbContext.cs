using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlumeriaStore.Api.Common.Data;

namespace PlumeriaStore.Api.Tests.TestSupport;

/// <summary>
/// A real Sqlite database (kept alive for the process lifetime via an open in-memory connection),
/// not the EF Core InMemory provider — that provider doesn't enforce relational behavior like the
/// reservation FK's ON DELETE SET NULL, which these tests need to exercise for real.
/// </summary>
public sealed class SqliteInMemoryDbContext : IDisposable
{
    private readonly SqliteConnection _connection;

    public PlumeriaDbContext Db { get; }

    public SqliteInMemoryDbContext()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<PlumeriaDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new PlumeriaDbContext(options);
        Db.Database.EnsureCreated();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
