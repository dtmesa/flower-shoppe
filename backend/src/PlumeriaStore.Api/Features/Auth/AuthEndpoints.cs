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
    }
}
