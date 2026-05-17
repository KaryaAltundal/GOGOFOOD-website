namespace GOGOFOOD.Models
{
    public class CartItem
    {
        public string CartItemKey { get; set; } = Guid.NewGuid().ToString();

        public int MenuItemId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Category { get; set; } = "Main Course";

        public string RestaurantName { get; set; } = string.Empty;

        public string ImageUrl { get; set; } = string.Empty;

        // Price per person for catering.
        public decimal Price { get; set; }

        // In the catering system, Quantity means people count.
        public int Quantity { get; set; }

        public string Customization { get; set; } = "No customization";

        public decimal CustomizationPrice { get; set; }

        public decimal TotalPrice
        {
            get
            {
                return (Price + CustomizationPrice) * Quantity;
            }
        }
    }
}