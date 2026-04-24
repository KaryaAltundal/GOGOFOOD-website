namespace GOGOFOOD.Models
{
    public class OrderItem
    {
        public int OrderItemId { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public string CustomizationName { get; set; } = "No customization";

        public decimal CustomizationPrice { get; set; }

        public int OrderId { get; set; }

        public Order? Order { get; set; }

        public int MenuItemId { get; set; }

        public MenuItem? MenuItem { get; set; }
    }
}