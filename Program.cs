using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InventoryApi.Data;
using InventoryApi.Services;
using InventoryApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Puerto Railway
// =======================
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

// =======================
// Health
// =======================
builder.Services.AddHealthChecks();

// =======================
// CORS
// =======================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://inventory-frontend-sigma-lilac.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// =======================
// JWT
// =======================
var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY")
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

// =======================
// MySQL (Railway vars)
// =======================
var mysqlHost = Environment.GetEnvironmentVariable("MYSQL_HOST");
var mysqlPort = Environment.GetEnvironmentVariable("MYSQL_PORT");
var mysqlDb = Environment.GetEnvironmentVariable("MYSQL_DATABASE");
var mysqlUser = Environment.GetEnvironmentVariable("MYSQL_USER");
var mysqlPass = Environment.GetEnvironmentVariable("MYSQL_PASSWORD");

var connectionString =
    $"server={mysqlHost};" +
    $"port={mysqlPort};" +
    $"database={mysqlDb};" +
    $"user={mysqlUser};" +
    $"password={mysqlPass};";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    )
);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// =======================
// Middlewares
// =======================
app.UseCors("AllowFrontend");
app.UseHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();

// =======================
// Migraciones + Seed
// =======================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.SeedAdmin(db);
}

// =======================
// Swagger
// =======================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// =======================
// Endpoints
// =======================
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapHardwareEndpoints();
app.MapStockEndpoints();
app.MapAssignmentEndpoints();
app.MapExportEndpoints();

app.Run();
