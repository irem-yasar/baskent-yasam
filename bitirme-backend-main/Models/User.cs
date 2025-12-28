using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApiProject.Models;

[Table("users")] // PostgreSQL'de küçük harf
public class User
{
    [Key]
    [Column("id")]
    public int Id { get; set; }
    
    [Required]
    [Column("role_id")]
    public int RoleId { get; set; }
    
    [Required]
    [MaxLength(100)]
    [Column("full_name")]
    public string FullName { get; set; } = string.Empty;
    
    [Required]
    [MaxLength(120)]
    [EmailAddress]
    [Column("email")]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [Column("password_hash", TypeName = "text")]
    public string PasswordHash { get; set; } = string.Empty;
    
    [MaxLength(40)]
    [Column("staff_id")]
    public string? StaffId { get; set; }
    
    // Navigation Properties
    public virtual ICollection<Appointment> StudentAppointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Appointment> TeacherAppointments { get; set; } = new List<Appointment>();
    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    
    // Helper property for Role enum (not mapped to DB)
    [NotMapped]
    public UserRole Role
    {
        get => (UserRole)RoleId;
        set => RoleId = (int)value;
    }
    
    // Helper property for Name (backward compatibility)
    [NotMapped]
    public string Name
    {
        get => FullName;
        set => FullName = value;
    }
    
    // Helper property for StudentNo (backward compatibility)
    [NotMapped]
    public string? StudentNo
    {
        get => StaffId;
        set => StaffId = value;
    }
}

public enum UserRole
{
    Student = 0,
    AcademicStaff = 1,
    Staff = 2,
    Admin = 3
}
