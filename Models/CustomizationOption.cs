namespace GOGOFOOD.Models
{
    public class CustomizationOption
    {
        public int CustomizationOptionId { get; set; }

        public string Name { get; set; }

        public decimal ExtraPrice { get; set; }

        public int MenuItemId { get; set; }

        public MenuItem? MenuItem { get; set; }
    }
}