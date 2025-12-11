using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using InventoryApi.Data;
using InventoryApi.Services;
using InventoryApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
builder.WebHost.UseUrls($"http://*:{port}");

builder.Services.AddHealthChecks();

// CORS: permitir sólo el frontend desplegado (más seguro que AllowAnyOrigin)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://inventory-frontend-sigma-lilac.vercel.app")
              .AllowAnyMethod()
              .AllowAnyHeader();
              // Si usas credenciales (cookies/Authorization por cookie) añade .AllowCredentials()
    });
});

// Jwt key (mantenlo aquí o muévelo a appsettings.json)
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKey123456789012345678901234567890";
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

// DbContext configurado vía DI
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=inventory.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplicar CORS lo antes posible para que incluso respuestas de error incluyan el header
app.UseCors("AllowFrontend");

// Seed inicial
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var db = services.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    InventoryApi.Data.DbSeeder.SeedAdmin(db);
}

app.UseHealthChecks("/health");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Mapear endpoints desde extensiones
app.MapAuthEndpoints();
app.MapUserEndpoints();
app.MapHardwareEndpoints();
app.MapStockEndpoints();
app.MapAssignmentEndpoints();
app.MapExportEndpoints();

app.Run();
