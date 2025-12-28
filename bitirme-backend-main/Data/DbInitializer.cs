using ApiProject.Models;
using BCrypt.Net;
using Microsoft.EntityFrameworkCore;

namespace ApiProject.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            // 0. ROLER TABLOSUNA KAYIT EKLE (Foreign key constraint için)
            // Eğer roles tablosu varsa ve foreign key constraint varsa, bu kayıtlar gerekli
            try
            {
                // Raw SQL ile roles tablosuna kayıt ekle (eğer yoksa)
                // Farklı tablo yapıları için deneme
                try
                {
                    // Senaryo 1: id, name kolonları
                    context.Database.ExecuteSqlRaw(@"
                        INSERT INTO roles (id, name) 
                        VALUES (0, 'Student'), (1, 'AcademicStaff'), (2, 'Staff'), (3, 'Admin')
                        ON CONFLICT (id) DO NOTHING;
                    ");
                }
                catch
                {
                    try
                    {
                        // Senaryo 2: role_id, role_name kolonları
                        context.Database.ExecuteSqlRaw(@"
                            INSERT INTO roles (role_id, role_name) 
                            VALUES (0, 'Student'), (1, 'AcademicStaff'), (2, 'Staff'), (3, 'Admin')
                            ON CONFLICT (role_id) DO NOTHING;
                        ");
                    }
                    catch
                    {
                        // Senaryo 3: Sadece id kolonu
                        try
                        {
                            context.Database.ExecuteSqlRaw(@"
                                INSERT INTO roles (id) 
                                VALUES (0), (1), (2), (3)
                                ON CONFLICT (id) DO NOTHING;
                            ");
                        }
                        catch
                        {
                            // Roles tablosu farklı bir yapıda, manuel SQL gerekebilir
                        }
                    }
                }
            }
            catch
            {
                // Roles tablosu yoksa veya foreign key constraint yoksa, devam et
                // Bu durumda role_id sadece integer değer olarak kullanılır
            }

            // 1. KULLANICILARI EKLE
            if (!context.Users.Any())
            {
                var student = new User
                {
                    FullName = "Ali Ogrenci",
                    Email = "ali.ogrenci@baskent.edu.tr",
                    RoleId = 0, // Student
                    StaffId = "20231001",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("baskent123")
                };

                var teacher = new User
                {
                    FullName = "Mehmet Hoca",
                    Email = "hoca@baskent.edu.tr",
                    RoleId = 1, // AcademicStaff
                    StaffId = "hoca",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("baskent123")
                };

                var admin = new User
                {
                    FullName = "Admin",
                    Email = "admin@baskent.edu.tr",
                    RoleId = 3, // Admin
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123")
                };

                context.Users.AddRange(student, teacher, admin);
                context.SaveChanges();
            }

            // 2. MENÜYÜ EKLE
            if (!context.MenuItems.Any())
            {
                var menuItems = new MenuItem[]
                {
                    new MenuItem { Name = "Hamburger", Price = 150, Description = "Klasik", IsAvailable = true },
                    new MenuItem { Name = "Tost", Price = 50, Description = "Kaşarlı", IsAvailable = true },
                    new MenuItem { Name = "Çay", Price = 10, Description = "Taze", IsAvailable = true }
                };
                context.MenuItems.AddRange(menuItems);
                context.SaveChanges();
            }

            // 3. ÖRNEK SİPARİŞ EKLE (YENİ KISIM)
            if (!context.Orders.Any())
            {
                // Kullanıcıyı ve Yemeği bul
                var ali = context.Users.FirstOrDefault(u => u.Email == "ali.ogrenci@baskent.edu.tr");
                var burger = context.MenuItems.FirstOrDefault(m => m.Name == "Hamburger");

                if (ali != null && burger != null)
                {
                    var order = new Order
                    {
                        StudentId = ali.Id,
                        OrderDate = DateTime.Now,
                        Status = OrderStatus.Preparing, // Mutfakta hazırlanıyor görünsün
                        TotalAmount = burger.Price
                    };
                    
                    // Siparişi kaydet ki ID oluşsun
                    context.Orders.Add(order);
                    context.SaveChanges();

                    // Sipariş Detayını ekle
                    var orderItem = new OrderItem
                    {
                        OrderId = order.Id,
                        MenuItemId = burger.Id,
                        Quantity = 1,
                        Price = burger.Price
                    };
                    context.OrderItems.Add(orderItem);
                    context.SaveChanges();
                }
            }
        }
    }
}
