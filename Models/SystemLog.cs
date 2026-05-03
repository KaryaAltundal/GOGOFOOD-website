namespace GOGOFOOD.Models
{
    public class SystemLog
    {
        public int SystemLogId { get; set; }

        public string EventType { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? UserEmail { get; set; }

        public string? UserRole { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}