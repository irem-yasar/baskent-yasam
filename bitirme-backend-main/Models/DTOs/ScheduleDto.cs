using System.Text.Json.Serialization;

namespace ApiProject.Models.DTOs
{
    public class ScheduleDto
    {
        [JsonPropertyName("day")]
        public string Day { get; set; } = string.Empty;

        [JsonPropertyName("timeSlot")]
        public string TimeSlot { get; set; } = string.Empty;
    }

    public class ScheduleResponseDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("teacherId")]
        public int TeacherId { get; set; }

        [JsonPropertyName("day")]
        public string Day { get; set; } = string.Empty;

        [JsonPropertyName("timeSlot")]
        public string TimeSlot { get; set; } = string.Empty;
    }

    public class ScheduleUpdateDto
    {
        [JsonPropertyName("slots")]
        public List<ScheduleDto> Slots { get; set; } = new List<ScheduleDto>();
    }
}

