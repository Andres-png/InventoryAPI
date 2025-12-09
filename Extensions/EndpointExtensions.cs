using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ClosedXML.Excel;
using InventoryApi.Data;
using InventoryApi.Models;
using InventoryApi.Dtos;
using InventoryApi.Services;

namespace InventoryApi.Extensions
{
    public static class EndpointExtensions
    {
        public static void MapAuthEndpoints(this WebApplication app)
        {
            app.MapPost("/api/auth/register", async (RegisterRequest req, AppDbContext db) =>
            {
                if (await db.Users.AnyAsync(u => u.Username == req.Username))
                    return Results.BadRequest(new { message = "Usuario ya existe" });

                var user = new User
                {
                    Username = req.Username,
                    Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
                    Role = "user"
                };

                db.Users.Add(user);
                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Usuario registrado exitosamente" });
            });

            app.MapPost("/api/auth/login", async (LoginRequest req, AppDbContext db, IJwtService jwtService) =>
            {
                var user = await db.Users.FirstOrDefaultAsync(u => u.Username == req.Username);
                if (user == null || !BCrypt.Net.BCrypt.Verify(req.Password, user.Password))
                    return Results.Unauthorized();

                var token = jwtService.GenerateToken(user);
                return Results.Ok(new { token, user = new { user.Id, user.Username, user.Role } });
            });
        }

        public static void MapUserEndpoints(this WebApplication app)
        {
            app.MapGet("/api/users", async (AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();
                var users = await db.Users.Select(u => new { u.Id, u.Username, u.Role, u.CreatedAt }).ToListAsync();
                return Results.Ok(users);
            }).RequireAuthorization();

            app.MapPost("/api/users", async (CreateUserRequest req, AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();
                if (await db.Users.AnyAsync(u => u.Username == req.Username))
                    return Results.BadRequest(new { message = "Usuario ya existe" });

                var newUser = new User
                {
                    Username = req.Username,
                    Password = BCrypt.Net.BCrypt.HashPassword(req.Password),
                    Role = "user"
                };

                db.Users.Add(newUser);
                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Usuario creado exitosamente" });
            }).RequireAuthorization();

            app.MapDelete("/api/users/{id}", async (int id, AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin"))
                    return Results.Forbid();

                var usr = await db.Users.FindAsync(id);
                if (usr == null)
                    return Results.NotFound(new { message = "Usuario no encontrado" });

                if (usr.Username == "admin")
                    return Results.BadRequest(new { message = "No puedes eliminar al usuario administrador principal" });

                db.Users.Remove(usr);
                await db.SaveChangesAsync();

                return Results.Ok(new { message = "Usuario eliminado exitosamente" });
            }).RequireAuthorization();

            app.MapPut("/api/users/{id}", async (int id, UpdateUserRequest req, AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();

                var usr = await db.Users.FindAsync(id);
                if (usr == null)
                    return Results.NotFound(new { message = "Usuario no encontrado" });

                if (!string.IsNullOrEmpty(req.Username))
                    usr.Username = req.Username;

                if (!string.IsNullOrEmpty(req.Password))
                    usr.Password = BCrypt.Net.BCrypt.HashPassword(req.Password);

                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Usuario actualizado exitosamente" });
            }).RequireAuthorization();
        }

        public static void MapHardwareEndpoints(this WebApplication app)
        {
            app.MapGet("/api/hardware", async (AppDbContext db, ClaimsPrincipal user) =>
            {
                var query = db.HardwareItems.Include(h => h.User).Include(h => h.AssignedTo).AsQueryable();

                var items = await query.Select(h => new
                {
                    h.Id,
                    h.EquipmentType,
                    h.Brand,
                    h.Model,
                    h.SerialNumber,
                    h.Status,
                    h.CreatedAt,
                    h.UpdatedAt,
                    User = new { h.User.Id, h.User.Username },
                    AssignedTo = h.AssignedTo != null ? new { h.AssignedTo.Id, h.AssignedTo.Username } : null
                }).ToListAsync();

                return Results.Ok(items);
            }).RequireAuthorization();

            app.MapPost("/api/hardware", async (CreateHardwareRequest req, AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();

                var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var item = new HardwareItem
                {
                    EquipmentType = req.EquipmentType,
                    Brand = req.Brand,
                    Model = req.Model,
                    SerialNumber = req.SerialNumber,
                    Status = string.IsNullOrEmpty(req.Status) ? "Activo" : req.Status,
                    UserId = userId,
                    AssignedToId = req.AssignedToId
                };

                db.HardwareItems.Add(item);
                await db.SaveChangesAsync();
                return Results.Ok(item);
            }).RequireAuthorization();

            app.MapPut("/api/hardware/{id}", async (int id, UpdateHardwareRequest req, AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();

                var item = await db.HardwareItems.FindAsync(id);

                if (item == null) return Results.NotFound(new { message = "Hardware no encontrado" });

                item.EquipmentType = req.EquipmentType ?? item.EquipmentType;
                item.Brand = req.Brand ?? item.Brand;
                item.Model = req.Model ?? item.Model;
                item.SerialNumber = req.SerialNumber ?? item.SerialNumber;
                item.Status = !string.IsNullOrEmpty(req.Status) ? req.Status : item.Status;
                item.AssignedToId = req.AssignedToId;
                item.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                return Results.Ok(item);
            }).RequireAuthorization();

            app.MapDelete("/api/hardware/{id}", async (int id, AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();

                var item = await db.HardwareItems.FindAsync(id);

                if (item == null) return Results.NotFound();

                db.HardwareItems.Remove(item);
                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Item eliminado" });
            }).RequireAuthorization();
        }

        public static void MapStockEndpoints(this WebApplication app)
        {
            app.MapGet("/api/stock", async (AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();

                var query = db.StockItems.Include(s => s.User).AsQueryable();

                var items = await query.Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.Category,
                    s.Quantity,
                    s.MinStock,
                    s.CreatedAt,
                    s.UpdatedAt,
                    User = new { s.User.Id, s.User.Username }
                }).ToListAsync();

                return Results.Ok(items);
            }).RequireAuthorization();

            app.MapPost("/api/stock", async (CreateStockRequest req, AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();

                var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var item = new InventoryApi.Models.StockItem
                {
                    Name = req.Name,
                    Category = req.Category,
                    Quantity = req.Quantity,
                    MinStock = req.MinStock,
                    UserId = userId
                };

                db.StockItems.Add(item);
                await db.SaveChangesAsync();
                return Results.Ok(item);
            }).RequireAuthorization();

            app.MapPut("/api/stock/{id}", async (int id, UpdateStockRequest req, AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();

                var item = await db.StockItems.FindAsync(id);

                if (item == null) return Results.NotFound();

                item.Name = req.Name; item.Category = req.Category; item.Quantity = req.Quantity;
                item.MinStock = req.MinStock;
                item.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                return Results.Ok(item);
            }).RequireAuthorization();

            app.MapDelete("/api/stock/{id}", async (int id, AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();

                var item = await db.StockItems.FindAsync(id);

                if (item == null) return Results.NotFound();

                db.StockItems.Remove(item);
                await db.SaveChangesAsync();
                return Results.Ok(new { message = "Item eliminado" });
            }).RequireAuthorization();
        }

        public static void MapAssignmentEndpoints(this WebApplication app)
        {
            app.MapGet("/api/assignments", async (AppDbContext db, ClaimsPrincipal user) =>
            {
                var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var isAdmin = user.IsInRole("admin");

                var query = db.Assignments
                    .Include(a => a.HardwareItem)
                    .Include(a => a.AssignedTo)
                    .Include(a => a.AssignedBy)
                    .AsQueryable();

                if (!isAdmin)
                {
                    query = query.Where(a => a.AssignedToId == userId && a.Status == "Activo");
                }

                var items = await query
                    .OrderByDescending(a => a.AssignmentDate)
                    .Select(a => new
                    {
                        a.Id,
                        a.AssignmentDate,
                        a.ExpectedReturnDate,
                        a.ActualReturnDate,
                        a.Status,
                        a.Notes,
                        a.RejectionReason,
                        HardwareItem = new { a.HardwareItem.Id, a.HardwareItem.EquipmentType, a.HardwareItem.Brand, a.HardwareItem.Model, a.HardwareItem.SerialNumber },
                        AssignedTo = new { a.AssignedTo.Id, a.AssignedTo.Username },
                        AssignedBy = new { a.AssignedBy.Id, a.AssignedBy.Username }
                    }).ToListAsync();

                return Results.Ok(items);
            }).RequireAuthorization();

            app.MapPost("/api/assignments", async (CreateAssignmentRequest req, AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();

                var userId = int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

                var hardware = await db.HardwareItems.FindAsync(req.HardwareItemId);
                if (hardware == null) return Results.NotFound(new { message = "Hardware no encontrado" });

                if (hardware.AssignedToId != null)
                    return Results.BadRequest(new { message = "Este equipo ya está asignado a otro usuario" });

                var userToAssign = await db.Users.FindAsync(req.AssignedToId);
                if (userToAssign == null) return Results.NotFound(new { message = "Usuario no encontrado" });

                hardware.AssignedToId = req.AssignedToId;
                hardware.UpdatedAt = DateTime.UtcNow;

                var assignment = new Assignment
                {
                    HardwareItemId = req.HardwareItemId,
                    AssignedToId = req.AssignedToId,
                    AssignedById = userId,
                    AssignmentDate = DateTime.UtcNow,
                    ExpectedReturnDate = req.ExpectedReturnDate,
                    Status = "Activo",
                    Notes = req.Notes
                };

                db.Assignments.Add(assignment);
                await db.SaveChangesAsync();
                return Results.Ok(assignment);
            }).RequireAuthorization();

            app.MapPut("/api/assignments/{id}/return", async (int id, AppDbContext db, ClaimsPrincipal user) =>
            {
                if (!user.IsInRole("admin")) return Results.Forbid();

                var assignment = await db.Assignments
                    .Include(a => a.HardwareItem)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (assignment == null) return Results.NotFound();

                assignment.ActualReturnDate = DateTime.UtcNow;
                assignment.Status = "Devuelto";

                assignment.HardwareItem.AssignedToId = null;
                assignment.HardwareItem.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
                return Results.Ok(assignment);
            }).RequireAuthorization();
        }

        public static void MapExportEndpoints(this WebApplication app)
        {
            app.MapGet("/api/export/hardware/excel", async (AppDbContext db) =>
            {
                var items = await db.HardwareItems
                    .Include(h => h.User)
                    .Include(h => h.AssignedTo)
                    .ToListAsync();

                using var workbook = new XLWorkbook();
                var ws = workbook.AddWorksheet("Hardware");

                ws.Cell(1, 1).Value = "ID";
                ws.Cell(1, 2).Value = "Tipo";
                ws.Cell(1, 3).Value = "Marca";
                ws.Cell(1, 4).Value = "Modelo";
                ws.Cell(1, 5).Value = "Serie";
                ws.Cell(1, 6).Value = "Estado";
                ws.Cell(1, 7).Value = "Propietario";
                ws.Cell(1, 8).Value = "Asignado";

                var row = 2;
                foreach (var item in items)
                {
                    ws.Cell(row, 1).Value = item.Id;
                    ws.Cell(row, 2).Value = item.EquipmentType;
                    ws.Cell(row, 3).Value = item.Brand;
                    ws.Cell(row, 4).Value = item.Model;
                    ws.Cell(row, 5).Value = item.SerialNumber;
                    ws.Cell(row, 6).Value = item.Status;
                    ws.Cell(row, 7).Value = item.User?.Username ?? "N/A";
                    ws.Cell(row, 8).Value = item.AssignedTo?.Username ?? "N/A";
                    row++;
                }

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                return Results.File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"hardware_{DateTime.Now:yyyy-MM-dd}.xlsx"
                );
            }).RequireAuthorization();

            app.MapGet("/api/export/stock/excel", async (AppDbContext db) =>
            {
                var items = await db.StockItems
                    .Include(h => h.User)
                    .ToListAsync();

                using var workbook = new XLWorkbook();
                var ws = workbook.AddWorksheet("Stock");

                ws.Cell(1, 1).Value = "ID";
                ws.Cell(1, 2).Value = "Nombre";
                ws.Cell(1, 3).Value = "Categoría";
                ws.Cell(1, 4).Value = "Cantidad";
                ws.Cell(1, 5).Value = "Stock Minimo";
                ws.Cell(1, 6).Value = "Usuario Asignado";

                var row = 2;
                foreach (var item in items)
                {
                    ws.Cell(row, 1).Value = item.Id;
                    ws.Cell(row, 2).Value = item.Name;
                    ws.Cell(row, 3).Value = item.Category;
                    ws.Cell(row, 4).Value = item.Quantity;
                    ws.Cell(row, 5).Value = item.MinStock;
                    ws.Cell(row, 6).Value = item.User?.Username;
                    row++;
                }

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);
                stream.Position = 0;

                return Results.File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"stock_{DateTime.Now:yyyy-MM-dd}.xlsx"
                );
            }).RequireAuthorization();
        }
    }
}