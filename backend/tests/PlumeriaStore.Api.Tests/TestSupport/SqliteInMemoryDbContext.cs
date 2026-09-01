using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PlumeriaStore.Api.Common.Data;
using PlumeriaStore.Api.Features.Inventory;

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

        // Mirrors CategorySeeder's defaults so InventoryService.GenerateIdAsync has something to
        // look up - production seeds these at startup, but this fixture never runs that path.
        Db.Categories.AddRange(
            new InventoryCategory { Kind = CategoryKind.TYPE, Name = "Cutting", Code = "C" },
            new InventoryCategory { Kind = CategoryKind.TYPE, Name = "Rooted Plant", Code = "R" },
            new InventoryCategory { Kind = CategoryKind.COLOR, Name = "Red", Code = "R" },
            new InventoryCategory { Kind = CategoryKind.COLOR, Name = "Pink", Code = "P" },
            new InventoryCategory { Kind = CategoryKind.COLOR, Name = "Yellow/White", Code = "Y" },
            new InventoryCategory { Kind = CategoryKind.SIZE, Name = "Small", Code = "S" },
            new InventoryCategory { Kind = CategoryKind.SIZE, Name = "Medium", Code = "M" },
            new InventoryCategory { Kind = CategoryKind.SIZE, Name = "Large", Code = "L" });
        Db.SaveChanges();
    }

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
