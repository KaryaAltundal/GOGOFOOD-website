namespace GOGOFOOD.Models
{
    public class Caretaker
    {
        public int CaretakerId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string RestaurantName { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string District { get; set; } = string.Empty;

        public string Neighborhood { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;

        public string AddressDetail { get; set; } = string.Empty;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public bool IsDeleted { get; set; } = false;

        public int? UserId { get; set; }

        public User? User { get; set; }

        public List<MenuItem>? MenuItems { get; set; }

        public List<CaretakerRating>? CaretakerRatings { get; set; }
    }
}