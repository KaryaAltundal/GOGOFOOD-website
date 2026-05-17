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
            string? startDate,
            string? endDate,
            int page = 1)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");

            if (string.IsNullOrWhiteSpace(role))
            {
                return RedirectToAction("Login", "Account");
            }

            var ordersQuery = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(mi => mi.Caretaker)
                .AsQueryable();

            if (role == "User")
            {
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                ordersQuery = ordersQuery.Where(o => o.UserId == userId.Value);
            }
            else if (role == "Caretaker")
            {
                if (caretakerId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                ordersQuery = ordersQuery.Where(o =>
                    o.OrderItems.Any(oi =>
                        oi.MenuItem != null &&
                        oi.MenuItem.CaretakerId == caretakerId.Value));
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var search = searchText.Trim().ToLower();

                ordersQuery = ordersQuery.Where(o =>
                    (o.Status != null && o.Status.ToLower().Contains(search)) ||
                    (o.User != null && o.User.Name != null && o.User.Name.ToLower().Contains(search)) ||
                    (o.User != null && o.User.Email != null && o.User.Email.ToLower().Contains(search)) ||
                    o.OrderItems.Any(oi =>
                        (oi.MenuItem != null && oi.MenuItem.Name != null && oi.MenuItem.Name.ToLower().Contains(search)) ||
                        (oi.MenuItem != null && oi.MenuItem.Caretaker != null && oi.MenuItem.Caretaker.RestaurantName != null && oi.MenuItem.Caretaker.RestaurantName.ToLower().Contains(search)) ||
                        (oi.CustomizationName != null && oi.CustomizationName.ToLower().Contains(search))));
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

            if (!string.IsNullOrWhiteSpace(startDate) && DateTime.TryParse(startDate, out var parsedStartDate))
            {
                ordersQuery = ordersQuery.Where(o => o.OrderDate >= parsedStartDate);
            }

            if (!string.IsNullOrWhiteSpace(endDate) && DateTime.TryParse(endDate, out var parsedEndDate))
            {
                parsedEndDate = parsedEndDate.Date.AddDays(1).AddTicks(-1);
                ordersQuery = ordersQuery.Where(o => o.OrderDate <= parsedEndDate);
            }

            var filteredOrders = ordersQuery
                .OrderByDescending(o => o.OrderDate)
                .ToList();

            if (role == "Admin")
            {
                ViewBag.AdminTotalOrders = filteredOrders.Count;
                ViewBag.AdminTotalRevenue = filteredOrders.Sum(o => o.TotalPrice);
                ViewBag.AdminTotalItems = filteredOrders
                    .Where(o => o.OrderItems != null)
                    .Sum(o => o.OrderItems.Sum(oi => oi.Quantity));

                ViewBag.AdminAverageOrderValue = filteredOrders.Count > 0
                    ? filteredOrders.Average(o => o.TotalPrice)
                    : 0;

                ViewBag.AdminCompletedOrders = filteredOrders.Count(o => o.Status == "Completed");

                ViewBag.AdminTodayOrders = filteredOrders.Count(o =>
                    o.OrderDate.Date == DateTime.Today);

                ViewBag.AdminUniqueCustomers = filteredOrders
                    .Where(o => o.UserId != 0)
                    .Select(o => o.UserId)
                    .Distinct()
                    .Count();

                ViewBag.AdminUniqueRestaurants = filteredOrders
                    .SelectMany(o => o.OrderItems)
                    .Where(oi => oi.MenuItem?.Caretaker != null)
                    .Select(oi => oi.MenuItem!.Caretaker!.CaretakerId)
                    .Distinct()
                    .Count();
            }

            const int pageSize = 5;

            if (page < 1)
            {
                page = 1;
            }

            var totalItems = filteredOrders.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages < 1)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var pagedOrders = filteredOrders
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SearchText = searchText ?? "";
            ViewBag.StatusFilter = statusFilter ?? "";
            ViewBag.MinTotal = minTotal;
            ViewBag.MaxTotal = maxTotal;
            ViewBag.StartDate = startDate ?? "";
            ViewBag.EndDate = endDate ?? "";
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(pagedOrders);
        }

        [HttpGet]
        public IActionResult LiveCall(int orderId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");

            if (string.IsNullOrWhiteSpace(role))
            {
                return RedirectToAction("Login", "Account");
            }

            var order = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                        .ThenInclude(mi => mi.Caretaker)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null)
            {
                return RedirectToAction("Index", "Order");
            }

            if (order.Status != "Completed")
            {
                TempData["RatingMessage"] = "Live call is available only after the purchase is completed.";
                return RedirectToAction("Index", "Order");
            }

            var isOrderUser = role == "User" &&
                              userId != null &&
                              order.UserId == userId.Value;

            var isOrderCaretaker = role == "Caretaker" &&
                                   caretakerId != null &&
                                   order.OrderItems.Any(x =>
                                       x.MenuItem != null &&
                                       x.MenuItem.CaretakerId == caretakerId.Value);

            var isAdmin = role == "Admin";

            if (!isOrderUser && !isOrderCaretaker && !isAdmin)
            {
                return RedirectToAction("Index", "Order");
            }

            var catererNames = order.OrderItems
                .Where(x => x.MenuItem?.Caretaker != null)
                .Select(x => x.MenuItem!.Caretaker!.RestaurantName)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            ViewBag.Order = order;
            ViewBag.RoomName = "GOGOFOOD-Order-" + order.OrderId;
            ViewBag.CustomerName = order.User != null ? order.User.Name : "Customer";
            ViewBag.CatererNames = catererNames.Count > 0
                ? string.Join(", ", catererNames)
                : "Caterer";

            return View();
        }
    }
}