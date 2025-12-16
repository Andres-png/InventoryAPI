using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InventoryApi.Data;
using InventoryApi.Services;
using InventoryApi.Extensions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Railway port
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddHealthChecks();

// CORS: frontend Vercel
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

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]
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

// 🔵 MySQL Railway (OPCIÓN 1)
var connectionString =
    $"Server={Environment.GetEnvironmentVariable("MYSQLHOST")};" +
    $"Port={Environment.GetEnvironmentVariable("MYSQLPORT")};" +
    $"Database={Environment.GetEnvironmentVariable("MYSQLDATABASE")};" +
    $"User={Environment.GetEnvironmentVariable("MYSQLUSER")};" +
    $"Password={Environment.GetEnvironmentVariable("MYSQLPASSWORD")};" +
    "SslMode=Required;";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        ServerVersion.AutoDetect(connectionString)
    ));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// CORS primero
app.UseCors("AllowFrontend");

// Migraciones + seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    DbSeeder.SeedAdmin(db);
}

app.UseHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Endpoints
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapHardwareEndpoints();
app.MapStockEndpoints();
app.MapAssignmentEndpoints();
app.MapExportEndpoints();

app.Run();
