using GOGOFOOD.Data;
using GOGOFOOD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GOGOFOOD.Controllers
{
    public class CartController : Controller
    {
        private readonly GogoFoodContext _context;

        public CartController(GogoFoodContext context)
        {
            _context = context;
        }

        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString("Cart");

            if (string.IsNullOrEmpty(cartJson))
            {
                return new List<CartItem>();
            }

            var cart = JsonSerializer.Deserialize<List<CartItem>>(cartJson);

            return cart ?? new List<CartItem>();
        }

        private void SaveCart(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString("Cart", cartJson);
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddToCart(
            int id,
            int[]? customizationOptionIds,
            int peopleCount = 100,
            string? returnUrl = null)
        {
            var menuItem = _context.MenuItems
                .Include(x => x.Caretaker)
                .FirstOrDefault(x => x.MenuItemId == id && !x.IsDeleted);

            if (menuItem == null)
            {
                return RedirectToAction("Index", "MenuItem");
            }

            // Catering system works with 100-person steps.
            if (peopleCount < 100)
            {
                peopleCount = 100;
            }

            if (peopleCount % 100 != 0)
            {
                peopleCount = (int)Math.Ceiling(peopleCount / 100.0) * 100;
            }

            var cart = GetCart();

            string customizationName = "No customization";
            decimal customizationPrice = 0;

            if (customizationOptionIds != null && customizationOptionIds.Length > 0)
            {
                var selectedOptions = _context.CustomizationOptions
                    .Where(x =>
                        customizationOptionIds.Contains(x.CustomizationOptionId) &&
                        x.MenuItemId == id)
                    .OrderBy(x => x.CustomizationOptionId)
                    .ToList();

                if (selectedOptions.Count > 0)
                {
                    customizationName = string.Join(", ", selectedOptions.Select(x => x.Name));
                    customizationPrice = selectedOptions.Sum(x => x.ExtraPrice);
                }
            }

            var existingItem = cart.FirstOrDefault(x =>
                x.MenuItemId == id &&
                x.Customization == customizationName);

            if (existingItem != null)
            {
                existingItem.Quantity += peopleCount;
            }
            else
            {
                cart.Add(new CartItem
                {
                    CartItemKey = Guid.NewGuid().ToString(),
                    MenuItemId = menuItem.MenuItemId,
                    Name = menuItem.Name,
                    Category = menuItem.Category,
                    RestaurantName = menuItem.Caretaker?.RestaurantName ?? string.Empty,
                    ImageUrl = menuItem.ImageUrl ?? string.Empty,
                    Price = menuItem.Price,
                    Quantity = peopleCount,
                    Customization = customizationName,
                    CustomizationPrice = customizationPrice
                });
            }

            SaveCart(cart);

            TempData["SelectedCategory"] = menuItem.Category;
            TempData["SelectedMenuName"] = menuItem.Name;
            TempData["MenuMessage"] = menuItem.Category + " selected: " + menuItem.Name;

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Cart");
        }

        public IActionResult IncreaseQuantity(string cartItemKey)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(x => x.CartItemKey == cartItemKey);

            if (item != null)
            {
                item.Quantity += 100;
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        public IActionResult DecreaseQuantity(string cartItemKey)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(x => x.CartItemKey == cartItemKey);

            if (item != null)
            {
                item.Quantity -= 100;

                if (item.Quantity <= 0)
                {
                    cart.Remove(item);
                }
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }

        public IActionResult RemoveItem(string cartItemKey)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(x => x.CartItemKey == cartItemKey);

            if (item != null)
            {
                cart.Remove(item);
            }

            SaveCart(cart);

            return RedirectToAction("Index");
        }
    }
}