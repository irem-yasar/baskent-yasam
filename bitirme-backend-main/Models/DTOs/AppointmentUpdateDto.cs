using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using ApiProject.Models;

namespace ApiProject.Models.DTOs;

public class AppointmentUpdateDto
{
    // Randevu Bilgileri (güncelleme için opsiyonel)
    public DateTime? Date { get; set; }
    
    [JsonPropertyName("time")]
    public string? TimeString { get; set; }

    [JsonIgnore]
    public TimeSpan? Time
    {
        get
        {
            if (string.IsNullOrWhiteSpace(TimeString))
                return null;
            
            if (TimeSpan.TryParse(TimeString, out var timeSpan))
                return timeSpan;
            
            // "HH:mm" formatını destekle (örn: "14:30")
            if (TimeString.Contains(":") && TimeString.Split(':').Length == 2)
            {
                var parts = TimeString.Split(':');
                if (int.TryParse(parts[0], out var hours) && int.TryParse(parts[1], out var minutes))
                    return new TimeSpan(hours, minutes, 0);
            }
            
            return null;
        }
    }
    
    [MaxLength(200)]
    public string? Subject { get; set; }

    // Durum (Hoca için) - String olarak kabul et, enum'a çevir
    [JsonPropertyName("status")]
    public string? StatusString { get; set; }

    [JsonIgnore]
    public AppointmentStatus? Status
    {
        get
        {
            if (string.IsNullOrWhiteSpace(StatusString))
                return null;
            
            // Case-insensitive enum parsing
            if (Enum.TryParse<AppointmentStatus>(StatusString, ignoreCase: true, out var status))
                return status;
            
            return null;
        }
    }
    
    // Reddetme sebebi (opsiyonel)
    [MaxLength(500)]
    public string? RejectionReason { get; set; }
}

