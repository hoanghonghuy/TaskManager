using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskManager.Data.Models;
using System.Threading.Tasks;
using BCrypt.Net;

namespace TaskManager.Data.Data
{
    public static class SeedData
    {
        public static async System.Threading.Tasks.Task InitializeAsync(IServiceProvider serviceProvider)
        {
            using (var context = new ApplicationDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // 1. Tạo Roles nếu chưa tồn tại
                if (!await context.Roles.AnyAsync(r => r.Name == "Admin"))
                {
                    await context.Roles.AddAsync(new Role { Name = "Admin" });
                }
                if (!await context.Roles.AnyAsync(r => r.Name == "User"))
                {
                    await context.Roles.AddAsync(new Role { Name = "User" });
                }
                await context.SaveChangesAsync();

                // 2. Tạo Admin User nếu chưa tồn tại
                if (!await context.Users.AnyAsync(u => u.Username == "admin"))
                {
                    var adminUser = new User
                    {
                        Username = "admin",
                        Email = "admin@taskmanager.com",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), // Nhớ dùng mật khẩu mạnh hơn
                        FullName = "Administrator",
                        CreatedAt = DateTime.UtcNow
                    };
                    await context.Users.AddAsync(adminUser);
                    await context.SaveChangesAsync();

                    // 3. Gán Role "Admin" cho adminUser
                    var adminRole = await context.Roles.SingleAsync(r => r.Name == "Admin");
                    await context.UserRoles.AddAsync(new UserRole { User = adminUser, Role = adminRole });
                    await context.SaveChangesAsync();
                }
            }
        }
    }
}