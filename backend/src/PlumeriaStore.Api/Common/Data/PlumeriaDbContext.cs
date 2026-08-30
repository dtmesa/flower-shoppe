using Microsoft.EntityFrameworkCore;
using PlumeriaStore.Api.Features.Auth;
using PlumeriaStore.Api.Features.Inventory;
using PlumeriaStore.Api.Features.Reservations;

namespace PlumeriaStore.Api.Common.Data;

public class PlumeriaDbContext : DbContext
{
    public PlumeriaDbContext(DbContextOptions<PlumeriaDbContext> options) : base(options)
    {
    }

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();
    public DbSet<InventoryImage> InventoryImages => Set<InventoryImage>();
    public DbSet<AdminUser> AdminUsers => Set<AdminUser>();
    public DbSet<Reservation> Reservations => Set<Reservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PlumeriaDbContext).Assembly);
    }
}
