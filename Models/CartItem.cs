namespace GOGOFOOD.Models
{
    public class CartItem
    {
        public string CartItemKey { get; set; } = Guid.NewGuid().ToString();

        public int MenuItemId { get; set; }

        public string Name { get; set; } = string.Empty;

        public decimal Price { get; set; }

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