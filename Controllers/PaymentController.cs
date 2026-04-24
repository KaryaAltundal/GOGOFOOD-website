using GOGOFOOD.Data;
using GOGOFOOD.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GOGOFOOD.Controllers
{
    public class PaymentController : Controller
    {
        private readonly GogoFoodContext _context;

        public PaymentController(GogoFoodContext context)
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

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson);
        }

        private void ClearCart()
        {
            HttpContext.Session.Remove("Cart");
        }

        public IActionResult Checkout()
        {
            var cart = GetCart();

            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            return View(cart);
        }

        [HttpPost]
        public IActionResult CompletePayment(string cardHolder, string cardNumber, string expirationDate, string cvv)
        {
            var cart = GetCart();

            if (cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            // toplam fiyat
            decimal total = cart.Sum(x => x.TotalPrice);

            // order oluştur
            var order = new Order
            {
                OrderDate = DateTime.Now,
                TotalPrice = total,
                Status = "Completed",
                UserId = 1 // şimdilik sabit (login sistemi sonra bağlanır)
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            // order items ekle
            foreach (var item in cart)
            {
                var orderItem = new OrderItem
                {
                    OrderId = order.OrderId,
                    MenuItemId = item.MenuItemId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    CustomizationName = item.Customization,
                    CustomizationPrice = item.CustomizationPrice
                };

                _context.OrderItems.Add(orderItem);
            }

            _context.SaveChanges();

            // cart temizle
            ClearCart();

            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}