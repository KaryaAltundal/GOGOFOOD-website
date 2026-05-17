using System.Globalization;
using GOGOFOOD.Data;
using GOGOFOOD.Services;
using Microsoft.AspNetCore.Mvc;

namespace GOGOFOOD.Controllers
{
    public class UserController : Controller
    {
        private readonly GogoFoodContext _context;
        private readonly GoogleMapsService _googleMapsService;
        private readonly LogService _logService;
        private readonly IConfiguration _configuration;

        public UserController(
            GogoFoodContext context,
            GoogleMapsService googleMapsService,
            LogService logService,
            IConfiguration configuration)
        {
            _context = context;
            _googleMapsService = googleMapsService;
            _logService = logService;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Location()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(x => x.UserId == userId.Value);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Location(
            string city,
            string district,
            string neighborhood,
            string street,
            string addressDetail,
            string latitude,
            string longitude)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = _context.Users.FirstOrDefault(x => x.UserId == userId.Value);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string fullAddress = BuildFullAddress(
                addressDetail,
                street,
                neighborhood,
                district,
                city
            );

            double? selectedLatitude = TryParseCoordinate(latitude);
            double? selectedLongitude = TryParseCoordinate(longitude);

            if (selectedLatitude == null || selectedLongitude == null)
            {
                var coordinates = await _googleMapsService.GetCoordinatesFromAddressAsync(fullAddress);

                if (coordinates == null)
                {
                    TempData["LocationError"] = "Location could not be found by Google Maps. Please check your address or select a point on the map.";

                    _logService.AddLog(
                        "Google Maps Error",
                        "User address could not be converted to coordinates: " + fullAddress,
                        userEmail,
                        userRole
                    );

                    return RedirectToAction("Location");
                }

                selectedLatitude = coordinates.Value.Latitude;
                selectedLongitude = coordinates.Value.Longitude;
            }

            user.City = city ?? string.Empty;
            user.District = district ?? string.Empty;
            user.Neighborhood = neighborhood ?? string.Empty;
            user.Street = street ?? string.Empty;
            user.AddressDetail = addressDetail ?? string.Empty;
            user.Latitude = selectedLatitude;
            user.Longitude = selectedLongitude;

            _context.Users.Update(user);
            _context.SaveChanges();

            _logService.AddLog(
                "Location Updated",
                "User location updated using Google Maps.",
                userEmail,
                userRole
            );

            TempData["LocationMessage"] = "Your location was updated successfully.";

            return RedirectToAction("Location");
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