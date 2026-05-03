using GOGOFOOD.Data;
using GOGOFOOD.Models;
using GOGOFOOD.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace GOGOFOOD.Controllers
{
    public class PaymentController : Controller
    {
        private readonly GogoFoodContext _context;
        private readonly LogService _logService;
        private readonly EmailService _emailService;

        public PaymentController(
            GogoFoodContext context,
            LogService logService,
            EmailService emailService)
        {
            _context = context;
            _logService = logService;
            _emailService = emailService;
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

            var user = _context.Users.FirstOrDefault(x => x.UserId == userId.Value);

            if (user == null)
            {
                _logService.AddLog(
                    "Payment Failed",
                    "Payment attempt failed because user could not be found.",
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

            SendOrderEmails(order.OrderId);

            ClearCart();

            return RedirectToAction("Success");
        }

        private void SendOrderEmails(int orderId)
        {
            var order = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null || order.User == null || order.OrderItems == null)
            {
                return;
            }

            var userEmailBody = BuildUserOrderEmailBody(order);

            _emailService.SendEmail(
                order.User.Email,
                "GOGOFOOD Order Confirmation - Order #" + order.OrderId,
                userEmailBody
            );

            var caretakerIds = order.OrderItems
                .Where(x => x.MenuItem != null && x.MenuItem.CaretakerId != null)
                .Select(x => x.MenuItem!.CaretakerId!.Value)
                .Distinct()
                .ToList();

            foreach (var caretakerId in caretakerIds)
            {
                var caretaker = _context.Caretakers
                    .Include(c => c.User)
                    .FirstOrDefault(c => c.CaretakerId == caretakerId);

                if (caretaker == null || caretaker.User == null)
                {
                    _logService.AddLog(
                        "Email Error",
                        "Caretaker email could not be sent because caretaker user was not found. CaretakerId: " + caretakerId,
                        order.User.Email,
                        "System"
                    );

                    continue;
                }

                var caretakerItems = order.OrderItems
                    .Where(x => x.MenuItem != null && x.MenuItem.CaretakerId == caretakerId)
                    .ToList();

                var caretakerEmailBody = BuildCaretakerOrderEmailBody(order, caretaker, caretakerItems);

                _emailService.SendEmail(
                    caretaker.User.Email,
                    "New GOGOFOOD Order Received - Order #" + order.OrderId,
                    caretakerEmailBody
                );
            }
        }

        private string BuildUserOrderEmailBody(Order order)
        {
            var body = new StringBuilder();

            body.Append("<h2>Thank you for your order!</h2>");
            body.Append("<p>Your GOGOFOOD order has been completed successfully.</p>");

            body.Append("<p><strong>Order ID:</strong> " + order.OrderId + "</p>");
            body.Append("<p><strong>Order Date:</strong> " + order.OrderDate.ToString("dd.MM.yyyy HH:mm") + "</p>");
            body.Append("<p><strong>Status:</strong> " + order.Status + "</p>");

            body.Append("<table border='1' cellpadding='8' cellspacing='0'>");
            body.Append("<tr>");
            body.Append("<th>Menu Item</th>");
            body.Append("<th>Quantity</th>");
            body.Append("<th>Unit Price</th>");
            body.Append("<th>Customization</th>");
            body.Append("<th>Customization Price</th>");
            body.Append("</tr>");

            foreach (var item in order.OrderItems ?? new List<OrderItem>())
            {
                body.Append("<tr>");
                body.Append("<td>" + item.MenuItem?.Name + "</td>");
                body.Append("<td>" + item.Quantity + "</td>");
                body.Append("<td>" + item.UnitPrice + "</td>");
                body.Append("<td>" + item.CustomizationName + "</td>");
                body.Append("<td>" + item.CustomizationPrice + "</td>");
                body.Append("</tr>");
            }

            body.Append("</table>");

            body.Append("<h3>Total Price: " + order.TotalPrice + "</h3>");
            body.Append("<p>Enjoy your meal!</p>");

            return body.ToString();
        }

        private string BuildCaretakerOrderEmailBody(Order order, Caretaker caretaker, List<OrderItem> items)
        {
            var body = new StringBuilder();

            body.Append("<h2>New Order Received</h2>");
            body.Append("<p>A new order has been placed for your restaurant.</p>");

            body.Append("<p><strong>Restaurant:</strong> " + caretaker.RestaurantName + "</p>");
            body.Append("<p><strong>Order ID:</strong> " + order.OrderId + "</p>");
            body.Append("<p><strong>Order Date:</strong> " + order.OrderDate.ToString("dd.MM.yyyy HH:mm") + "</p>");

            if (order.User != null)
            {
                body.Append("<p><strong>Customer:</strong> " + order.User.Name + " (" + order.User.Email + ")</p>");
            }

            body.Append("<table border='1' cellpadding='8' cellspacing='0'>");
            body.Append("<tr>");
            body.Append("<th>Menu Item</th>");
            body.Append("<th>Quantity</th>");
            body.Append("<th>Unit Price</th>");
            body.Append("<th>Customization</th>");
            body.Append("<th>Customization Price</th>");
            body.Append("</tr>");

            foreach (var item in items)
            {
                body.Append("<tr>");
                body.Append("<td>" + item.MenuItem?.Name + "</td>");
                body.Append("<td>" + item.Quantity + "</td>");
                body.Append("<td>" + item.UnitPrice + "</td>");
                body.Append("<td>" + item.CustomizationName + "</td>");
                body.Append("<td>" + item.CustomizationPrice + "</td>");
                body.Append("</tr>");
            }

            body.Append("</table>");

            var caretakerTotal = items.Sum(x => (x.UnitPrice + x.CustomizationPrice) * x.Quantity);

            body.Append("<h3>Restaurant Total: " + caretakerTotal + "</h3>");

            return body.ToString();
        }

        public IActionResult Success()
        {
            return View();
        }
    }
}