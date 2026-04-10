namespace GOGOFOOD.Models
{
    public class MenuItem
    {
        public int MenuItemId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public string Description { get; set; }
        public string ImageUrl { get; set; }

        public int CaretakerId { get; set; }
        public Caretaker? Caretaker { get; set; }

        public List<OrderItem>? OrderItems { get; set; }
    }
}