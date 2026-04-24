namespace GOGOFOOD.Models
{
    public class CartItem
    {
        public int MenuItemId { get; set; }

        public string Name { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string Customization { get; set; }

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