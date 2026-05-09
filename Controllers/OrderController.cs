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

        public IActionResult Index(string? searchText, int page = 1)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");

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

            if (role == "Caretaker" && caretakerId != null)
            {
                ordersQuery = ordersQuery.Where(o =>
                    o.OrderItems != null &&
                    o.OrderItems.Any(oi =>
                        oi.MenuItem != null &&
                        oi.MenuItem.CaretakerId == caretakerId.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                ordersQuery = ordersQuery.Where(o =>
                    o.Status.Contains(searchText) ||
                    o.User.Name.Contains(searchText) ||
                    o.User.Email.Contains(searchText) ||
                    o.OrderItems.Any(oi => oi.MenuItem.Name.Contains(searchText)));
            }

            int pageSize = 5;
            int totalOrders = ordersQuery.Count();
            int totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);

            if (page < 1)
            {
                page = 1;
            }

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var orders = ordersQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SearchText = searchText;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(orders);
        }
    }
}