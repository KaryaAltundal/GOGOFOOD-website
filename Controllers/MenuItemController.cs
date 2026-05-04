using GOGOFOOD.Data;
using GOGOFOOD.Models;
using GOGOFOOD.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace GOGOFOOD.Controllers
{
    public class MenuItemController : Controller
    {
        private readonly GogoFoodContext _context;
        private readonly GoogleMapsService _googleMapsService;
        private readonly LogService _logService;
        private readonly IWebHostEnvironment _environment;

        public MenuItemController(
            GogoFoodContext context,
            GoogleMapsService googleMapsService,
            LogService logService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _googleMapsService = googleMapsService;
            _logService = logService;
            _environment = environment;
        }

        [HttpGet]
        public async Task<IActionResult> Index(double? maxDistanceKm)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");
            var userId = HttpContext.Session.GetInt32("UserId");

            var menuItemsQuery = _context.MenuItems
                .Include(m => m.CustomizationOptions)
                .Include(m => m.MenuItemRatings)
                .Include(m => m.Caretaker)
                .ThenInclude(c => c.CaretakerRatings)
                .Where(m => !m.IsDeleted)
                .AsQueryable();

            if (role == "Caretaker")
            {
                if (caretakerId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                menuItemsQuery = menuItemsQuery.Where(m => m.CaretakerId == caretakerId.Value);
            }

            var menuItems = menuItemsQuery.ToList();

            var distances = new Dictionary<int, double>();

            if (role == "User" && maxDistanceKm != null && userId != null)
            {
                var user = _context.Users.FirstOrDefault(x => x.UserId == userId.Value);

                if (user == null || user.Latitude == null || user.Longitude == null)
                {
                    TempData["LocationWarning"] = "Please enter your address first.";
                    return RedirectToAction("Location", "User");
                }

                var filteredItems = new List<MenuItem>();

                foreach (var item in menuItems)
                {
                    if (item.Caretaker == null ||
                        item.Caretaker.Latitude == null ||
                        item.Caretaker.Longitude == null)
                    {
                        continue;
                    }

                    var distance = await _googleMapsService.GetDistanceInKmAsync(
                        user.Latitude.Value,
                        user.Longitude.Value,
                        item.Caretaker.Latitude.Value,
                        item.Caretaker.Longitude.Value
                    );

                    if (distance != null)
                    {
                        distances[item.MenuItemId] = distance.Value;

                        if (distance.Value <= maxDistanceKm.Value)
                        {
                            filteredItems.Add(item);
                        }
                    }
                }

                menuItems = filteredItems;
            }

            ViewBag.MaxDistanceKm = maxDistanceKm;
            ViewBag.Distances = distances;

            return View(menuItems);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            if (role != "Caretaker")
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(MenuItem item, IFormFile? imageFile)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            if (role != "Caretaker")
            {
                return RedirectToAction("Index", "Home");
            }

            if (caretakerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadedImagePath = await SaveImage(imageFile);

                if (uploadedImagePath == null)
                {
                    ViewBag.ImageError = "Only JPG, JPEG, PNG, and WEBP images are allowed.";
                    return View(item);
                }

                item.ImageUrl = uploadedImagePath;
            }
            else
            {
                item.ImageUrl = string.Empty;
            }

            item.CaretakerId = caretakerId.Value;
            item.IsDeleted = false;

            _context.MenuItems.Add(item);
            _context.SaveChanges();

            _logService.AddLog(
                "Menu Item Created",
                "Caretaker created a new menu item: " + item.Name,
                HttpContext.Session.GetString("UserEmail"),
                role
            );

            return RedirectToAction("Index", "MenuItem");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            MenuItem? item = null;

            if (role == "Admin")
            {
                item = _context.MenuItems
                    .FirstOrDefault(x => x.MenuItemId == id && !x.IsDeleted);
            }
            else if (role == "Caretaker" && caretakerId != null)
            {
                item = _context.MenuItems
                    .FirstOrDefault(x =>
                        x.MenuItemId == id &&
                        x.CaretakerId == caretakerId.Value &&
                        !x.IsDeleted);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }

            if (item == null)
            {
                TempData["MenuMessage"] = "Menu item could not be found or you are not allowed to edit it.";
                return RedirectToAction("Index", "MenuItem");
            }

            return View(item);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(MenuItem item, IFormFile? imageFile)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            MenuItem? existingItem = null;

            if (role == "Admin")
            {
                existingItem = _context.MenuItems
                    .FirstOrDefault(x => x.MenuItemId == item.MenuItemId && !x.IsDeleted);
            }
            else if (role == "Caretaker" && caretakerId != null)
            {
                existingItem = _context.MenuItems
                    .FirstOrDefault(x =>
                        x.MenuItemId == item.MenuItemId &&
                        x.CaretakerId == caretakerId.Value &&
                        !x.IsDeleted);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }

            if (existingItem == null)
            {
                TempData["MenuMessage"] = "Menu item could not be found or you are not allowed to edit it.";
                return RedirectToAction("Index", "MenuItem");
            }

            existingItem.Name = item.Name;
            existingItem.Description = item.Description;
            existingItem.Price = item.Price;

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadedImagePath = await SaveImage(imageFile);

                if (uploadedImagePath == null)
                {
                    ViewBag.ImageError = "Only JPG, JPEG, PNG, and WEBP images are allowed.";
                    return View(existingItem);
                }

                existingItem.ImageUrl = uploadedImagePath;
            }

            _context.MenuItems.Update(existingItem);
            _context.SaveChanges();

            _logService.AddLog(
                "Menu Item Updated",
                role + " updated menu item: " + existingItem.Name,
                userEmail,
                role
            );

            TempData["MenuMessage"] = "Menu item updated successfully.";

            return RedirectToAction("Index", "MenuItem");
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            MenuItem? item = null;

            if (role == "Admin")
            {
                item = _context.MenuItems
                    .Include(x => x.Caretaker)
                    .FirstOrDefault(x => x.MenuItemId == id && !x.IsDeleted);
            }
            else if (role == "Caretaker" && caretakerId != null)
            {
                item = _context.MenuItems
                    .Include(x => x.Caretaker)
                    .FirstOrDefault(x =>
                        x.MenuItemId == id &&
                        x.CaretakerId == caretakerId.Value &&
                        !x.IsDeleted);
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }

            if (item == null)
            {
                TempData["MenuMessage"] = "Menu item could not be found or you are not allowed to delete it.";
                return RedirectToAction("Index", "MenuItem");
            }

            item.IsDeleted = true;

            _context.MenuItems.Update(item);
            _context.SaveChanges();

            _logService.AddLog(
                "Menu Item Deleted",
                role + " deleted menu item: " + item.Name,
                userEmail,
                role
            );

            TempData["MenuMessage"] = "Menu item deleted successfully.";

            return RedirectToAction("Index", "MenuItem");
        }

        private async Task<string?> SaveImage(IFormFile imageFile)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var extension = Path.GetExtension(imageFile.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                return null;
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "menu");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString() + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return "/images/menu/" + uniqueFileName;
        }
    }
}