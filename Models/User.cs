using System.ComponentModel.DataAnnotations;

namespace GOGOFOOD.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        // Security note:
        // This property stores the hashed password, not the plain text password.
        // It is kept as "Password" to avoid changing the existing database column.
        [Required]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
        [RegularExpression(@"^(?=.*[^\w\s]).{6,}$",
            ErrorMessage = "Password must include at least one punctuation mark.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;

        public string District { get; set; } = string.Empty;

        public string Neighborhood { get; set; } = string.Empty;

        public string Street { get; set; } = string.Empty;

        public string AddressDetail { get; set; } = string.Empty;

        public double? Latitude { get; set; }

        public double? Longitude { get; set; }

        public bool IsDeleted { get; set; } = false;

        public Caretaker? Caretaker { get; set; }

        public List<Order>? Orders { get; set; }

        public List<MenuItemRating>? MenuItemRatings { get; set; }

        public List<CaretakerRating>? CaretakerRatings { get; set; }
    }
}