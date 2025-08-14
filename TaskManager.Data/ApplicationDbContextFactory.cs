using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace TaskManager.Data
{
    /// <summary>
    /// Class này giúp cho công cụ dòng lệnh (dotnet ef) có thể tạo DbContext
    /// mà không cần project khởi động (startup project). Nó sẽ tự tìm file appsettings.json
    /// và lấy chuỗi kết nối.
    /// </summary>
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Xây dựng đối tượng configuration để đọc file appsettings.json
            IConfigurationRoot configuration = new ConfigurationBuilder()
                // Thiết lập đường dẫn cơ sở là thư mục của project TaskManager.Data
                .SetBasePath(Directory.GetCurrentDirectory())
                // Chỉ đường đến file appsettings.json trong project Web
                .AddJsonFile(Path.Combine(Directory.GetCurrentDirectory(), "../TaskManager.Web/appsettings.json"))
                .Build();

            // Tạo đối tượng options cho DbContext
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Lấy chuỗi kết nối từ configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");

            // Sử dụng chuỗi kết nối
            optionsBuilder.UseSqlServer(connectionString);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}