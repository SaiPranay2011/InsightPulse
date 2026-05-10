using InsightPulse.API.Data;
using InsightPulse.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// ── Services ─────────────────────────────────────────────────────────────────
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        opt => opt.CommandTimeout(30)
    )
);

// Redis cache
builder.Services.AddStackExchangeRedisCache(options =>
    options.Configuration = builder.Configuration.GetConnectionString("Redis")
);

// Application services
builder.Services.AddScoped<IAuthService,      AuthService>();
builder.Services.AddScoped<IMetricService,    MetricService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAlertService,     AlertService>();
builder.Services.AddHttpClient<IAlertService, AlertService>();

// ── JWT ───────────────────────────────────────────────────────────────────────
// Fail fast at startup — a missing or empty key causes a silent 500 on every
// request, which the browser reports as a CORS error because no response
// headers (including CORS headers) are ever written.
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtKey     = jwtSection["Key"] ?? string.Empty;

if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException(
        "Jwt__Key is not set. Pass it via the Jwt__Key environment variable.");

if (jwtKey.Length < 32)
    throw new InvalidOperationException(
        $"Jwt__Key is too short ({jwtKey.Length} chars). Minimum is 32 characters.");

var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken            = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer           = true,
        ValidIssuer              = jwtSection["Issuer"],
        ValidateAudience         = true,
        ValidAudience            = jwtSection["Audience"],
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.Zero
    };
});

// ── CORS ──────────────────────────────────────────────────────────────────────
// Must be registered before app.Build().
// WithOrigins + AllowCredentials is the correct combination for cookie/token auth.
// AllowAnyOrigin() cannot be combined with AllowCredentials() — keep explicit origins.
builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:3001",
                "http://3.22.167.100:3000",
                "https://3.22.167.100:3000",
                "http://3.22.167.100",
                "https://3.22.167.100"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
    )
);

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== Application Starting ===");
logger.LogInformation("Environment: {Env}", app.Environment.EnvironmentName);

// ── Database migrations ───────────────────────────────────────────────────────
// Running migrations here eliminates the fragile "docker exec dotnet ef" step
// in CI/CD. The container retries the healthcheck until the API is ready,
// so there is no race condition with the healthcheck probe.
try
{
    logger.LogInformation("Applying database migrations...");
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    logger.LogInformation("Database migrations applied successfully.");
}
catch (Exception ex)
{
    // Log the failure but keep the process alive so the health endpoint
    // can respond and the orchestrator can report a meaningful status.
    logger.LogError(ex, "Database migration failed. Check DB connectivity and credentials.");
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
// ORDER IS CRITICAL:
//   1. UseCors   — must be first so OPTIONS preflight requests get CORS headers
//                  before any other middleware can reject or redirect them.
//   2. UseAuthentication / UseAuthorization
//
// Do NOT call UseHttpsRedirection — TLS is terminated by nginx upstream;
// redirecting here would cause preflight OPTIONS requests to be 307-redirected
// before CORS headers are written, which is exactly what causes the browser
// "No 'Access-Control-Allow-Origin' header is present" error.

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health endpoint — used by Docker healthcheck
app.MapGet("/api/health", () =>
    Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

logger.LogInformation("=== Application Ready ===");
app.Run();