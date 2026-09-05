using Microsoft.Extensions.Options;
using PlumeriaStore.Api.Common.Options;

namespace PlumeriaStore.Api.Features.Auth;

public static class AdminSeeder
{
    /// <summary>
    /// Creates the single admin account from configuration the first time the app runs against an
    /// empty table. Config is a bootstrap value, not a permanent override - once the account
    /// exists, the admin can change their own username/password in-app (see AuthEndpoints'
    /// PUT /api/auth/admin), and this seeder leaves it alone on later startups so that change sticks.
    /// </summary>
    public static async Task SeedAdminUserAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var admins = scope.ServiceProvider.GetRequiredService<AdminRepository>();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AdminSeedOptions>>().Value;

        if (await admins.FindAsync() is not null)
        {
            return;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(options.Password);

        // Conditional, because on Lambda this runs on every cold start and two of those can
        // overlap - the write must not overwrite an account the admin has already renamed.
        await admins.TryCreateAsync(new AdminUser { Username = options.Username, PasswordHash = passwordHash });
    }
}
