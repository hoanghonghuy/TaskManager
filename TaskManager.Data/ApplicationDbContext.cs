using Microsoft.EntityFrameworkCore;
using TaskManager.Data.Models;

namespace TaskManager.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Models.Task> Tasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình mối quan hệ 1-nhiều
            // Một User có thể có nhiều Tasks
            modelBuilder.Entity<Models.Task>()
                .HasOne(t => t.User)         // Một Task chỉ thuộc về một User
                .WithMany(u => u.Tasks)      // Một User có nhiều Tasks
                .HasForeignKey(t => t.UserId) // Khóa ngoại là UserId
                .OnDelete(DeleteBehavior.Cascade); // Khi User bị xóa, các Task liên quan cũng bị xóa

            base.OnModelCreating(modelBuilder);
        }
    }
}