using GOGOFOOD.Data;
using GOGOFOOD.Models;
using GOGOFOOD.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GOGOFOOD.Controllers
{
    public class MenuItemController : Controller
    {
        private readonly GogoFoodContext _context;
        private readonly GoogleMapsService _googleMapsService;
        private readonly LogService _logService;
        private readonly IWebHostEnvironment _environment;

        private const long MaxImageSize = 2 * 1024 * 1024; // 2 MB

        private readonly string[] _allowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private readonly string[] _allowedContentTypes =
        {
            "image/jpeg", "image/png", "image/webp"
        };

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
        public async Task<IActionResult> Index(double? maxDistanceKm, string? searchText, int page = 1)
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

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var search = searchText.ToLower();

                menuItems = menuItems
                    .Where(m =>
                        m.Name.ToLower().Contains(search) ||
                        m.Description.ToLower().Contains(search) ||
                        (m.Caretaker != null &&
                         m.Caretaker.RestaurantName.ToLower().Contains(search)))
                    .ToList();
            }

            int pageSize = 6;
            int totalItems = menuItems.Count;
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            menuItems = menuItems
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.MaxDistanceKm = maxDistanceKm;
            ViewBag.Distances = distances;
            ViewBag.SearchText = searchText;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

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
        [ValidateAntiForgeryToken]
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

            if (item.Price <= 0)
            {
                ViewBag.ImageError = "Price must be greater than 0.";
                return View(item);
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadResult = await SaveImage(imageFile);

                if (!uploadResult.Success)
                {
                    ViewBag.ImageError = uploadResult.ErrorMessage;
                    return View(item);
                }

                item.ImageUrl = uploadResult.ImageUrl ?? string.Empty;
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
        [ValidateAntiForgeryToken]
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

            if (item.Price <= 0)
            {
                ViewBag.ImageError = "Price must be greater than 0.";
                return View(existingItem);
            }

            existingItem.Name = item.Name;
            existingItem.Description = item.Description;
            existingItem.Price = item.Price;

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadResult = await SaveImage(imageFile);

                if (!uploadResult.Success)
                {
                    ViewBag.ImageError = uploadResult.ErrorMessage;
                    return View(existingItem);
                }

                existingItem.ImageUrl = uploadResult.ImageUrl ?? existingItem.ImageUrl;
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
        [ValidateAntiForgeryToken]
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

        private async Task<(bool Success, string? ImageUrl, string ErrorMessage)> SaveImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return (false, null, "Please select an image file.");
            }

            if (imageFile.Length > MaxImageSize)
            {
                return (false, null, "Image size must be smaller than 2 MB.");
            }

            var extension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(extension) || !_allowedExtensions.Contains(extension))
            {
                return (false, null, "Only JPG, JPEG, PNG and WEBP image files are allowed.");
            }

            if (!_allowedContentTypes.Contains(imageFile.ContentType.ToLowerInvariant()))
            {
                return (false, null, "Invalid image type. Please upload JPG, PNG or WEBP.");
            }

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "menu");

            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            var uniqueFileName = Guid.NewGuid().ToString("N") + extension;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            return (true, "/images/menu/" + uniqueFileName, string.Empty);
        }
    }
}