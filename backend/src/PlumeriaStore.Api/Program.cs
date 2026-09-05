using System.Text;
using Amazon.Lambda.Serialization.SystemTextJson;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using PlumeriaStore.Api.Common.Data;
using PlumeriaStore.Api.Common.Errors;
using PlumeriaStore.Api.Common.Options;
using PlumeriaStore.Api.Common.Serialization;
using PlumeriaStore.Api.Features.Auth;
using PlumeriaStore.Api.Features.Inventory;
using PlumeriaStore.Api.Features.Notifications;
using PlumeriaStore.Api.Features.Reservations;

// Loads EMAIL_*/AWS_* secrets from backend/.env into the process environment - picked up below
// both by our own config reads and by the AWS SDK's default credential/region resolution. Skipped
// on Lambda, where there is no .env to find and configuration arrives as function environment
// variables set by the CloudFormation stack.
if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME")))
{
    DotNetEnv.Env.TraversePath().Load();
}

// CreateSlimBuilder rather than CreateBuilder: it drops the hosting features this API never uses
// (IIS integration, static web assets, hosting startup assemblies), which is what makes the
// Native AOT publish viable. Configuration sources and Kestrel are still there, so `dotnet run`
// locally behaves the same as before.
var builder = WebApplication.CreateSlimBuilder(args);

// Config sections resolved eagerly here are needed at startup wiring time (JWT signing key, CORS origin);
// they're also bound below via the Options pattern for the services that consume them per-request.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

// HMAC-SHA256 needs a 256-bit key, and a signing key that short (or empty, if appsettings.json
// didn't ship or App__Jwt__Secret wasn't set on the function) otherwise turns into a 500 on the
// first authenticated request, from inside the auth middleware, with nothing pointing at the cause.
if (Encoding.UTF8.GetByteCount(jwtOptions.Secret) < 32)
{
    throw new InvalidOperationException(
        $"{JwtOptions.SectionName}:Secret must be at least 32 bytes. Set it via App__Jwt__Secret.");
}

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection(AdminSeedOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));
builder.Services.Configure<AwsOptions>(builder.Configuration.GetSection(AwsOptions.SectionName));
builder.Services.Configure<DynamoOptions>(builder.Configuration.GetSection(DynamoOptions.SectionName));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));

// EMAIL_FROM_ADDRESS/App:Email:Region are flat/ad-hoc keys rather than a nested App: section
// (the former comes straight from backend/.env), so this is bound by hand instead of via
// GetSection().
builder.Services.Configure<EmailOptions>(options =>
{
    options.FromAddress = builder.Configuration["EMAIL_FROM_ADDRESS"] ?? string.Empty;
    options.Region = builder.Configuration["App:Email:Region"] ?? new EmailOptions().Region;
});

builder.Services.AddAwsClients();
builder.Services.AddSingleton<DynamoTable>();

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<IFileStorage, S3FileStorage>();
builder.Services.AddScoped<AdminRepository>();
builder.Services.AddScoped<InventoryRepository>();
builder.Services.AddScoped<CategoryRepository>();
builder.Services.AddScoped<ReservationRepository>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ReservationService>();

builder.Services.AddSingleton<IEmailSender, SesEmailSender>();
builder.Services.AddSingleton<EmailNotificationService>();

// The source-generated contract goes in front of the default resolver so it wins for the types it
// knows; anything else still falls through to reflection when running JIT-compiled.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret)),
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(corsOptions.Origins)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Turns the app into a Lambda handler when one is hosting it, and does nothing at all otherwise -
// so the same build runs under Kestrel for local development and tests.
builder.Services.AddAWSLambdaHosting(
    LambdaEventSource.HttpApi,
    new SourceGeneratorLambdaJsonSerializer<LambdaJsonSerializerContext>());

var app = builder.Build();

await app.Services.EnsureStorageAsync();
await app.Services.SeedAdminUserAsync();
await app.Services.SeedDefaultCategoriesAsync();

app.UseExceptionHandler();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapInventoryEndpoints();
app.MapCategoryEndpoints();
app.MapReservationEndpoints();
app.MapUploadEndpoints();

// Cheap liveness probe that touches nothing downstream - handy for confirming the
// function and its API Gateway route are wired up before pointing the frontend at them.
app.MapGet("/health", () => TypedResults.Ok(new HealthResponse("ok"))).AllowAnonymous();

app.Run();

public record HealthResponse(string Status);

public partial class Program
{
}
