using GOGOFOOD.Data;
using GOGOFOOD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GOGOFOOD.Controllers
{
    public class RatingController : Controller
    {
        private readonly GogoFoodContext _context;

        public RatingController(GogoFoodContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult AddMenuItemRating(int orderId, int menuItemId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "User" || userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var orderItem = _context.OrderItems
                .Include(x => x.Order)
                .Include(x => x.MenuItem)
                    .ThenInclude(x => x!.Caretaker)
                .FirstOrDefault(x =>
                    x.OrderId == orderId &&
                    x.MenuItemId == menuItemId &&
                    x.Order != null &&
                    x.Order.UserId == userId.Value);

            if (orderItem == null)
            {
                TempData["RatingMessage"] = "You can only rate menu items from your own completed orders.";
                return RedirectToAction("Index", "Order");
            }

            var existingRating = _context.MenuItemRatings
                .FirstOrDefault(x =>
                    x.OrderId == orderId &&
                    x.MenuItemId == menuItemId &&
                    x.UserId == userId.Value);

            var ratingCount = _context.MenuItemRatings.Count(x => x.MenuItemId == menuItemId);

            var ratingAverage = ratingCount > 0
                ? _context.MenuItemRatings
                    .Where(x => x.MenuItemId == menuItemId)
                    .Average(x => x.RatingValue)
                : 0;

            ViewBag.OrderId = orderId;
            ViewBag.MenuItemId = menuItemId;
            ViewBag.MenuItem = orderItem.MenuItem;
            ViewBag.ExistingRating = existingRating;
            ViewBag.RatingCount = ratingCount;
            ViewBag.RatingAverage = ratingAverage;

            return View(existingRating ?? new MenuItemRating());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddMenuItemRating(int orderId, int menuItemId, int ratingValue, string? comment)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "User" || userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ratingValue < 1 || ratingValue > 5)
            {
                TempData["RatingMessage"] = "Please select a rating between 1 and 5.";
                return RedirectToAction("AddMenuItemRating", new { orderId, menuItemId });
            }

            var orderItem = _context.OrderItems
                .Include(x => x.Order)
                .FirstOrDefault(x =>
                    x.OrderId == orderId &&
                    x.MenuItemId == menuItemId &&
                    x.Order != null &&
                    x.Order.UserId == userId.Value);

            if (orderItem == null)
            {
                TempData["RatingMessage"] = "You can only rate menu items from your own completed orders.";
                return RedirectToAction("Index", "Order");
            }

            var existingRating = _context.MenuItemRatings
                .FirstOrDefault(x =>
                    x.OrderId == orderId &&
                    x.MenuItemId == menuItemId &&
                    x.UserId == userId.Value);

            if (existingRating == null)
            {
                var rating = new MenuItemRating
                {
                    OrderId = orderId,
                    MenuItemId = menuItemId,
                    UserId = userId.Value,
                    RatingValue = ratingValue,
                    Comment = comment ?? "",
                    CreatedAt = DateTime.Now
                };

                _context.MenuItemRatings.Add(rating);
            }
            else
            {
                existingRating.RatingValue = ratingValue;
                existingRating.Comment = comment ?? "";
                existingRating.CreatedAt = DateTime.Now;

                _context.MenuItemRatings.Update(existingRating);
            }

            _context.SaveChanges();

            TempData["RatingMessage"] = "Menu rating saved successfully.";
            return RedirectToAction("Reviews", new { menuItemId });
        }

        [HttpGet]
        public IActionResult AddCaretakerRating(int orderId, int caretakerId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "User" || userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var caretaker = _context.Caretakers
                .FirstOrDefault(x => x.CaretakerId == caretakerId && !x.IsDeleted);

            if (caretaker == null)
            {
                TempData["RatingMessage"] = "Caterer could not be found.";
                return RedirectToAction("Index", "Order");
            }

            var orderHasThisCaretaker = _context.OrderItems
                .Include(x => x.Order)
                .Include(x => x.MenuItem)
                .Any(x =>
                    x.OrderId == orderId &&
                    x.Order != null &&
                    x.Order.UserId == userId.Value &&
                    x.MenuItem != null &&
                    x.MenuItem.CaretakerId == caretakerId);

            if (!orderHasThisCaretaker)
            {
                TempData["RatingMessage"] = "You can only rate caterers from your own completed orders.";
                return RedirectToAction("Index", "Order");
            }

            var existingRating = _context.CaretakerRatings
                .FirstOrDefault(x =>
                    x.OrderId == orderId &&
                    x.CaretakerId == caretakerId &&
                    x.UserId == userId.Value);

            var ratingCount = _context.CaretakerRatings.Count(x => x.CaretakerId == caretakerId);

            var ratingAverage = ratingCount > 0
                ? _context.CaretakerRatings
                    .Where(x => x.CaretakerId == caretakerId)
                    .Average(x => x.RatingValue)
                : 0;

            ViewBag.OrderId = orderId;
            ViewBag.CaretakerId = caretakerId;
            ViewBag.Caretaker = caretaker;
            ViewBag.ExistingRating = existingRating;
            ViewBag.RatingCount = ratingCount;
            ViewBag.RatingAverage = ratingAverage;

            return View(existingRating ?? new CaretakerRating());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCaretakerRating(int orderId, int caretakerId, int ratingValue, string? comment)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var userId = HttpContext.Session.GetInt32("UserId");

            if (role != "User" || userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (ratingValue < 1 || ratingValue > 5)
            {
                TempData["RatingMessage"] = "Please select a rating between 1 and 5.";
                return RedirectToAction("AddCaretakerRating", new { orderId, caretakerId });
            }

            var orderHasThisCaretaker = _context.OrderItems
                .Include(x => x.Order)
                .Include(x => x.MenuItem)
                .Any(x =>
                    x.OrderId == orderId &&
                    x.Order != null &&
                    x.Order.UserId == userId.Value &&
                    x.MenuItem != null &&
                    x.MenuItem.CaretakerId == caretakerId);

            if (!orderHasThisCaretaker)
            {
                TempData["RatingMessage"] = "You can only rate caterers from your own completed orders.";
                return RedirectToAction("Index", "Order");
            }

            var existingRating = _context.CaretakerRatings
                .FirstOrDefault(x =>
                    x.OrderId == orderId &&
                    x.CaretakerId == caretakerId &&
                    x.UserId == userId.Value);

            if (existingRating == null)
            {
                var rating = new CaretakerRating
                {
                    OrderId = orderId,
                    CaretakerId = caretakerId,
                    UserId = userId.Value,
                    RatingValue = ratingValue,
                    Comment = comment ?? "",
                    CreatedAt = DateTime.Now
                };

                _context.CaretakerRatings.Add(rating);
            }
            else
            {
                existingRating.RatingValue = ratingValue;
                existingRating.Comment = comment ?? "";
                existingRating.CreatedAt = DateTime.Now;

                _context.CaretakerRatings.Update(existingRating);
            }

            _context.SaveChanges();

            TempData["RatingMessage"] = "Caterer rating saved successfully.";
            return RedirectToAction("Reviews", new { caretakerId });
        }

        [HttpGet]
        public IActionResult Reviews(int? menuItemId, int? caretakerId)
        {
            MenuItem? menuItem = null;
            Caretaker? caretaker = null;

            if (menuItemId != null)
            {
                menuItem = _context.MenuItems
                    .Include(x => x.Caretaker)
                    .FirstOrDefault(x => x.MenuItemId == menuItemId.Value && !x.IsDeleted);

                if (menuItem != null && menuItem.CaretakerId != null)
                {
                    caretakerId = menuItem.CaretakerId;
                }
            }

            if (caretakerId != null)
            {
                caretaker = _context.Caretakers
                    .FirstOrDefault(x => x.CaretakerId == caretakerId.Value && !x.IsDeleted);
            }

            if (menuItem == null && caretaker == null)
            {
                TempData["RatingMessage"] = "Review details could not be found.";
                return RedirectToAction("Index", "MenuItem");
            }

            var menuRatings = new List<MenuItemRating>();

            if (menuItem != null)
            {
                menuRatings = _context.MenuItemRatings
                    .Include(x => x.User)
                    .Where(x => x.MenuItemId == menuItem.MenuItemId)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
            }

            var caretakerRatings = new List<CaretakerRating>();

            if (caretaker != null)
            {
                caretakerRatings = _context.CaretakerRatings
                    .Include(x => x.User)
                    .Where(x => x.CaretakerId == caretaker.CaretakerId)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();
            }

            ViewBag.MenuItem = menuItem;
            ViewBag.Caretaker = caretaker;
            ViewBag.MenuRatings = menuRatings;
            ViewBag.CaretakerRatings = caretakerRatings;
            ViewBag.ShowMenuSection = menuItem != null;
            ViewBag.ShowCaretakerSection = caretaker != null;

            return View();
        }
    }
}