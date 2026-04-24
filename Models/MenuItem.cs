namespace GOGOFOOD.Models
{
    public class MenuItem
    {
        public int MenuItemId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public string Description { get; set; } = string.Empty;

        public List<CustomizationOption>? CustomizationOptions { get; set; }
    }
}