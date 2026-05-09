using GOGOFOOD.Data;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GOGOFOOD.Controllers
{
    public class AdminController : Controller
    {
        private readonly GogoFoodContext _context;

        public AdminController(GogoFoodContext context)
        {
            _context = context;
        }

        public IActionResult Logs(string? searchText, int page = 1)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var logsQuery = _context.SystemLogs
                .OrderByDescending(x => x.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                logsQuery = logsQuery.Where(x =>
                    x.EventType.Contains(searchText) ||
                    x.Message.Contains(searchText) ||
                    x.UserEmail.Contains(searchText) ||
                    x.UserRole.Contains(searchText));
            }

            int pageSize = 10;
            int totalLogs = logsQuery.Count();
            int totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize);

            if (page < 1)
            {
                page = 1;
            }

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var logs = logsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SearchText = searchText;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(logs);
        }
    }
}