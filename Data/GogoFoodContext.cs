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

        public DbSet<MenuItemRating> MenuItemRatings { get; set; }

        public DbSet<CaretakerRating> CaretakerRatings { get; set; }

        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Decimal precision warnings düzelsin diye
            modelBuilder.Entity<MenuItem>()
                .Property(x => x.Price)
                .HasPrecision(18, 2);

            modelBuilder.Entity<CustomizationOption>()
                .Property(x => x.ExtraPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<Order>()
                .Property(x => x.TotalPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(x => x.UnitPrice)
                .HasPrecision(18, 2);

            modelBuilder.Entity<OrderItem>()
                .Property(x => x.CustomizationPrice)
                .HasPrecision(18, 2);

            // User - Caretaker bağlantısı
            modelBuilder.Entity<Caretaker>()
                .HasOne(x => x.User)
                .WithOne(x => x.Caretaker)
                .HasForeignKey<Caretaker>(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            // Cascade path hatasını engellemek için
            modelBuilder.Entity<MenuItemRating>()
                .HasOne(x => x.User)
                .WithMany(x => x.MenuItemRatings)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MenuItemRating>()
                .HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MenuItemRating>()
                .HasOne(x => x.MenuItem)
                .WithMany(x => x.MenuItemRatings)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CaretakerRating>()
                .HasOne(x => x.User)
                .WithMany(x => x.CaretakerRatings)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CaretakerRating>()
                .HasOne(x => x.Order)
                .WithMany()
                .HasForeignKey(x => x.OrderId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<CaretakerRating>()
                .HasOne(x => x.Caretaker)
                .WithMany(x => x.CaretakerRatings)
                .HasForeignKey(x => x.CaretakerId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<MenuItem>()
                .HasOne(x => x.Caretaker)
                .WithMany(x => x.MenuItems)
                .HasForeignKey(x => x.CaretakerId)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}