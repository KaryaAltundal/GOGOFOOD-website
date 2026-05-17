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
        private readonly IConfiguration _configuration;

        private const long MaxImageSize = 2 * 1024 * 1024;

        private readonly string[] _allowedExtensions =
        {
            ".jpg", ".jpeg", ".png", ".webp"
        };

        private readonly string[] _allowedContentTypes =
        {
            "image/jpeg", "image/png", "image/webp"
        };

        private readonly string[] _categories =
        {
            "Starter",
            "Main Course",
            "Dessert",
            "Drink"
        };

        public MenuItemController(
            GogoFoodContext context,
            GoogleMapsService googleMapsService,
            LogService logService,
            IWebHostEnvironment environment,
            IConfiguration configuration)
        {
            _context = context;
            _googleMapsService = googleMapsService;
            _logService = logService;
            _environment = environment;
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            double? maxDistanceKm,
            string? searchText,
            string? categoryFilter,
            int? selectedCatererId,
            int page = 1)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");
            var userId = HttpContext.Session.GetInt32("UserId");

            var menuItemsQuery = _context.MenuItems
                .Include(m => m.CustomizationOptions)
                .Include(m => m.MenuItemRatings)
                .Include(m => m.Caretaker)
                    .ThenInclude(c => c!.CaretakerRatings)
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

            if (selectedCatererId != null && role == "User")
            {
                menuItemsQuery = menuItemsQuery.Where(m => m.CaretakerId == selectedCatererId.Value);
            }

            var menuItems = menuItemsQuery.ToList();

            var distances = new Dictionary<int, double>();
            var catererDistances = new Dictionary<int, double>();
            var nearbyRestaurants = new List<object>();

            double? userLatitude = null;
            double? userLongitude = null;

            if (role == "User" && userId != null)
            {
                var user = _context.Users.FirstOrDefault(x => x.UserId == userId.Value);

                if (user != null && user.Latitude != null && user.Longitude != null)
                {
                    userLatitude = user.Latitude.Value;
                    userLongitude = user.Longitude.Value;

                    var catererGroupsForDistance = menuItems
                        .Where(x =>
                            x.Caretaker != null &&
                            x.Caretaker.Latitude != null &&
                            x.Caretaker.Longitude != null)
                        .GroupBy(x => x.Caretaker!.CaretakerId)
                        .ToList();

                    foreach (var group in catererGroupsForDistance)
                    {
                        var firstItem = group.First();
                        var caterer = firstItem.Caretaker!;

                        var distance = await _googleMapsService.GetDistanceInKmAsync(
                            user.Latitude.Value,
                            user.Longitude.Value,
                            caterer.Latitude!.Value,
                            caterer.Longitude!.Value
                        );

                        if (distance != null)
                        {
                            catererDistances[caterer.CaretakerId] = distance.Value;

                            foreach (var item in group)
                            {
                                distances[item.MenuItemId] = distance.Value;
                            }
                        }
                    }

                    if (maxDistanceKm != null)
                    {
                        menuItems = menuItems
                            .Where(item =>
                                item.CaretakerId != null &&
                                catererDistances.ContainsKey(item.CaretakerId.Value) &&
                                catererDistances[item.CaretakerId.Value] <= maxDistanceKm.Value)
                            .ToList();
                    }
                }
                else if (maxDistanceKm != null)
                {
                    TempData["LocationWarning"] = "Please enter your address first.";
                    return RedirectToAction("Location", "User");
                }
            }

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var search = searchText.ToLower();

                menuItems = menuItems
                    .Where(m =>
                        m.Name.ToLower().Contains(search) ||
                        m.Description.ToLower().Contains(search) ||
                        m.Category.ToLower().Contains(search) ||
                        (m.Caretaker != null &&
                         (
                             m.Caretaker.RestaurantName.ToLower().Contains(search) ||
                             m.Caretaker.Name.ToLower().Contains(search) ||
                             m.Caretaker.City.ToLower().Contains(search) ||
                             m.Caretaker.District.ToLower().Contains(search)
                         )))
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(categoryFilter))
            {
                menuItems = menuItems
                    .Where(m => m.Category == categoryFilter)
                    .ToList();
            }

            nearbyRestaurants = menuItems
                .Where(x =>
                    x.Caretaker != null &&
                    x.Caretaker.Latitude != null &&
                    x.Caretaker.Longitude != null)
                .GroupBy(x => x.Caretaker!.CaretakerId)
                .Select(group =>
                {
                    var firstItem = group.First();
                    var caretaker = firstItem.Caretaker!;

                    double? restaurantDistance = null;

                    if (catererDistances.ContainsKey(caretaker.CaretakerId))
                    {
                        restaurantDistance = catererDistances[caretaker.CaretakerId];
                    }

                    return new
                    {
                        RestaurantName = caretaker.RestaurantName,
                        Address = caretaker.Address,
                        Latitude = caretaker.Latitude,
                        Longitude = caretaker.Longitude,
                        Distance = restaurantDistance,
                        MenuCount = group.Count()
                    };
                })
                .ToList<object>();

            var caterersForCards = menuItems
                .Where(x => x.Caretaker != null)
                .Select(x => x.Caretaker!)
                .GroupBy(x => x.CaretakerId)
                .Select(x => x.First())
                .OrderBy(x => x.RestaurantName)
                .ToList();

            var catererMenuCounts = menuItems
                .Where(x => x.CaretakerId != null)
                .GroupBy(x => x.CaretakerId!.Value)
                .ToDictionary(x => x.Key, x => x.Count());

            Caretaker? selectedCaterer = null;

            if (selectedCatererId != null)
            {
                selectedCaterer = _context.Caretakers
                    .Include(x => x.CaretakerRatings)
                    .FirstOrDefault(x => x.CaretakerId == selectedCatererId.Value);
            }

            if (role == "User")
            {
                menuItems = menuItems
                    .OrderBy(x => Array.IndexOf(_categories, x.Category))
                    .ThenBy(x => x.Name)
                    .ToList();

                ViewBag.CurrentPage = 1;
                ViewBag.TotalPages = 1;
            }
            else
            {
                int pageSize = 8;
                int totalItems = menuItems.Count;
                int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

                if (page < 1)
                {
                    page = 1;
                }

                if (totalPages > 0 && page > totalPages)
                {
                    page = totalPages;
                }

                menuItems = menuItems
                    .OrderBy(x => x.Caretaker != null ? x.Caretaker.RestaurantName : "")
                    .ThenBy(x => Array.IndexOf(_categories, x.Category))
                    .ThenBy(x => x.Name)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.CurrentPage = page;
                ViewBag.TotalPages = totalPages;
            }

            ViewBag.MaxDistanceKm = maxDistanceKm;
            ViewBag.Distances = distances;
            ViewBag.CatererDistances = catererDistances;
            ViewBag.SearchText = searchText;
            ViewBag.CategoryFilter = categoryFilter;
            ViewBag.SelectedCatererId = selectedCatererId;
            ViewBag.SelectedCaterer = selectedCaterer;
            ViewBag.Categories = _categories;
            ViewBag.CaterersForCards = caterersForCards;
            ViewBag.CatererMenuCounts = catererMenuCounts;

            ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;
            ViewBag.NearbyRestaurants = nearbyRestaurants;
            ViewBag.UserLatitude = userLatitude;
            ViewBag.UserLongitude = userLongitude;

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

            ViewBag.Categories = _categories;

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
                ViewBag.Categories = _categories;
                ViewBag.ImageError = "Price per person must be greater than 0.";
                return View(item);
            }

            if (!_categories.Contains(item.Category))
            {
                item.Category = "Main Course";
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadResult = await SaveImage(imageFile);

                if (!uploadResult.Success)
                {
                    ViewBag.Categories = _categories;
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
                "Catering Menu Created",
                "Caretaker created a new catering menu option: " + item.Name,
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
                TempData["MenuMessage"] = "Catering menu option could not be found or you are not allowed to edit it.";
                return RedirectToAction("Index", "MenuItem");
            }

            ViewBag.Categories = _categories;

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
                TempData["MenuMessage"] = "Catering menu option could not be found or you are not allowed to edit it.";
                return RedirectToAction("Index", "MenuItem");
            }

            if (item.Price <= 0)
            {
                ViewBag.Categories = _categories;
                ViewBag.ImageError = "Price per person must be greater than 0.";
                return View(existingItem);
            }

            existingItem.Name = item.Name;
            existingItem.Description = item.Description;
            existingItem.Price = item.Price;
            existingItem.Category = _categories.Contains(item.Category) ? item.Category : "Main Course";

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadResult = await SaveImage(imageFile);

                if (!uploadResult.Success)
                {
                    ViewBag.Categories = _categories;
                    ViewBag.ImageError = uploadResult.ErrorMessage;
                    return View(existingItem);
                }

                existingItem.ImageUrl = uploadResult.ImageUrl ?? existingItem.ImageUrl;
            }

            _context.MenuItems.Update(existingItem);
            _context.SaveChanges();

            _logService.AddLog(
                "Catering Menu Updated",
                role + " updated catering menu option: " + existingItem.Name,
                userEmail,
                role
            );

            TempData["MenuMessage"] = "Catering menu option updated successfully.";

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
                TempData["MenuMessage"] = "Catering menu option could not be found or you are not allowed to delete it.";
                return RedirectToAction("Index", "MenuItem");
            }

            item.IsDeleted = true;

            _context.MenuItems.Update(item);
            _context.SaveChanges();

            _logService.AddLog(
                "Catering Menu Deleted",
                role + " deleted catering menu option: " + item.Name,
                userEmail,
                role
            );

            TempData["MenuMessage"] = "Catering menu option deleted successfully.";

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