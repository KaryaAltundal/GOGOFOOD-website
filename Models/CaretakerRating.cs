namespace GOGOFOOD.Models
{
    public class CaretakerRating
    {
        public int CaretakerRatingId { get; set; }

        public int RatingValue { get; set; }

        public string Comment { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public int UserId { get; set; }

        public User? User { get; set; }

        public int OrderId { get; set; }

        public Order? Order { get; set; }

        public int CaretakerId { get; set; }

        public Caretaker? Caretaker { get; set; }
    }
}