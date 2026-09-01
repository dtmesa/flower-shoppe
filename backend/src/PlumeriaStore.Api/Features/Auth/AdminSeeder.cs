using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Options;

namespace PlumeriaStore.Api.Features.Auth;

public static class AdminSeeder
{
    /// <summary>
    /// Creates the single admin account from configuration the first time the app runs against an
    /// empty database. Config is a bootstrap value, not a permanent override - once the account
    /// exists, the admin can change their own username/password in-app (see AuthEndpoints'
    /// PUT /api/auth/admin), and this seeder leaves it alone on later startups so that change sticks.
    /// </summary>
    public static async Task SeedAdminUserAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlumeriaDbContext>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedOptions>>().Value;

        if (await db.AdminUsers.AnyAsync())
        {
            return;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(options.Password);
        db.AdminUsers.Add(new AdminUser { Username = options.Username, PasswordHash = passwordHash });
        await db.SaveChangesAsync();
    }
}
