using InsightPulse.API.Data;
using InsightPulse.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add logging to see CORS being configured
builder.Logging.AddConsole();

// Add services
builder.Services.AddControllers();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        opt => opt.CommandTimeout(30)
    );
});

// Redis Cache
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Application Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMetricService, MetricService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddHttpClient<IAlertService, AlertService>();

// JWT Configuration
var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.ASCII.GetBytes(jwtSettings["Key"]);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// CORS - CRITICAL: Must be added BEFORE building app
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
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
            .AllowCredentials();
    });
});

var app = builder.Build();

// Log that app is starting
var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("=== Application Starting ===");
logger.LogInformation($"Environment: {app.Environment.EnvironmentName}");

// CRITICAL: UseCors MUST come BEFORE UseAuthentication and UseHttpsRedirection
logger.LogInformation("Applying CORS policy");
app.UseCors("AllowFrontend");

// Only redirect to HTTPS in production with a real cert; not needed behind a reverse proxy
if (!app.Environment.IsDevelopment())
{
    // app.UseHttpsRedirection(); // Disabled: HTTPS is handled by the reverse proxy (nginx)
}

logger.LogInformation("Setting up authentication");
app.UseAuthentication();

logger.LogInformation("Setting up authorization");
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Health check endpoint (used by Docker and monitoring)
app.MapGet("/api/health", () =>
{
    return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
});

logger.LogInformation("=== Application Ready ===");
logger.LogInformation("CORS enabled for: http://3.22.167.100:3000, http://localhost:3000");

app.Run();