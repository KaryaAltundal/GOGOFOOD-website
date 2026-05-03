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

        public IActionResult Logs()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var logs = _context.SystemLogs
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            return View(logs);
        }
    }
}