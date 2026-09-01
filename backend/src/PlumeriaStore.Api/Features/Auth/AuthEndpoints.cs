using System.Security.Claims;
using PlumeriaStore.Api.Common.Validation;

namespace PlumeriaStore.Api.Features.Auth;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (LoginRequest request, PlumeriaDbContext db, JwtTokenService jwtTokenService) =>
        {
            var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Username == request.Username);

            if (admin is null || !BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
            {
                return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid username or password");
            }

            var token = jwtTokenService.GenerateToken(admin.Username);
            return Results.Ok(new LoginResponse(token, admin.Username));
        })
        .AddEndpointFilter<ValidationFilter<LoginRequest>>()
        .AllowAnonymous();

        group.MapGet("/me", async (ClaimsPrincipal user, PlumeriaDbContext db) =>
        {
            var username = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var admin = await db.AdminUsers.AsNoTracking().FirstOrDefaultAsync(a => a.Username == username);

            return admin is null
                ? Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Not logged in")
                : Results.Ok(new AdminProfileResponse(admin.Username));
        });

        // Re-issues a token on success (rather than requiring a fresh login) since a username
        // change invalidates the old token's identity going forward.
        group.MapPut("/admin", async (UpdateCredentialsRequest request, ClaimsPrincipal user, PlumeriaDbContext db, JwtTokenService jwtTokenService) =>
        {
            var currentUsername = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var admin = await db.AdminUsers.FirstOrDefaultAsync(a => a.Username == currentUsername)
                ?? throw new NotFoundException("Admin account not found");

            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, admin.PasswordHash))
            {
                throw new BadRequestException("Current password is incorrect");
            }

            if (string.IsNullOrWhiteSpace(request.NewUsername))
            {
                throw new BadRequestException("Username cannot be blank");
            }

            if (request.NewPassword is not null && request.NewPassword.Length < 4)
            {
                throw new BadRequestException("New password must be at least 4 characters");
            }

            admin.Username = request.NewUsername.Trim();
            if (!string.IsNullOrEmpty(request.NewPassword))
            {
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }

            await db.SaveChangesAsync();

            var token = jwtTokenService.GenerateToken(admin.Username);
            return Results.Ok(new LoginResponse(token, admin.Username));
        })
        .AddEndpointFilter<ValidationFilter<UpdateCredentialsRequest>>();
    }
}
