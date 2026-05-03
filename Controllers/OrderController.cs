using GOGOFOOD.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GOGOFOOD.Controllers
{
    public class OrderController : Controller
    {
        private readonly GogoFoodContext _context;

        public OrderController(GogoFoodContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .ThenInclude(m => m.Caretaker)
                .OrderByDescending(o => o.OrderDate)
                .AsQueryable();

            if (role == "User" && userId != null)
            {
                ordersQuery = ordersQuery.Where(o => o.UserId == userId.Value);
            }

            var orders = ordersQuery.ToList();

            return View(orders);
        }
    }
}