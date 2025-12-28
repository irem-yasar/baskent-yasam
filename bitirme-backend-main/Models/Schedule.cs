using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ApiProject.Models
{
    [Table("instructor_schedule")]
    public class Schedule
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("instructor_id")]
        public int TeacherId { get; set; }

        [Required]
        [Column("day_of_week", TypeName = "smallint")]
        public int DayOfWeek { get; set; } // 1=Pazartesi, 2=Salı, 3=Çarşamba, 4=Perşembe, 5=Cuma (veritabanı constraint'i)
        
        // Helper property - string formatı için (frontend ile uyumlu)
        [NotMapped]
        public string Day
        {
            get => DayOfWeek switch
            {
                1 => "Pzt",
                2 => "Sal",
                3 => "Çar",
                4 => "Per",
                5 => "Cum",
                _ => ""
            };
            set => DayOfWeek = value switch
            {
                "Pzt" => 1, // Veritabanında 1-5 arası olmalı
                "Sal" => 2,
                "Çar" => 3,
                "Per" => 4,
                "Cum" => 5,
                _ => 1 // Varsayılan Pazartesi
            };
        }

        [Required]
        [Column("start_time", TypeName = "time")]
        public TimeSpan StartTime { get; set; } // Veritabanında time tipi
        
        // Helper property - string formatı için (frontend ile uyumlu)
        [NotMapped]
        public string TimeSlot
        {
            get
            {
                // Başlangıç saatini "09.00" formatına çevir
                var start = $"{StartTime.Hours:D2}.{StartTime.Minutes:D2}";
                // Bitiş saatini hesapla (50 dakika sonra)
                var endTime = StartTime.Add(TimeSpan.FromMinutes(50));
                var end = $"{endTime.Hours:D2}.{endTime.Minutes:D2}";
                return $"{start}-{end}"; // "09.00-09.50" formatı
            }
            set
            {
                // "09.00-09.50" formatından başlangıç saatini çıkar
                if (string.IsNullOrEmpty(value))
                {
                    StartTime = TimeSpan.Zero;
                    return;
                }
                
                // "09.00-09.50" -> "09.00" -> "09:00" -> TimeSpan
                var startPart = value.Split('-')[0]; // "09.00"
                var timeString = startPart.Replace(".", ":"); // "09:00"
                
                if (TimeSpan.TryParse(timeString, out var timeSpan))
                {
                    StartTime = timeSpan;
                }
                else
                {
                    StartTime = TimeSpan.Zero;
                }
            }
        }

        [Required]
        [MaxLength(200)]
        [Column("course_name")]
        public string CourseName { get; set; } = string.Empty; // Veritabanında NOT NULL, boş string olabilir

        // Navigation property
        [JsonIgnore]
        public User? Teacher { get; set; }
        
        // Helper properties (veritabanında yok ama kodda kullanılabilir)
        [NotMapped]
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        
        [NotMapped]
        public DateTime? UpdatedAt { get; set; }
    }
}

