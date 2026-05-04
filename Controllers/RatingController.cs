using GOGOFOOD.Data;
using GOGOFOOD.Models;
using GOGOFOOD.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GOGOFOOD.Controllers
{
    public class RatingController : Controller
    {
        private readonly GogoFoodContext _context;
        private readonly LogService _logService;

        public RatingController(GogoFoodContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        [HttpGet]
        public IActionResult AddMenuItemRating(int orderId, int menuItemId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .ThenInclude(m => m.Caretaker)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId.Value);

            if (order == null || order.Status != "Completed")
            {
                return RedirectToAction("Index", "Order");
            }

            var orderItem = order.OrderItems?
                .FirstOrDefault(x => x.MenuItemId == menuItemId);

            if (orderItem == null || orderItem.MenuItem == null)
            {
                return RedirectToAction("Index", "Order");
            }

            var existingRating = _context.MenuItemRatings.FirstOrDefault(x =>
                x.UserId == userId.Value &&
                x.OrderId == orderId &&
                x.MenuItemId == menuItemId);

            if (existingRating != null)
            {
                TempData["RatingMessage"] = "You already rated this menu item.";
                return RedirectToAction("Index", "Order");
            }

            ViewBag.OrderId = orderId;
            ViewBag.MenuItemId = menuItemId;
            ViewBag.MenuItemName = orderItem.MenuItem.Name;
            ViewBag.MenuItemDescription = orderItem.MenuItem.Description;
            ViewBag.MenuItemImage = orderItem.MenuItem.ImageUrl;
            ViewBag.RestaurantName = orderItem.MenuItem.Caretaker?.RestaurantName;
            ViewBag.CustomizationName = orderItem.CustomizationName;
            ViewBag.Quantity = orderItem.Quantity;
            ViewBag.UnitPrice = orderItem.UnitPrice;
            ViewBag.CustomizationPrice = orderItem.CustomizationPrice;

            return View();
        }

        [HttpPost]
        public IActionResult AddMenuItemRating(int orderId, int menuItemId, int ratingValue, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ratingValue < 1 || ratingValue > 5)
            {
                TempData["RatingMessage"] = "Rating must be between 1 and 5.";
                return RedirectToAction("AddMenuItemRating", new { orderId = orderId, menuItemId = menuItemId });
            }

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId.Value);

            if (order == null || order.Status != "Completed")
            {
                return RedirectToAction("Index", "Order");
            }

            var orderItemExists = order.OrderItems != null &&
                                  order.OrderItems.Any(x => x.MenuItemId == menuItemId);

            if (!orderItemExists)
            {
                return RedirectToAction("Index", "Order");
            }

            var existingRating = _context.MenuItemRatings.FirstOrDefault(x =>
                x.UserId == userId.Value &&
                x.OrderId == orderId &&
                x.MenuItemId == menuItemId);

            if (existingRating != null)
            {
                TempData["RatingMessage"] = "You already rated this menu item.";
                return RedirectToAction("Index", "Order");
            }

            var rating = new MenuItemRating
            {
                RatingValue = ratingValue,
                Comment = comment ?? string.Empty,
                CreatedAt = DateTime.Now,
                UserId = userId.Value,
                OrderId = orderId,
                MenuItemId = menuItemId
            };

            _context.MenuItemRatings.Add(rating);
            _context.SaveChanges();

            _logService.AddLog(
                "Rating Submitted",
                "Menu item rating submitted. MenuItemId: " + menuItemId + ", Rating: " + ratingValue,
                userEmail,
                userRole
            );

            TempData["RatingMessage"] = "Thank you! Your rating was submitted.";

            return RedirectToAction("Index", "Order");
        }

        [HttpGet]
        public IActionResult AddCaretakerRating(int orderId, int caretakerId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .ThenInclude(m => m.Caretaker)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId.Value);

            if (order == null || order.Status != "Completed")
            {
                return RedirectToAction("Index", "Order");
            }

            bool caretakerExistsInOrder = order.OrderItems != null &&
                order.OrderItems.Any(x =>
                    x.MenuItem != null &&
                    x.MenuItem.CaretakerId == caretakerId);

            if (!caretakerExistsInOrder)
            {
                return RedirectToAction("Index", "Order");
            }

            var existingRating = _context.CaretakerRatings.FirstOrDefault(x =>
                x.UserId == userId.Value &&
                x.OrderId == orderId &&
                x.CaretakerId == caretakerId);

            if (existingRating != null)
            {
                TempData["RatingMessage"] = "You already rated this restaurant.";
                return RedirectToAction("Index", "Order");
            }

            var caretaker = _context.Caretakers.FirstOrDefault(x => x.CaretakerId == caretakerId);

            var caretakerItems = order.OrderItems?
                .Where(x => x.MenuItem != null && x.MenuItem.CaretakerId == caretakerId)
                .ToList();

            ViewBag.OrderId = orderId;
            ViewBag.CaretakerId = caretakerId;
            ViewBag.CaretakerName = caretaker?.Name;
            ViewBag.RestaurantName = caretaker?.RestaurantName;
            ViewBag.RestaurantAddress = caretaker?.Address;
            ViewBag.CaretakerItems = caretakerItems;

            return View();
        }

        [HttpPost]
        public IActionResult AddCaretakerRating(int orderId, int caretakerId, int ratingValue, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ratingValue < 1 || ratingValue > 5)
            {
                TempData["RatingMessage"] = "Rating must be between 1 and 5.";
                return RedirectToAction("AddCaretakerRating", new { orderId = orderId, caretakerId = caretakerId });
            }

            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefault(o => o.OrderId == orderId && o.UserId == userId.Value);

            if (order == null || order.Status != "Completed")
            {
                return RedirectToAction("Index", "Order");
            }

            bool caretakerExistsInOrder = order.OrderItems != null &&
                order.OrderItems.Any(x =>
                    x.MenuItem != null &&
                    x.MenuItem.CaretakerId == caretakerId);

            if (!caretakerExistsInOrder)
            {
                return RedirectToAction("Index", "Order");
            }

            var existingRating = _context.CaretakerRatings.FirstOrDefault(x =>
                x.UserId == userId.Value &&
                x.OrderId == orderId &&
                x.CaretakerId == caretakerId);

            if (existingRating != null)
            {
                TempData["RatingMessage"] = "You already rated this restaurant.";
                return RedirectToAction("Index", "Order");
            }

            var rating = new CaretakerRating
            {
                RatingValue = ratingValue,
                Comment = comment ?? string.Empty,
                CreatedAt = DateTime.Now,
                UserId = userId.Value,
                OrderId = orderId,
                CaretakerId = caretakerId
            };

            _context.CaretakerRatings.Add(rating);
            _context.SaveChanges();

            _logService.AddLog(
                "Rating Submitted",
                "Caretaker rating submitted. CaretakerId: " + caretakerId + ", Rating: " + ratingValue,
                userEmail,
                userRole
            );

            TempData["RatingMessage"] = "Thank you! Your restaurant rating was submitted.";

            return RedirectToAction("Index", "Order");
        }
    }
}