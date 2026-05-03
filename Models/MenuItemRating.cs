namespace GOGOFOOD.Models
{
    public class MenuItemRating
    {
        public int MenuItemRatingId { get; set; }

        public int RatingValue { get; set; }

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int UserId { get; set; }

        public User? User { get; set; }

        public int OrderId { get; set; }

        public Order? Order { get; set; }

        public int MenuItemId { get; set; }

        public MenuItem? MenuItem { get; set; }
    }
}