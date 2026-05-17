using System.Globalization;
using GOGOFOOD.Data;
using GOGOFOOD.Models;
using GOGOFOOD.Services;
using Microsoft.AspNetCore.Mvc;

namespace GOGOFOOD.Controllers
{
    public class CaretakerController : Controller
    {
        private readonly GogoFoodContext _context;
        private readonly GoogleMapsService _googleMapsService;
        private readonly LogService _logService;
        private readonly IConfiguration _configuration;

        public CaretakerController(
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
        public IActionResult Profile()
        {
            var role = HttpContext.Session.GetString("UserRole");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");

            if (role != "Caretaker" || caretakerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var caretaker = _context.Caretakers
                .FirstOrDefault(x => x.CaretakerId == caretakerId.Value);

            if (caretaker == null)
            {
                return RedirectToAction("Login", "Account");
            }

            ViewBag.GoogleMapsApiKey = _configuration["GoogleMaps:ApiKey"] ?? string.Empty;

            return View(caretaker);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(Caretaker caretaker, string latitude, string longitude)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");
            var userEmail = HttpContext.Session.GetString("UserEmail");

            if (role != "Caretaker" || caretakerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var existingCaretaker = _context.Caretakers
                .FirstOrDefault(x => x.CaretakerId == caretakerId.Value);

            if (existingCaretaker == null)
            {
                return RedirectToAction("Login", "Account");
            }

            string fullAddress = BuildFullAddress(
                caretaker.AddressDetail,
                caretaker.Street,
                caretaker.Neighborhood,
                caretaker.District,
                caretaker.City
            );

            double? selectedLatitude = TryParseCoordinate(latitude);
            double? selectedLongitude = TryParseCoordinate(longitude);

            if (selectedLatitude == null || selectedLongitude == null)
            {
                var coordinates = await _googleMapsService.GetCoordinatesFromAddressAsync(fullAddress);

                if (coordinates == null)
                {
                    TempData["ProfileError"] = "Restaurant location could not be found by Google Maps. Please check the address or select a point on the map.";

                    _logService.AddLog(
                        "Google Maps Error",
                        "Caretaker restaurant address could not be converted to coordinates: " + fullAddress,
                        userEmail,
                        role
                    );

                    return RedirectToAction("Profile");
                }

                selectedLatitude = coordinates.Value.Latitude;
                selectedLongitude = coordinates.Value.Longitude;
            }

            existingCaretaker.Name = caretaker.Name;
            existingCaretaker.RestaurantName = caretaker.RestaurantName;
            existingCaretaker.City = caretaker.City ?? string.Empty;
            existingCaretaker.District = caretaker.District ?? string.Empty;
            existingCaretaker.Neighborhood = caretaker.Neighborhood ?? string.Empty;
            existingCaretaker.Street = caretaker.Street ?? string.Empty;
            existingCaretaker.AddressDetail = caretaker.AddressDetail ?? string.Empty;
            existingCaretaker.Address = fullAddress;
            existingCaretaker.Latitude = selectedLatitude;
            existingCaretaker.Longitude = selectedLongitude;

            _context.Caretakers.Update(existingCaretaker);
            _context.SaveChanges();

            _logService.AddLog(
                "Restaurant Profile Updated",
                "Caretaker restaurant profile and location updated using Google Maps.",
                userEmail,
                role
            );

            TempData["ProfileMessage"] = "Restaurant profile updated successfully.";

            return RedirectToAction("Profile");
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