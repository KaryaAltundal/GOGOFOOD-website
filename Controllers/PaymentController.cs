using GOGOFOOD.Data;
using GOGOFOOD.Models;
using GOGOFOOD.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GOGOFOOD.Controllers
{
    public class PaymentController : Controller
    {
        private readonly GogoFoodContext _context;
        private readonly LogService _logService;

        public PaymentController(GogoFoodContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
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

        private void ClearCart()
        {
            HttpContext.Session.Remove("Cart");
        }

        public IActionResult Checkout()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

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
            var userId = HttpContext.Session.GetInt32("UserId");
            var userEmail = HttpContext.Session.GetString("UserEmail");
            var userRole = HttpContext.Session.GetString("UserRole");

            if (userId == null)
            {
                _logService.AddLog(
                    "Payment Failed",
                    "Payment attempt failed because user was not logged in.",
                    userEmail,
                    userRole
                );

                return RedirectToAction("Login", "Account");
            }

            var cart = GetCart();

            if (cart.Count == 0)
            {
                _logService.AddLog(
                    "Payment Failed",
                    "Payment attempt failed because cart was empty.",
                    userEmail,
                    userRole
                );

                return RedirectToAction("Index", "Cart");
            }

            decimal total = cart.Sum(x => x.TotalPrice);

            var order = new Order
            {
                OrderDate = DateTime.Now,
                TotalPrice = total,
                Status = "Completed",
                UserId = userId.Value
            };

            _context.Orders.Add(order);
            _context.SaveChanges();

            _logService.AddLog(
                "Order Created",
                "Order created with Order ID: " + order.OrderId + " Total: " + total,
                userEmail,
                userRole
            );

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

            _logService.AddLog(
                "Payment Completed",
                "Payment completed successfully for Order ID: " + order.OrderId,
                userEmail,
                userRole
            );

            ClearCart();

            return RedirectToAction("Success");
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}