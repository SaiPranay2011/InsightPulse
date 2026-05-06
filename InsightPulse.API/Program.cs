using InsightPulse.API.Data;
using InsightPulse.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        opt => opt.CommandTimeout(30)
    );
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IMetricService, MetricService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IAlertService, AlertService>();
builder.Services.AddHttpClient<IAlertService, AlertService>();

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

// CORS Configuration - UPDATED WITH PRODUCTION IPv4
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:3000",              // Local development
                "http://localhost:3001",              // Local development alternate
                "http://3.22.167.100:3000",           // Production IPv4
                "https://3.22.167.100:3000",          // Production IPv4 HTTPS (if added)
                "http://3.22.167.100",                // Production IPv4 without port
                "https://3.22.167.100"                // Production IPv4 HTTPS without port
            )
            .AllowAnyMethod()                         // GET, POST, PUT, DELETE, OPTIONS, etc.
            .AllowAnyHeader()                         // Content-Type, Authorization, etc.
            .AllowCredentials();                      // For JWT tokens
    });
});

var app = builder.Build();

app.UseHttpsRedirection();

// CORS must be applied BEFORE Authentication and Authorization
app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint for Docker healthcheck
app.MapGet("/api/health", () => 
    Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Run();