using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Options;

namespace PlumeriaStore.Api.Features.Auth;

public static class AdminSeeder
{
    /// <summary>
    /// Upserts the single admin account from configuration on every startup, so rotating the
    /// password is just an env var change + restart.
    /// </summary>
    public static async Task SeedAdminUserAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlumeriaDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedOptions>>().Value;

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(options.Password);
        var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Username == options.Username);

        if (admin is null)
        {
            db.AdminUsers.Add(new AdminUser { Username = options.Username, PasswordHash = passwordHash });
        }
        else
        {
            admin.PasswordHash = passwordHash;
        }

        await db.SaveChangesAsync();
    }
}
