using InventoryApi.Models;
using BCrypt.Net;

namespace InventoryApi.Data
{
    public static class DbSeeder
    {
        public static void SeedAdmin(AppDbContext db)
        {
            if (!db.Users.Any(u => u.Username == "admin"))
            {
                db.Users.Add(new User
                {
                    Username = "admin",
                    Password = BCrypt.Net.BCrypt.HashPassword("admin123"),
                    Role = "admin"
                });
                db.SaveChanges();
            }
        }
    }
}