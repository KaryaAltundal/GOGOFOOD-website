using GOGOFOOD.Data;
using GOGOFOOD.Models;

namespace GOGOFOOD.Services
{
    public class LogService
    {
        private readonly GogoFoodContext _context;

        public LogService(GogoFoodContext context)
        {
            _context = context;
        }

        public void AddLog(string eventType, string message, string? userEmail, string? userRole)
        {
            var log = new SystemLog
            {
                EventType = eventType,
                Message = message,
                UserEmail = userEmail,
                UserRole = userRole,
                CreatedAt = DateTime.Now
            };

            _context.SystemLogs.Add(log);
            _context.SaveChanges();
        }
    }
}