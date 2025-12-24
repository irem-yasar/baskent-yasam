using ApiProject.Models;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // --- İŞTE BUNLAR EKSİK OLDUĞU İÇİN TABLOLAR GELMEDİ ---
        public DbSet<User> Users { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<OccupancyLog> OccupancyLogs { get; set; }
        public DbSet<Notification> Notifications { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enum Ayarları
            modelBuilder.Entity<User>().Property(u => u.Role).HasConversion<string>();
            modelBuilder.Entity<Appointment>().Property(a => a.Status).HasConversion<string>();
            modelBuilder.Entity<Order>().Property(o => o.Status).HasConversion<string>();
            modelBuilder.Entity<Notification>().Property(n => n.Type).HasConversion<string>();

            // İlişki Ayarları - Shadow property uyarısını önlemek için navigation property'leri belirt
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Student)
                .WithMany(u => u.StudentAppointments)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.Teacher)
                .WithMany(u => u.TeacherAppointments)
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
