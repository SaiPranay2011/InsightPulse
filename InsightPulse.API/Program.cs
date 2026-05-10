using InsightPulse.API.Data;
using InsightPulse.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.AddConsole();

// ── Services ──────────────────────────────────────────────────────────────────

builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        opt => opt.CommandTimeout(30)
    )
);

// Redis cache — fall back to in-memory if Redis connection string is missing.
// This prevents a startup crash when Redis is slow to become available.
var redisConn = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConn))
    builder.Services.AddStackExchangeRedisCache(o => o.Configuration = redisConn);
else
    builder.Services.AddDistributedMemoryCache();

// Application services
builder.Services.AddScoped<IAuthService,      AuthService>();
builder.Services.AddScoped<IMetricService,    MetricService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAlertService,     AlertService>();
builder.Services.AddHttpClient<IAlertService, AlertService>();

// ── JWT ───────────────────────────────────────────────────────────────────────
// Fail fast at startup with a clear message.
// A missing key causes an IDX10703 crash on every request, which the browser
// reports as a CORS error because no response headers are ever written.
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
// Read allowed origins from config so they can be overridden per environment
// without rebuilding the image (set Cors__AllowedOrigins__0, __1, ... in compose).
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

if (allowedOrigins == null || allowedOrigins.Length == 0)
    allowedOrigins = new[]
    {
        "http://localhost:3000",
        "http://localhost:3001",
        "http://3.22.167.100:3000",
        "https://3.22.167.100:3000",
        "http://3.22.167.100",
        "https://3.22.167.100"
    };

builder.Services.AddCors(options =>
    options.AddPolicy("AllowFrontend", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
    )
);

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== InsightPulse API starting — {Env} ===",
    app.Environment.EnvironmentName);

// ── Database migrations ───────────────────────────────────────────────────────
// Retried with backoff to handle the race where Postgres finishes its own
// startup after the API container has already started.
const int maxRetries = 6;
for (int attempt = 1; attempt <= maxRetries; attempt++)
{
    try
    {
        logger.LogInformation("Applying DB migrations (attempt {A}/{Max})...",
            attempt, maxRetries);
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
        logger.LogInformation("DB migrations complete.");
        break;
    }
    catch (Exception ex) when (attempt < maxRetries)
    {
        logger.LogWarning(ex, "Migration attempt {A} failed — retrying in 5 s...", attempt);
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "All {Max} migration attempts failed. " +
            "The API will start but requests that need the DB will fail.", maxRetries);
    }
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
// ORDER IS CRITICAL:
//   1. UseCors first — OPTIONS preflight must get CORS headers before any
//      other middleware (especially auth) can short-circuit the request.
//   2. No UseHttpsRedirection — TLS is terminated at the nginx reverse proxy.
//      Adding it here would cause preflight OPTIONS requests to be 307-redirected
//      before CORS headers are written, producing the "No CORS header" browser error.

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health endpoint — used by the Docker healthcheck and load balancers.
// Must be registered AFTER UseCors so CORS headers are present on this route too.
app.MapGet("/api/health", () =>
    Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

logger.LogInformation("=== InsightPulse API ready ===");
app.Run();