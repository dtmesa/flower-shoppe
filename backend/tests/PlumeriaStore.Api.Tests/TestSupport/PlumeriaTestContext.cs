using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Data;
using PlumeriaStore.Api.Common.Options;
using PlumeriaStore.Api.Features.Auth;
using PlumeriaStore.Api.Features.Inventory;
using PlumeriaStore.Api.Features.Reservations;

namespace PlumeriaStore.Api.Tests.TestSupport;

/// <summary>
/// The service layer wired up against an isolated table and bucket, for tests that exercise a
/// service directly rather than over HTTP. Categories are seeded to the same defaults production
/// starts with, so <see cref="InventoryService"/> has codes to build item IDs out of.
/// </summary>
public sealed class PlumeriaTestContext : IDisposable
{
    private readonly TestTable _table = new();

    public InventoryRepository Items { get; }
    public CategoryRepository Categories { get; }
    public ReservationRepository Requests { get; }
    public AdminRepository Admins { get; }
    public IFileStorage FileStorage { get; }

    public PlumeriaTestContext()
    {
        var dynamoOptions = Options.Create(new DynamoOptions { TableName = _table.TableName });
        var table = new DynamoTable(LocalAws.CreateDynamoClient(), dynamoOptions);

        Items = new InventoryRepository(table);
        Categories = new CategoryRepository(table);
        Requests = new ReservationRepository(table);
        Admins = new AdminRepository(table);

        FileStorage = new S3FileStorage(
            LocalAws.CreateS3Client(),
            Options.Create(new StorageOptions { BucketName = _table.BucketName }));

        CategorySeeder.SeedDefaultCategoriesAsync(Categories).GetAwaiter().GetResult();
    }

    public InventoryService NewInventoryService() => new(Items, Categories, Requests, FileStorage);

    public CategoryService NewCategoryService() => new(Categories);

    public ReservationService NewReservationService() =>
        new(Requests, Items, NoopEmailNotificationService.Create());

    public void Dispose() => _table.Dispose();
}
