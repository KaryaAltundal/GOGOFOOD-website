using GOGOFOOD.Data;
using GOGOFOOD.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
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
        public IActionResult AddToCart(
            int id,
            string name,
            decimal price,
            string imageUrl,
            int[]? customizationOptionIds)
        {
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
                existingItem.Quantity++;
            }
            else
            {
                cart.Add(new CartItem
                {
                    CartItemKey = Guid.NewGuid().ToString(),
                    MenuItemId = id,
                    Name = name,
                    ImageUrl = imageUrl ?? string.Empty,
                    Price = price,
                    Quantity = 1,
                    Customization = customizationName,
                    CustomizationPrice = customizationPrice
                });
            }

            SaveCart(cart);

            return RedirectToAction("Index", "Cart");
        }

        public IActionResult IncreaseQuantity(string cartItemKey)
        {
            var cart = GetCart();

            var item = cart.FirstOrDefault(x => x.CartItemKey == cartItemKey);

            if (item != null)
            {
                item.Quantity++;
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
                item.Quantity--;

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