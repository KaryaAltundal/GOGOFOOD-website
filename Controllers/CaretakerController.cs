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

        public CaretakerController(
            GogoFoodContext context,
            GoogleMapsService googleMapsService,
            LogService logService)
        {
            _context = context;
            _googleMapsService = googleMapsService;
            _logService = logService;
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

            return View(caretaker);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(Caretaker caretaker)
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

            var coordinates = await _googleMapsService.GetCoordinatesFromAddressAsync(fullAddress);

            if (coordinates == null)
            {
                TempData["ProfileError"] = "Restaurant location could not be found by Google Maps. Please check the address.";

                _logService.AddLog(
                    "Google Maps Error",
                    "Caretaker restaurant address could not be converted to coordinates: " + fullAddress,
                    userEmail,
                    role
                );

                return RedirectToAction("Profile");
            }

            existingCaretaker.Name = caretaker.Name;
            existingCaretaker.RestaurantName = caretaker.RestaurantName;
            existingCaretaker.City = caretaker.City ?? string.Empty;
            existingCaretaker.District = caretaker.District ?? string.Empty;
            existingCaretaker.Neighborhood = caretaker.Neighborhood ?? string.Empty;
            existingCaretaker.Street = caretaker.Street ?? string.Empty;
            existingCaretaker.AddressDetail = caretaker.AddressDetail ?? string.Empty;
            existingCaretaker.Address = fullAddress;
            existingCaretaker.Latitude = coordinates.Value.Latitude;
            existingCaretaker.Longitude = coordinates.Value.Longitude;

            _context.Caretakers.Update(existingCaretaker);
            _context.SaveChanges();

            _logService.AddLog(
                "Restaurant Profile Updated",
                "Caretaker restaurant profile and location updated using Google Geocoding API.",
                userEmail,
                role
            );

            TempData["ProfileMessage"] = "Restaurant profile updated successfully using Google Maps.";

            return RedirectToAction("Profile");
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