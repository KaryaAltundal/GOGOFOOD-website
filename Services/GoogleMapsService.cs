using System.Globalization;
using System.Text.Json;

namespace GOGOFOOD.Services
{
    public class GoogleMapsService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly LogService _logService;

        public GoogleMapsService(
            IConfiguration configuration,
            HttpClient httpClient,
            LogService logService)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _logService = logService;
        }

        public async Task<(double Latitude, double Longitude)?> GetCoordinatesFromAddressAsync(string address)
        {
            try
            {
                string apiKey = _configuration["GoogleMaps:ApiKey"] ?? "";

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logService.AddLog(
                        "Google Maps Error",
                        "Google Maps API key is missing in appsettings.json.",
                        null,
                        "System"
                    );

                    return null;
                }

                if (string.IsNullOrWhiteSpace(address))
                {
                    _logService.AddLog(
                        "Google Maps Error",
                        "Address is empty. Coordinates could not be calculated.",
                        null,
                        "System"
                    );

                    return null;
                }

                string encodedAddress = Uri.EscapeDataString(address);

                string url =
                    "https://maps.googleapis.com/maps/api/geocode/json" +
                    "?address=" + encodedAddress +
                    "&key=" + apiKey;

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                string status = root.GetProperty("status").GetString() ?? "";

                if (status != "OK")
                {
                    _logService.AddLog(
                        "Google Maps Error",
                        "Geocoding API returned status: " + status + " for address: " + address,
                        null,
                        "System"
                    );

                    return null;
                }

                var location = root
                    .GetProperty("results")[0]
                    .GetProperty("geometry")
                    .GetProperty("location");

                double latitude = location.GetProperty("lat").GetDouble();
                double longitude = location.GetProperty("lng").GetDouble();

                return (latitude, longitude);
            }
            catch (Exception ex)
            {
                _logService.AddLog(
                    "Google Maps Error",
                    "Geocoding failed. Error: " + ex.Message,
                    null,
                    "System"
                );

                return null;
            }
        }

        public async Task<double?> GetDistanceInKmAsync(
            double originLatitude,
            double originLongitude,
            double destinationLatitude,
            double destinationLongitude)
        {
            try
            {
                string apiKey = _configuration["GoogleMaps:ApiKey"] ?? "";

                if (string.IsNullOrEmpty(apiKey))
                {
                    _logService.AddLog(
                        "Google Maps Error",
                        "Google Maps API key is missing in appsettings.json.",
                        null,
                        "System"
                    );

                    return null;
                }

                string origins =
                    originLatitude.ToString(CultureInfo.InvariantCulture) +
                    "," +
                    originLongitude.ToString(CultureInfo.InvariantCulture);

                string destinations =
                    destinationLatitude.ToString(CultureInfo.InvariantCulture) +
                    "," +
                    destinationLongitude.ToString(CultureInfo.InvariantCulture);

                string url =
                    "https://maps.googleapis.com/maps/api/distancematrix/json" +
                    "?origins=" + origins +
                    "&destinations=" + destinations +
                    "&mode=driving" +
                    "&units=metric" +
                    "&key=" + apiKey;

                var response = await _httpClient.GetAsync(url);
                var json = await response.Content.ReadAsStringAsync();

                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                string status = root.GetProperty("status").GetString() ?? "";

                if (status != "OK")
                {
                    _logService.AddLog(
                        "Google Maps Error",
                        "Distance Matrix API returned status: " + status,
                        null,
                        "System"
                    );

                    return null;
                }

                var element = root
                    .GetProperty("rows")[0]
                    .GetProperty("elements")[0];

                string elementStatus = element.GetProperty("status").GetString() ?? "";

                if (elementStatus != "OK")
                {
                    _logService.AddLog(
                        "Google Maps Error",
                        "Distance Matrix route element returned status: " + elementStatus,
                        null,
                        "System"
                    );

                    return null;
                }

                int distanceMeters = element
                    .GetProperty("distance")
                    .GetProperty("value")
                    .GetInt32();

                double distanceKm = distanceMeters / 1000.0;

                return distanceKm;
            }
            catch (Exception ex)
            {
                _logService.AddLog(
                    "Google Maps Error",
                    "Distance calculation failed. Error: " + ex.Message,
                    null,
                    "System"
                );

                return null;
            }
        }
    }
}