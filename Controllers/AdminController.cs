using System.Globalization;
using GOGOFOOD.Data;
using GOGOFOOD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GOGOFOOD.Controllers
{
    public class AdminController : Controller
    {
        private readonly GogoFoodContext _context;

        public AdminController(GogoFoodContext context)
        {
            _context = context;
        }

        public IActionResult Logs(string? searchText, int page = 1)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var logsQuery = _context.SystemLogs
                .OrderByDescending(x => x.CreatedAt)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                logsQuery = logsQuery.Where(x =>
                    x.EventType.Contains(searchText) ||
                    x.Message.Contains(searchText) ||
                    x.UserEmail.Contains(searchText) ||
                    x.UserRole.Contains(searchText));
            }

            int pageSize = 10;
            int totalLogs = logsQuery.Count();
            int totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize);

            if (page < 1)
            {
                page = 1;
            }

            if (totalPages > 0 && page > totalPages)
            {
                page = totalPages;
            }

            var logs = logsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SearchText = searchText;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(logs);
        }

        [HttpGet]
        public IActionResult Users(string tab = "users", string? searchText = null)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            tab = string.IsNullOrWhiteSpace(tab) ? "users" : tab.ToLower();

            var usersQuery = _context.Users
                .Include(x => x.Caretaker)
                .OrderBy(x => x.IsDeleted)
                .ThenBy(x => x.Role)
                .ThenBy(x => x.Name)
                .AsQueryable();

            var caretakersQuery = _context.Caretakers
                .Include(x => x.User)
                .Include(x => x.MenuItems)
                .Include(x => x.CaretakerRatings)
                .OrderBy(x => x.IsDeleted)
                .ThenBy(x => x.RestaurantName)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var search = searchText.ToLower();

                usersQuery = usersQuery.Where(x =>
                    x.Name.ToLower().Contains(search) ||
                    x.Email.ToLower().Contains(search) ||
                    x.Role.ToLower().Contains(search) ||
                    x.City.ToLower().Contains(search) ||
                    x.District.ToLower().Contains(search) ||
                    x.Neighborhood.ToLower().Contains(search) ||
                    x.Street.ToLower().Contains(search) ||
                    x.AddressDetail.ToLower().Contains(search));

                caretakersQuery = caretakersQuery.Where(x =>
                    x.Name.ToLower().Contains(search) ||
                    x.RestaurantName.ToLower().Contains(search) ||
                    x.City.ToLower().Contains(search) ||
                    x.District.ToLower().Contains(search) ||
                    x.Neighborhood.ToLower().Contains(search) ||
                    x.Street.ToLower().Contains(search) ||
                    x.AddressDetail.ToLower().Contains(search) ||
                    x.Address.ToLower().Contains(search) ||
                    (x.User != null && x.User.Email.ToLower().Contains(search)));
            }

            var users = usersQuery.ToList();
            var caretakers = caretakersQuery.ToList();

            ViewBag.ActiveTab = tab;
            ViewBag.SearchText = searchText;
            ViewBag.Users = users;
            ViewBag.Caretakers = caretakers;

            ViewBag.TotalUsers = users.Count;
            ViewBag.ActiveUsers = users.Count(x => !x.IsDeleted);
            ViewBag.DeletedUsers = users.Count(x => x.IsDeleted);

            ViewBag.TotalCaretakers = caretakers.Count;
            ViewBag.ActiveCaretakers = caretakers.Count(x => !x.IsDeleted);
            ViewBag.DeletedCaretakers = caretakers.Count(x => x.IsDeleted);

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateUser(
            int userId,
            string name,
            string email,
            string role,
            string city,
            string district,
            string neighborhood,
            string street,
            string addressDetail,
            string latitude,
            string longitude,
            string tab = "users",
            string? searchText = null)
        {
            if (!IsCurrentUserAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users
                .Include(x => x.Caretaker)
                .FirstOrDefault(x => x.UserId == userId);

            if (user == null)
            {
                TempData["AdminUsersError"] = "User could not be found.";
                return RedirectToAction("Users", new { tab, searchText });
            }

            var emailExists = _context.Users
                .Any(x => x.Email == email && x.UserId != userId);

            if (emailExists)
            {
                TempData["AdminUsersError"] = "This email is already used by another account.";
                return RedirectToAction("Users", new { tab, searchText });
            }

            user.Name = name ?? string.Empty;
            user.Email = email ?? string.Empty;
            user.Role = role ?? "User";
            user.City = city ?? string.Empty;
            user.District = district ?? string.Empty;
            user.Neighborhood = neighborhood ?? string.Empty;
            user.Street = street ?? string.Empty;
            user.AddressDetail = addressDetail ?? string.Empty;
            user.Latitude = TryParseCoordinate(latitude);
            user.Longitude = TryParseCoordinate(longitude);

            if (user.Role == "Caretaker")
            {
                var caretaker = _context.Caretakers.FirstOrDefault(x => x.UserId == user.UserId);

                if (caretaker == null)
                {
                    caretaker = new Caretaker
                    {
                        Name = user.Name,
                        RestaurantName = user.Name + "'s Restaurant",
                        Address = BuildFullAddress(
                            user.AddressDetail,
                            user.Street,
                            user.Neighborhood,
                            user.District,
                            user.City
                        ),
                        City = user.City,
                        District = user.District,
                        Neighborhood = user.Neighborhood,
                        Street = user.Street,
                        AddressDetail = user.AddressDetail,
                        Latitude = user.Latitude,
                        Longitude = user.Longitude,
                        UserId = user.UserId,
                        IsDeleted = user.IsDeleted
                    };

                    _context.Caretakers.Add(caretaker);
                }
            }

            _context.Users.Update(user);
            _context.SaveChanges();

            TempData["AdminUsersMessage"] = "User information updated successfully.";

            return RedirectToAction("Users", new { tab, searchText });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteUser(int userId, string tab = "users", string? searchText = null)
        {
            if (!IsCurrentUserAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUserId = HttpContext.Session.GetInt32("UserId");

            if (currentUserId == userId)
            {
                TempData["AdminUsersError"] = "You cannot delete your own admin account while you are logged in.";
                return RedirectToAction("Users", new { tab, searchText });
            }

            var user = _context.Users
                .Include(x => x.Caretaker)
                .FirstOrDefault(x => x.UserId == userId);

            if (user == null)
            {
                TempData["AdminUsersError"] = "User could not be found.";
                return RedirectToAction("Users", new { tab, searchText });
            }

            if (user.Role == "Admin")
            {
                var activeAdminCount = _context.Users.Count(x => x.Role == "Admin" && !x.IsDeleted);

                if (activeAdminCount <= 1)
                {
                    TempData["AdminUsersError"] = "At least one active admin account must remain.";
                    return RedirectToAction("Users", new { tab, searchText });
                }
            }

            user.IsDeleted = true;

            if (user.Caretaker != null)
            {
                user.Caretaker.IsDeleted = true;

                var menuItems = _context.MenuItems
                    .Where(x => x.CaretakerId == user.Caretaker.CaretakerId)
                    .ToList();

                foreach (var item in menuItems)
                {
                    item.IsDeleted = true;
                }
            }

            _context.SaveChanges();

            TempData["AdminUsersMessage"] = "User soft deleted successfully.";

            return RedirectToAction("Users", new { tab, searchText });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RestoreUser(int userId, string tab = "users", string? searchText = null)
        {
            if (!IsCurrentUserAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users
                .Include(x => x.Caretaker)
                .FirstOrDefault(x => x.UserId == userId);

            if (user == null)
            {
                TempData["AdminUsersError"] = "User could not be found.";
                return RedirectToAction("Users", new { tab, searchText });
            }

            user.IsDeleted = false;

            if (user.Caretaker != null)
            {
                user.Caretaker.IsDeleted = false;
            }

            _context.SaveChanges();

            TempData["AdminUsersMessage"] = "User restored successfully.";

            return RedirectToAction("Users", new { tab, searchText });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateCaretaker(
            int caretakerId,
            string name,
            string restaurantName,
            string city,
            string district,
            string neighborhood,
            string street,
            string addressDetail,
            string latitude,
            string longitude,
            string tab = "caretakers",
            string? searchText = null)
        {
            if (!IsCurrentUserAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var caretaker = _context.Caretakers
                .Include(x => x.User)
                .FirstOrDefault(x => x.CaretakerId == caretakerId);

            if (caretaker == null)
            {
                TempData["AdminUsersError"] = "Caretaker could not be found.";
                return RedirectToAction("Users", new { tab, searchText });
            }

            caretaker.Name = name ?? string.Empty;
            caretaker.RestaurantName = restaurantName ?? string.Empty;
            caretaker.City = city ?? string.Empty;
            caretaker.District = district ?? string.Empty;
            caretaker.Neighborhood = neighborhood ?? string.Empty;
            caretaker.Street = street ?? string.Empty;
            caretaker.AddressDetail = addressDetail ?? string.Empty;
            caretaker.Latitude = TryParseCoordinate(latitude);
            caretaker.Longitude = TryParseCoordinate(longitude);

            caretaker.Address = BuildFullAddress(
                caretaker.AddressDetail,
                caretaker.Street,
                caretaker.Neighborhood,
                caretaker.District,
                caretaker.City
            );

            if (caretaker.User != null)
            {
                caretaker.User.Name = caretaker.Name;
                caretaker.User.City = caretaker.City;
                caretaker.User.District = caretaker.District;
                caretaker.User.Neighborhood = caretaker.Neighborhood;
                caretaker.User.Street = caretaker.Street;
                caretaker.User.AddressDetail = caretaker.AddressDetail;
                caretaker.User.Latitude = caretaker.Latitude;
                caretaker.User.Longitude = caretaker.Longitude;
            }

            _context.Caretakers.Update(caretaker);
            _context.SaveChanges();

            TempData["AdminUsersMessage"] = "Caretaker information updated successfully.";

            return RedirectToAction("Users", new { tab, searchText });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SoftDeleteCaretaker(int caretakerId, string tab = "caretakers", string? searchText = null)
        {
            if (!IsCurrentUserAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var caretaker = _context.Caretakers
                .Include(x => x.User)
                .FirstOrDefault(x => x.CaretakerId == caretakerId);

            if (caretaker == null)
            {
                TempData["AdminUsersError"] = "Caretaker could not be found.";
                return RedirectToAction("Users", new { tab, searchText });
            }

            caretaker.IsDeleted = true;

            if (caretaker.User != null)
            {
                caretaker.User.IsDeleted = true;
            }

            var menuItems = _context.MenuItems
                .Where(x => x.CaretakerId == caretaker.CaretakerId)
                .ToList();

            foreach (var item in menuItems)
            {
                item.IsDeleted = true;
            }

            _context.SaveChanges();

            TempData["AdminUsersMessage"] = "Caretaker soft deleted successfully.";

            return RedirectToAction("Users", new { tab, searchText });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RestoreCaretaker(int caretakerId, string tab = "caretakers", string? searchText = null)
        {
            if (!IsCurrentUserAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var caretaker = _context.Caretakers
                .Include(x => x.User)
                .FirstOrDefault(x => x.CaretakerId == caretakerId);

            if (caretaker == null)
            {
                TempData["AdminUsersError"] = "Caretaker could not be found.";
                return RedirectToAction("Users", new { tab, searchText });
            }

            caretaker.IsDeleted = false;

            if (caretaker.User != null)
            {
                caretaker.User.IsDeleted = false;
            }

            _context.SaveChanges();

            TempData["AdminUsersMessage"] = "Caretaker restored successfully.";

            return RedirectToAction("Users", new { tab, searchText });
        }

        private bool IsCurrentUserAdmin()
        {
            var role = HttpContext.Session.GetString("UserRole");
            return role == "Admin";
        }

        private double? TryParseCoordinate(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result))
            {
                return result;
            }

            return null;
        }

        private string BuildFullAddress(
            string addressDetail,
            string street,
            string neighborhood,
            string district,
            string city)
        {
            return string.Join(", ", new[]
            {
                addressDetail,
                street,
                neighborhood,
                district,
                city,
                "Turkey"
            }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }
}