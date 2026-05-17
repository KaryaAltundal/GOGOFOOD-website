using GOGOFOOD.Data;
using GOGOFOOD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GOGOFOOD.Controllers
{
    public class OrderController : Controller
    {
        private readonly GogoFoodContext _context;

        public OrderController(GogoFoodContext context)
        {
            _context = context;
        }

        public IActionResult Index(
            string? searchText,
            string? statusFilter,
            decimal? minTotal,
            decimal? maxTotal,
            DateTime? startDate,
            DateTime? endDate,
            int page = 1)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            IQueryable<Order> ordersQuery = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(m => m.Caretaker)
                .OrderByDescending(o => o.OrderDate);

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
                var search = searchText.ToLower();

                ordersQuery = ordersQuery.Where(o =>
                    o.Status.ToLower().Contains(search) ||
                    (o.User != null && o.User.Name.ToLower().Contains(search)) ||
                    (o.User != null && o.User.Email.ToLower().Contains(search)) ||
                    o.OrderItems.Any(oi =>
                        oi.MenuItem != null &&
                        (
                            oi.MenuItem.Name.ToLower().Contains(search) ||
                            oi.MenuItem.Description.ToLower().Contains(search) ||
                            (
                                oi.MenuItem.Caretaker != null &&
                                oi.MenuItem.Caretaker.RestaurantName.ToLower().Contains(search)
                            )
                        )));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                ordersQuery = ordersQuery.Where(o => o.Status == statusFilter);
            }

            if (minTotal != null)
            {
                ordersQuery = ordersQuery.Where(o => o.TotalPrice >= minTotal.Value);
            }

            if (maxTotal != null)
            {
                ordersQuery = ordersQuery.Where(o => o.TotalPrice <= maxTotal.Value);
            }

            if (startDate != null)
            {
                var start = startDate.Value.Date;
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= start);
            }

            if (endDate != null)
            {
                var end = endDate.Value.Date.AddDays(1);
                ordersQuery = ordersQuery.Where(o => o.OrderDate < end);
            }

            if (role == "Admin")
            {
                var allAdminOrders = ordersQuery.ToList();

                ViewBag.AdminTotalOrders = allAdminOrders.Count;

                ViewBag.AdminTotalRevenue = allAdminOrders.Sum(o => o.TotalPrice);

                ViewBag.AdminTotalItems = allAdminOrders
                    .Where(o => o.OrderItems != null)
                    .Sum(o => o.OrderItems!.Sum(oi => oi.Quantity));

                ViewBag.AdminCompletedOrders = allAdminOrders
                    .Count(o => o.Status == "Completed");

                ViewBag.AdminTodayOrders = allAdminOrders
                    .Count(o => o.OrderDate.Date == DateTime.Today);

                ViewBag.AdminUniqueCustomers = allAdminOrders
                    .Where(o => o.User != null)
                    .Select(o => o.UserId)
                    .Distinct()
                    .Count();

                ViewBag.AdminUniqueRestaurants = allAdminOrders
                    .Where(o => o.OrderItems != null)
                    .SelectMany(o => o.OrderItems!)
                    .Where(oi => oi.MenuItem != null && oi.MenuItem.CaretakerId != null)
                    .Select(oi => oi.MenuItem!.CaretakerId)
                    .Distinct()
                    .Count();

                ViewBag.AdminAverageOrderValue = allAdminOrders.Count > 0
                    ? allAdminOrders.Average(o => o.TotalPrice)
                    : 0;
            }

            int pageSize = role == "Admin" ? 8 : 5;

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
            ViewBag.StatusFilter = statusFilter;
            ViewBag.MinTotal = minTotal;
            ViewBag.MaxTotal = maxTotal;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(orders);
        }
    }
}