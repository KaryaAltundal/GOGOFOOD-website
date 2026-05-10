namespace GOGOFOOD.Models
{
    public class User
    {
        public int UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        // Security note:
        // This property stores the hashed password, not the plain text password.
        // It is kept as "Password" to avoid changing the existing database column.
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string District { get; set; } = string.Empty;

        public string Neighborhood { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;

        public string AddressDetail { get; set; } = string.Empty;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public Caretaker? Caretaker { get; set; }

        public List<Order>? Orders { get; set; }

        public List<MenuItemRating>? MenuItemRatings { get; set; }

        public List<CaretakerRating>? CaretakerRatings { get; set; }
    }
}