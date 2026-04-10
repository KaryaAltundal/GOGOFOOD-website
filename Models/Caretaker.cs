namespace GOGOFOOD.Models
{
    public class Caretaker
    {
        public int CaretakerId { get; set; }
        public string Name { get; set; }
        public string RestaurantName { get; set; }
        public string Address { get; set; }

        public List<MenuItem>? MenuItems { get; set; }
    }
}