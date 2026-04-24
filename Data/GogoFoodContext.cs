using Microsoft.EntityFrameworkCore;
using GOGOFOOD.Models;

namespace GOGOFOOD.Data
{
    public class GogoFoodContext : DbContext
    {
        public GogoFoodContext(DbContextOptions<GogoFoodContext> options)
            : base(options)
        {
        }
public DbSet<User> Users { get; set; }
public DbSet<Caretaker> Caretakers { get; set; }
public DbSet<MenuItem> MenuItems { get; set; }
public DbSet<CustomizationOption> CustomizationOptions { get; set; }
public DbSet<Order> Orders { get; set; }
public DbSet<OrderItem> OrderItems { get; set; }
    }
}