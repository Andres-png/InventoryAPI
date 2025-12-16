using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InventoryApi.Data;
using InventoryApi.Services;
using InventoryApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// 🚀 Railway PORT
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Healthchecks
builder.Services.AddHealthChecks();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://inventory-frontend-sigma-lilac.vercel.app")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? Environment.GetEnvironmentVariable("JWT_KEY")
    ?? "SuperSecretKey123456789012345678901234567890";

builder.Services.AddSingleton<IJwtService>(new JwtService(jwtKey));

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();


// 🔥 MYSQL RAILWAY (CLAVE)
var mysqlUrl = Environment.GetEnvironmentVariable("MYSQL_URL");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        mysqlUrl,
        ServerVersion.AutoDetect(mysqlUrl),
        mySqlOptions =>
        {
            mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(10),
                errorNumbersToAdd: null
            );
        }
    )
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// CORS primero
app.UseCors("AllowFrontend");

// 🟢 Health check REAL
app.MapHealthChecks("/health");


// 🔥 MIGRACIONES (NO EnsureCreated)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.SeedAdmin(db);
}

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapHardwareEndpoints();
app.MapStockEndpoints();
app.MapAssignmentEndpoints();
app.MapExportEndpoints();

app.Run();
