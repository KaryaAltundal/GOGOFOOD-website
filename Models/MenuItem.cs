namespace GOGOFOOD.Models
{
    public class MenuItem
    {
        public int MenuItemId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Description { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public int? CaretakerId { get; set; }

        public Caretaker? Caretaker { get; set; }

        public List<CustomizationOption>? CustomizationOptions { get; set; }

        public List<MenuItemRating>? MenuItemRatings { get; set; }
    }
}