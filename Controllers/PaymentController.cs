using GOGOFOOD.Data;
using GOGOFOOD.Models;
using GOGOFOOD.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
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
                .Include(o => o.OrderItems!)
                .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefault(o => o.OrderId == orderId);

            if (order == null || order.User == null || order.OrderItems == null)
            {
                return;
            }

            var receiptPdf = GenerateReceiptPdf(order);
            var userAgreementPdf = GenerateAgreementPdf(order, null, order.OrderItems.ToList());
            var userEmailBody = BuildUserOrderEmailBody(order);

            _emailService.SendEmail(
                order.User.Email,
                "GOGOFOOD Order Confirmation - Order #" + order.OrderId,
                userEmailBody,
                new List<(string FileName, byte[] Content, string ContentType)>
                {
                    ("GOGOFOOD-Receipt-Order-" + order.OrderId + ".pdf", receiptPdf, "application/pdf"),
                    ("GOGOFOOD-Agreement-Order-" + order.OrderId + ".pdf", userAgreementPdf, "application/pdf")
                }
            );

            _logService.AddLog(
                "PDF Sent",
                "Receipt PDF and agreement PDF were sent to user for Order ID: " + order.OrderId,
                order.User.Email,
                "System"
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

                var caretakerAgreementPdf = GenerateAgreementPdf(order, caretaker, caretakerItems);
                var caretakerEmailBody = BuildCaretakerOrderEmailBody(order, caretaker, caretakerItems);

                _emailService.SendEmail(
                    caretaker.User.Email,
                    "New GOGOFOOD Order Received - Order #" + order.OrderId,
                    caretakerEmailBody,
                    new List<(string FileName, byte[] Content, string ContentType)>
                    {
                        ("GOGOFOOD-Caretaker-Agreement-Order-" + order.OrderId + ".pdf", caretakerAgreementPdf, "application/pdf")
                    }
                );

                _logService.AddLog(
                    "PDF Sent",
                    "Agreement PDF was sent to caretaker for Order ID: " + order.OrderId + " CaretakerId: " + caretakerId,
                    caretaker.User.Email,
                    "System"
                );
            }
        }

        private byte[] GenerateReceiptPdf(Order order)
        {
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(column =>
                    {
                        column.Item().Text("GOGOFOOD RECEIPT")
                            .FontSize(24)
                            .Bold()
                            .FontColor(Colors.Pink.Medium);

                        column.Item().Text("Dynamic receipt generated for this specific order.")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Spacing(12);

                        column.Item().Text("Order Information").FontSize(16).Bold();
                        column.Item().Text("Order ID: " + order.OrderId);
                        column.Item().Text("Order Date: " + order.OrderDate.ToString("dd.MM.yyyy HH:mm"));
                        column.Item().Text("Customer: " + (order.User?.Name ?? "Unknown"));
                        column.Item().Text("Email: " + (order.User?.Email ?? "Unknown"));
                        column.Item().Text("Status: " + order.Status);

                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        column.Item().Text("Ordered Items").FontSize(16).Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(55);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("Item");
                                header.Cell().Element(HeaderCellStyle).Text("Qty");
                                header.Cell().Element(HeaderCellStyle).Text("Unit");
                                header.Cell().Element(HeaderCellStyle).Text("Total");
                            });

                            foreach (var item in order.OrderItems ?? new List<OrderItem>())
                            {
                                var lineTotal = (item.UnitPrice + item.CustomizationPrice) * item.Quantity;

                                var customization = string.IsNullOrWhiteSpace(item.CustomizationName)
                                    ? "No customization"
                                    : item.CustomizationName;

                                table.Cell().Element(BodyCellStyle).Column(c =>
                                {
                                    c.Item().Text(item.MenuItem?.Name ?? "Menu Item").Bold();
                                    c.Item().Text("Customization: " + customization)
                                        .FontSize(9)
                                        .FontColor(Colors.Grey.Darken1);
                                });

                                table.Cell().Element(BodyCellStyle).Text(item.Quantity.ToString());
                                table.Cell().Element(BodyCellStyle).Text("₺" + (item.UnitPrice + item.CustomizationPrice));
                                table.Cell().Element(BodyCellStyle).Text("₺" + lineTotal);
                            }
                        });

                        column.Item().AlignRight().Text("Total Price: ₺" + order.TotalPrice)
                            .FontSize(18)
                            .Bold()
                            .FontColor(Colors.Pink.Medium);
                    });

                    page.Footer().AlignCenter().Text("Thank you for choosing GOGOFOOD.")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf();
        }

        private byte[] GenerateAgreementPdf(Order order, Caretaker? caretaker, List<OrderItem> items)
        {
            var agreementTitle = caretaker == null
                ? "GOGOFOOD CUSTOMER AGREEMENT"
                : "GOGOFOOD RESTAURANT AGREEMENT";

            var agreementTotal = items.Sum(x => (x.UnitPrice + x.CustomizationPrice) * x.Quantity);

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(11));

                    page.Header().Column(column =>
                    {
                        column.Item().Text(agreementTitle)
                            .FontSize(22)
                            .Bold()
                            .FontColor(Colors.Pink.Medium);

                        column.Item().Text("This agreement PDF is generated dynamically with order-specific data.")
                            .FontSize(10)
                            .FontColor(Colors.Grey.Darken1);
                    });

                    page.Content().PaddingVertical(20).Column(column =>
                    {
                        column.Spacing(12);

                        column.Item().Text("Agreement Details").FontSize(16).Bold();
                        column.Item().Text("Order ID: " + order.OrderId);
                        column.Item().Text("Order Date: " + order.OrderDate.ToString("dd.MM.yyyy HH:mm"));
                        column.Item().Text("Customer: " + (order.User?.Name ?? "Unknown"));
                        column.Item().Text("Customer Email: " + (order.User?.Email ?? "Unknown"));

                        if (caretaker != null)
                        {
                            column.Item().Text("Restaurant: " + caretaker.RestaurantName);
                            column.Item().Text("Caretaker: " + caretaker.Name);
                            column.Item().Text("Address: " + BuildCaretakerAddress(caretaker));
                        }
                        else
                        {
                            column.Item().Text("Agreement Type: Customer order agreement");
                        }

                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        column.Item().Text("Agreement Terms").FontSize(16).Bold();
                        column.Item().Text("1. This document confirms that the order was created through the GOGOFOOD system.");
                        column.Item().Text("2. The listed items, quantities, prices, and customizations belong only to this order.");
                        column.Item().Text("3. The payment process is a simulation for the course project and no real transaction is processed.");
                        column.Item().Text("4. The restaurant is responsible for preparing the listed menu items according to the selected customizations.");
                        column.Item().Text("5. The customer may use the receipt and this agreement as order documentation.");

                        column.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                        column.Item().Text("Items Included in This Agreement").FontSize(16).Bold();

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(55);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("Item");
                                header.Cell().Element(HeaderCellStyle).Text("Qty");
                                header.Cell().Element(HeaderCellStyle).Text("Customization");
                                header.Cell().Element(HeaderCellStyle).Text("Total");
                            });

                            foreach (var item in items)
                            {
                                var lineTotal = (item.UnitPrice + item.CustomizationPrice) * item.Quantity;

                                var customization = string.IsNullOrWhiteSpace(item.CustomizationName)
                                    ? "No customization"
                                    : item.CustomizationName;

                                table.Cell().Element(BodyCellStyle).Text(item.MenuItem?.Name ?? "Menu Item");
                                table.Cell().Element(BodyCellStyle).Text(item.Quantity.ToString());
                                table.Cell().Element(BodyCellStyle).Text(customization);
                                table.Cell().Element(BodyCellStyle).Text("₺" + lineTotal);
                            }
                        });

                        column.Item().AlignRight().Text("Agreement Total: ₺" + agreementTotal)
                            .FontSize(18)
                            .Bold()
                            .FontColor(Colors.Pink.Medium);
                    });

                    page.Footer().AlignCenter().Text("Generated by GOGOFOOD Document Generation System")
                        .FontSize(10)
                        .FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf();
        }

        private static IContainer HeaderCellStyle(IContainer container)
        {
            return container
                .Background(Colors.Grey.Lighten3)
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten1)
                .Padding(6);
        }

        private static IContainer BodyCellStyle(IContainer container)
        {
            return container
                .BorderBottom(1)
                .BorderColor(Colors.Grey.Lighten2)
                .Padding(6);
        }

        private string BuildCaretakerAddress(Caretaker caretaker)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(caretaker.Address)) parts.Add(caretaker.Address);
            if (!string.IsNullOrWhiteSpace(caretaker.Street)) parts.Add(caretaker.Street);
            if (!string.IsNullOrWhiteSpace(caretaker.Neighborhood)) parts.Add(caretaker.Neighborhood);
            if (!string.IsNullOrWhiteSpace(caretaker.District)) parts.Add(caretaker.District);
            if (!string.IsNullOrWhiteSpace(caretaker.City)) parts.Add(caretaker.City);
            if (!string.IsNullOrWhiteSpace(caretaker.AddressDetail)) parts.Add(caretaker.AddressDetail);

            return parts.Count == 0 ? "No address information" : string.Join(", ", parts);
        }

        private string BuildUserOrderEmailBody(Order order)
        {
            var body = new StringBuilder();

            body.Append("<h2>Thank you for your order!</h2>");
            body.Append("<p>Your GOGOFOOD order has been completed successfully.</p>");
            body.Append("<p>Your receipt PDF and agreement PDF are attached to this email.</p>");

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
            body.Append("<p>The agreement PDF for this order is attached to this email.</p>");

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