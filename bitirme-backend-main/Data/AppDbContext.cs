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
        public DbSet<Schedule> Schedules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User entity yapılandırması - Veritabanı şemasına göre (PostgreSQL case-sensitive)
            modelBuilder.Entity<User>(entity =>
            {
                // PostgreSQL'de tablo adı genellikle küçük harf
                entity.ToTable("users", (string?)null);
                
                // Primary Key - ÇOK ÖNEMLİ: PostgreSQL için açıkça belirt
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                
                // Diğer kolonlar - PostgreSQL case-sensitive olduğu için küçük harf
                entity.Property(e => e.RoleId)
                    .HasColumnName("role_id");
                
                // Foreign key constraint'i ignore et (role_id sadece integer değer)
                // Eğer roles tablosu varsa, bu satırı kaldırın ve roles tablosuna kayıt ekleyin
                entity.Ignore(e => e.Role); // NotMapped zaten var ama emin olmak için
                
                entity.Property(e => e.FullName)
                    .HasColumnName("full_name")
                    .HasMaxLength(100)
                    .IsRequired();
                
                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasMaxLength(120)
                    .IsRequired();
                
                entity.Property(e => e.PasswordHash)
                    .HasColumnName("password_hash")
                    .HasColumnType("text")
                    .IsRequired();
                
                entity.Property(e => e.StaffId)
                    .HasColumnName("staff_id")
                    .HasMaxLength(40)
                    .IsRequired(false);
            });
            // Appointment entity yapılandırması - Veritabanı şemasına göre
            modelBuilder.Entity<Appointment>(entity =>
            {
                entity.ToTable("Appointments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("Id");
                
                // Sadece StudentId ve TeacherId - UserId/UserId1 YOK!
                entity.Property(e => e.StudentId)
                    .HasColumnName("StudentId")
                    .IsRequired();
                
                entity.Property(e => e.TeacherId)
                    .HasColumnName("TeacherId")
                    .IsRequired();
                
                // Diğer kolonlar
                entity.Property(e => e.Date)
                    .HasColumnName("Date")
                    .IsRequired();
                
                entity.Property(e => e.Time)
                    .HasColumnName("Time")
                    .IsRequired();
                
                entity.Property(e => e.Subject)
                    .HasColumnName("Subject")
                    .HasMaxLength(200)
                    .IsRequired();
                
                entity.Property(e => e.RequestReason)
                    .HasColumnName("RequestReason")
                    .HasMaxLength(500);
                
                entity.Property(e => e.Status)
                    .HasColumnName("Status")
                    .HasConversion<string>()
                    .IsRequired();
                
                entity.Property(e => e.RejectionReason)
                    .HasColumnName("RejectionReason")
                    .HasMaxLength(500);
                
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("CreatedAt")
                    .IsRequired();
                
                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("UpdatedAt");
                
                // İlişki Ayarları - Sadece StudentId ve TeacherId
                // UserId/UserId1 kolonları ignore ediliyor (veritabanında varsa bile kullanılmayacak)
                entity.HasOne(a => a.Student)
                    .WithMany(u => u.StudentAppointments)
                    .HasForeignKey(a => a.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(a => a.Teacher)
                    .WithMany(u => u.TeacherAppointments)
                    .HasForeignKey(a => a.TeacherId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
            
            modelBuilder.Entity<Order>().Property(o => o.Status).HasConversion<string>();
            modelBuilder.Entity<Notification>().Property(n => n.Type).HasConversion<string>();
            
            // Schedule entity yapılandırması - Veritabanı şemasına göre
            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.ToTable("instructor_schedule");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.TeacherId)
                    .HasColumnName("instructor_id")
                    .IsRequired();
                
                entity.Property(e => e.DayOfWeek)
                    .HasColumnName("day_of_week")
                    .HasColumnType("smallint")
                    .IsRequired();
                
                // Day property'sini ignore et (NotMapped)
                entity.Ignore(e => e.Day);
                
                entity.Property(e => e.StartTime)
                    .HasColumnName("start_time")
                    .HasColumnType("time")
                    .IsRequired();
                
                // TimeSlot property'sini ignore et (NotMapped)
                entity.Ignore(e => e.TimeSlot);
                
                entity.Property(e => e.CourseName)
                    .HasColumnName("course_name")
                    .HasMaxLength(200)
                    .IsRequired(); // Veritabanında NOT NULL constraint var
                
                // Foreign key ilişkisi - users tablosuna
                // Eğer veritabanında foreign key constraint yoksa, EF Core'un oluşturmasını engelle
                // Çünkü veritabanında zaten tablo var ve constraint'ler farklı olabilir
                // Foreign key constraint'i ignore et - veritabanında zaten var olabilir
                // entity.HasOne(s => s.Teacher)
                //     .WithMany()
                //     .HasForeignKey(s => s.TeacherId)
                //     .OnDelete(DeleteBehavior.Cascade);
                
                // Index ekle (performans için)
                entity.HasIndex(e => e.TeacherId)
                    .HasDatabaseName("IX_instructor_schedule_instructor_id");
            });
        }
    }
}
