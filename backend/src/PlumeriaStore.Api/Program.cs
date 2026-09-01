using System.Text;
using System.Text.Json.Serialization;
using Amazon.SimpleEmailV2;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlumeriaStore.Api.Common.Data;
using PlumeriaStore.Api.Common.Errors;
using PlumeriaStore.Api.Common.Options;
using PlumeriaStore.Api.Features.Auth;
using PlumeriaStore.Api.Features.Inventory;
using PlumeriaStore.Api.Features.Notifications;
using PlumeriaStore.Api.Features.Reservations;

// Loads EMAIL_*/AWS_* secrets from backend/.env into the process environment (no-op if the file
// isn't there, e.g. in a deployed environment where they're set directly) - picked up below both
// by our own config reads and by the AWS SDK's default credential/region resolution, which already
// checks AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY/AWS_REGION env vars on its own.
DotNetEnv.Env.TraversePath().Load();

var builder = WebApplication.CreateBuilder(args);

// Config sections resolved eagerly here are needed at startup wiring time (JWT signing key, CORS origin);
// they're also bound below via the Options pattern for the services that consume them per-request.
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var corsOptions = builder.Configuration.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.Configure<UploadOptions>(builder.Configuration.GetSection(UploadOptions.SectionName));
builder.Services.Configure<AdminSeedOptions>(builder.Configuration.GetSection(AdminSeedOptions.SectionName));
builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection(CorsOptions.SectionName));

// EMAIL_FROM_ADDRESS/App:Email:Region are flat/ad-hoc keys rather than a nested App: section
// (the former comes straight from backend/.env), so this is bound by hand instead of via
// GetSection().
builder.Services.Configure<EmailOptions>(options =>
{
    options.FromAddress = builder.Configuration["EMAIL_FROM_ADDRESS"] ?? string.Empty;
    options.Region = builder.Configuration["App:Email:Region"] ?? new EmailOptions().Region;
});

builder.Services.AddDbContext<PlumeriaDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddSingleton<JwtTokenService>();
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddScoped<InventoryService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<ReservationService>();

// Credentials come from AWS_ACCESS_KEY_ID/AWS_SECRET_ACCESS_KEY env vars (loaded above from
// backend/.env) via the SDK's own default resolution chain; region is passed explicitly from
// EmailOptions so startup doesn't depend on an AWS_REGION env var or ~/.aws/config existing too.
builder.Services.AddSingleton<IAmazonSimpleEmailServiceV2>(provider =>
{
    var region = provider.GetRequiredService<IOptions<EmailOptions>>().Value.Region;
    return new AmazonSimpleEmailServiceV2Client(Amazon.RegionEndpoint.GetBySystemName(region));
});
builder.Services.AddSingleton<IEmailSender, SesEmailSender>();
builder.Services.AddSingleton<EmailNotificationService>();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

var dbFilePath = new SqliteConnectionStringBuilder(builder.Configuration.GetConnectionString("Default")).DataSource;
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbFilePath)) ?? ".");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PlumeriaDbContext>();
    db.Database.Migrate();
}
await app.Services.SeedAdminUserAsync();
await app.Services.SeedDefaultCategoriesAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();

app.UseCors("Frontend");

var uploadOptions = app.Services.GetRequiredService<IOptions<UploadOptions>>().Value;
var uploadDir = Path.GetFullPath(uploadOptions.Directory);
Directory.CreateDirectory(uploadDir);
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadDir),
    RequestPath = "/uploads",
});

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapInventoryEndpoints();
app.MapCategoryEndpoints();
app.MapReservationEndpoints();

app.Run();

public partial class Program
{
}
