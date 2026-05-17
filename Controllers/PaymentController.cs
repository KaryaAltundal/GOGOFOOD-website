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
using System.Text.RegularExpressions;

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
        [ValidateAntiForgeryToken]
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

            string? paymentValidationError = ValidatePaymentInformation(
                cardHolder,
                cardNumber,
                expirationDate,
                cvv
            );

            if (!string.IsNullOrWhiteSpace(paymentValidationError))
            {
                _logService.AddLog(
                    "Payment Failed",
                    "Payment validation failed. Reason: " + paymentValidationError,
                    userEmail,
                    userRole
                );

                ViewBag.PaymentError = paymentValidationError;

                return View("Checkout", cart);
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

        private string? ValidatePaymentInformation(
            string cardHolder,
            string cardNumber,
            string expirationDate,
            string cvv)
        {
            if (string.IsNullOrWhiteSpace(cardHolder) || cardHolder.Trim().Length < 3)
            {
                return "Please enter the card holder name.";
            }

            if (!Regex.IsMatch(cardHolder.Trim(), @"^[a-zA-ZçÇğĞıİöÖşŞüÜ\s]+$"))
            {
                return "Card holder name can only contain letters.";
            }

            if (string.IsNullOrWhiteSpace(cardNumber) ||
                cardNumber.Length != 16 ||
                !cardNumber.All(char.IsDigit))
            {
                return "Card number must contain exactly 16 digits.";
            }

            if (!IsExpirationDateValid(expirationDate))
            {
                return "The card expiration date is invalid or expired.";
            }

            if (string.IsNullOrWhiteSpace(cvv) ||
                cvv.Length != 3 ||
                !cvv.All(char.IsDigit))
            {
                return "CVV must contain exactly 3 digits.";
            }

            return null;
        }

        private bool IsExpirationDateValid(string expirationDate)
        {
            if (string.IsNullOrWhiteSpace(expirationDate))
            {
                return false;
            }

            var parts = expirationDate.Split('/');

            if (parts.Length != 2)
            {
                return false;
            }

            if (!int.TryParse(parts[0], out int month))
            {
                return false;
            }

            if (!int.TryParse(parts[1], out int year))
            {
                return false;
            }

            if (month < 1 || month > 12)
            {
                return false;
            }

            int fullYear = 2000 + year;

            try
            {
                var expirationLimit = new DateTime(fullYear, month, 1).AddMonths(1);
                var today = DateTime.Now.Date;

                return expirationLimit > today;
            }
            catch
            {
                return false;
            }
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

            if (!IsDemoEmail(order.User.Email))
            {
                _emailService.SendEmail(
                    order.User.Email,
                    "GOGOFOOD Order Confirmation - Order #" + order.OrderId,
                    userEmailBody,
                    new List<(string FileName, byte[] Content, string ContentType)>
                    {
                        ("GOGOFOOD-Receipt-Order-" + order.OrderId + ".pdf", receiptPdf, "application/pdf"),
                        ("GOGOFOOD-Catering-Sozlesmesi-Siparis-" + order.OrderId + ".pdf", userAgreementPdf, "application/pdf")
                    }
                );

                _logService.AddLog(
                    "PDF Sent",
                    "Receipt PDF and agreement PDF were sent to user for Order ID: " + order.OrderId,
                    order.User.Email,
                    "System"
                );
            }
            else
            {
                _logService.AddLog(
                    "Email Skipped",
                    "User email was skipped because it is a demo email address: " + order.User.Email,
                    order.User.Email,
                    "System"
                );
            }

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
                        "Caterer email could not be sent because caterer user was not found. CaretakerId: " + caretakerId,
                        order.User.Email,
                        "System"
                    );

                    continue;
                }

                if (IsDemoEmail(caretaker.User.Email))
                {
                    _logService.AddLog(
                        "Email Skipped",
                        "Caterer email was skipped because it is a demo email address: " + caretaker.User.Email,
                        caretaker.User.Email,
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
                        ("GOGOFOOD-Caterer-Sozlesmesi-Siparis-" + order.OrderId + ".pdf", caretakerAgreementPdf, "application/pdf")
                    }
                );

                _logService.AddLog(
                    "PDF Sent",
                    "Agreement PDF was sent to caterer for Order ID: " + order.OrderId + " CaretakerId: " + caretakerId,
                    caretaker.User.Email,
                    "System"
                );
            }
        }

        private bool IsDemoEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return true;
            }

            return email.Trim().EndsWith("@gogofood.test", StringComparison.OrdinalIgnoreCase);
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
            var agreementNo = caretaker == null
                ? "GOGO-" + order.OrderId + "-CUSTOMER"
                : "GOGO-" + order.OrderId + "-CATERER-" + caretaker.CaretakerId;

            var agreementDate = order.OrderDate.ToString("dd.MM.yyyy");
            var agreementDateTime = order.OrderDate.ToString("dd.MM.yyyy HH:mm");
            var agreementTotal = items.Sum(x => (x.UnitPrice + x.CustomizationPrice) * x.Quantity);

            var customerName = order.User?.Name ?? "Bilinmiyor";
            var customerEmail = order.User?.Email ?? "Bilinmiyor";
            var customerAddress = BuildUserAddress(order.User);

            var providerName = caretaker == null
                ? "GOGOFOOD Platformu ve Siparişte Yer Alan Catering Firmaları"
                : caretaker.RestaurantName;

            var providerResponsibleName = caretaker == null
                ? "GOGOFOOD"
                : caretaker.Name;

            var providerEmail = caretaker?.User?.Email ?? "Sistemde kayıtlı değil";

            var providerAddress = caretaker == null
                ? "GOGOFOOD online catering sistemi"
                : BuildCaretakerAddress(caretaker);

            var serviceLocation = customerAddress == "No address information"
                ? "Müşteri tarafından sistemde belirtilen teslimat/hizmet adresi"
                : customerAddress;

            var maxGuestCount = items.Count > 0 ? items.Max(x => x.Quantity) : 0;

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(35);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(column =>
                    {
                        column.Item().AlignCenter().Text("CATERING HİZMET SÖZLEŞMESİ")
                            .FontSize(18)
                            .Bold()
                            .FontColor(Colors.Pink.Medium);

                        column.Item().PaddingTop(6).Row(row =>
                        {
                            row.RelativeItem().Text("Sözleşme Tarihi: " + agreementDate).FontSize(9);
                            row.RelativeItem().AlignRight().Text("Sözleşme No: " + agreementNo).FontSize(9);
                        });

                        column.Item().PaddingTop(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                    });

                    page.Content().PaddingVertical(15).Column(column =>
                    {
                        column.Spacing(9);

                        column.Item().Text(
                            "Bu Catering Hizmet Sözleşmesi, aşağıda bilgileri yer alan taraflar arasında, " +
                            "6098 sayılı Türk Borçlar Kanunu, 6502 sayılı Tüketicinin Korunması Hakkında Kanun " +
                            "ve ilgili mevzuat çerçevesinde, GOGOFOOD sistemi üzerinden oluşturulan catering siparişine " +
                            "ilişkin esasların belirlenmesi amacıyla elektronik olarak oluşturulmuştur.")
                            .FontSize(9);

                        column.Item().Element(AgreementSectionTitle).Text("1. TARAFLAR");

                        column.Item().Text("Hizmet Alan (Müşteri):").Bold();
                        column.Item().Text("Adı-Soyadı / Unvanı: " + customerName);
                        column.Item().Text("T.C. Kimlik No / Vergi No: Sistemde kayıtlı değil");
                        column.Item().Text("Adres: " + customerAddress);
                        column.Item().Text("Telefon: Sistemde kayıtlı değil");
                        column.Item().Text("E-posta: " + customerEmail);

                        column.Item().PaddingTop(4).Text("Hizmet Sağlayıcı (Catering Firması):").Bold();
                        column.Item().Text("Adı-Soyadı / Unvanı: " + providerName);
                        column.Item().Text("Yetkili Kişi: " + providerResponsibleName);
                        column.Item().Text("T.C. Kimlik No / Vergi No: Sistemde kayıtlı değil");
                        column.Item().Text("Adres: " + providerAddress);
                        column.Item().Text("Telefon: Sistemde kayıtlı değil");
                        column.Item().Text("E-posta: " + providerEmail);

                        column.Item().Text(
                            "Taraflar işbu sözleşmede “Müşteri” ve “Catering Firması” olarak anılacaktır.");

                        column.Item().Element(AgreementSectionTitle).Text("2. SÖZLEŞMENİN KONUSU");

                        column.Item().Text(
                            "Bu sözleşmenin konusu, Catering Firması tarafından Müşteri’ye, GOGOFOOD sistemi üzerinden " +
                            "oluşturulan sipariş kapsamında yemek ve içecek hizmetlerinin sağlanmasıdır.");

                        column.Item().Text("Hizmet Türü: Organizasyon catering / yemek ve içecek hizmeti");
                        column.Item().Text("Hizmet Lokasyonu: " + serviceLocation);
                        column.Item().Text("Hizmet Tarihi: " + agreementDateTime);
                        column.Item().Text("Servis Günleri ve Saatleri: Sipariş detaylarına göre belirlenmiştir.");
                        column.Item().Text("Kişi Sayısı: " + maxGuestCount + " kişi. Kalem bazlı adetler aşağıdaki tabloda gösterilmiştir.");
                        column.Item().Text("Menü İçeriği: GOGOFOOD sipariş detaylarında belirtilen ürünlerden oluşmaktadır.");
                        column.Item().Text("Teslim Şekli: Catering hizmeti / organizasyon teslimatı");

                        column.Item().Element(AgreementSectionTitle).Text("3. CATERING FİRMASININ YÜKÜMLÜLÜKLERİ");

                        column.Item().Text("3.1 Kalite ve Sağlık Standartları").Bold();
                        column.Item().Text("Catering Firması, Türk Gıda Kodeksi ve ilgili hijyen yönetmeliklerine uygun üretim yapmakla yükümlüdür.");
                        column.Item().Text("Yemekler, mümkün olduğunca günlük ve taze malzemelerle hazırlanacaktır.");
                        column.Item().Text("Yemeklerin hazırlanması, taşınması ve servisi sırasında hijyen kurallarına uyulacaktır.");

                        column.Item().Text("3.2 Zamanında Teslimat ve Servis").Bold();
                        column.Item().Text("Yemekler, siparişte belirtilen gün ve saat aralığına uygun şekilde hazırlanacaktır.");
                        column.Item().Text("Catering Firması, teslimat ve servis sürecinde sipariş detaylarına uygun hareket etmekle yükümlüdür.");

                        column.Item().Text("3.3 Menü Değişiklikleri ve Alerjen İçerikler").Bold();
                        column.Item().Text("Menüde değişiklik yapılması gerekiyorsa, Müşteri’ye uygun süre içinde bilgi verilmelidir.");
                        column.Item().Text("Catering Firması, menü içeriğinde alerjen bileşenler bulunuyorsa bunları açıkça belirtmekle yükümlüdür.");

                        column.Item().Element(AgreementSectionTitle).Text("4. MÜŞTERİNİN YÜKÜMLÜLÜKLERİ");

                        column.Item().Text("4.1 Ödeme Şartları").Bold();
                        column.Item().Text("Toplam Hizmet Bedeli: ₺" + agreementTotal);
                        column.Item().Text("Ödeme Planı: Bu proje kapsamında ödeme işlemi simülasyon olarak tamamlanmıştır.");
                        column.Item().Text("Ödeme Durumu: Completed");

                        column.Item().Text("4.2 Kişi Sayısı ve Bilgilendirme").Bold();
                        column.Item().Text("Müşteri, sipariş kişi sayısı ve teslimat bilgilerini doğru girmekle yükümlüdür.");
                        column.Item().Text("Kişi sayısında veya hizmet bilgilerinde değişiklik yapılması durumunda Catering Firması’na uygun süre içinde bilgi verilmelidir.");

                        column.Item().Text("4.3 Servis Alanı Hazırlığı").Bold();
                        column.Item().Text("Müşteri, yemek servisinin yapılacağı alanın güvenli, erişilebilir ve hijyenik olmasını sağlamakla yükümlüdür.");

                        column.Item().Element(AgreementSectionTitle).Text("5. HİZMETİN AKSAMASI VE CEZAİ ŞARTLAR");

                        column.Item().Text("Catering Firması, siparişin eksik veya hatalı hazırlanması durumunda Müşteri ile iletişime geçerek durumu çözmekle yükümlüdür.");
                        column.Item().Text("Yemeklerin hijyenik şartlara uygun olmadığı resmi belgelerle tespit edilirse, Müşteri sözleşmeyi feshetme ve zararının karşılanmasını talep etme hakkına sahiptir.");
                        column.Item().Text("Müşteri, ödeme ve sipariş bilgilerini zamanında ve doğru şekilde sağlamaktan sorumludur.");

                        column.Item().Element(AgreementSectionTitle).Text("6. SÖZLEŞMENİN FESHİ VE MÜCBİR SEBEPLER");

                        column.Item().Text("6.1 Fesih Koşulları").Bold();
                        column.Item().Text("Müşteri, hizmet kalitesinden memnun kalmaması veya sipariş detaylarında önemli bir problem yaşanması durumunda Catering Firması ile iletişime geçebilir.");
                        column.Item().Text("Catering Firması, gerekli sipariş bilgilerinin sağlanmaması veya ödeme sürecinde problem oluşması durumunda hizmeti durdurma hakkına sahiptir.");

                        column.Item().Text("6.2 Mücbir Sebepler").Bold();
                        column.Item().Text("Doğal afetler, salgın hastalıklar, grev, savaş, kamu düzenini etkileyen olaylar gibi mücbir sebepler nedeniyle hizmetin aksaması durumunda taraflar sorumlu tutulamaz.");

                        column.Item().Element(AgreementSectionTitle).Text("7. GİZLİLİK VE TİCARİ SIRLAR");

                        column.Item().Text("Taraflar, işbu sözleşme kapsamında edindikleri kişisel ve ticari bilgileri üçüncü kişilerle paylaşamaz.");
                        column.Item().Text("Catering Firması, müşteriye özel menü, hizmet detayı ve sipariş bilgilerini izinsiz kullanamaz.");

                        column.Item().Element(AgreementSectionTitle).Text("8. UYUŞMAZLIKLARIN ÇÖZÜMÜ");

                        column.Item().Text("Taraflar, işbu sözleşmeden doğabilecek ihtilafları öncelikle müzakere yoluyla çözmeye çalışacaktır.");
                        column.Item().Text("Taraflar arasında çözülemeyen ihtilaflarda, ilgili Türkiye Cumhuriyeti Mahkemeleri ve İcra Daireleri yetkilidir.");

                        column.Item().Element(AgreementSectionTitle).Text("9. SON HÜKÜMLER");

                        column.Item().Text("İşbu sözleşme, GOGOFOOD sistemi tarafından elektronik olarak oluşturulmuştur.");
                        column.Item().Text("Taraflardan herhangi biri, yazılı veya sistem üzerinden kayıtlı bildirim olmadan sözleşme hükümlerinde değişiklik yapamaz.");
                        column.Item().Text("Taraflar, siparişin tamamlanması ile birlikte sözleşme maddelerini kabul etmiş sayılır.");

                        column.Item().Element(AgreementSectionTitle).Text("10. SİPARİŞE DAHİL ÜRÜNLER");

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.ConstantColumn(55);
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderCellStyle).Text("Ürün");
                                header.Cell().Element(HeaderCellStyle).Text("Kişi");
                                header.Cell().Element(HeaderCellStyle).Text("Özelleştirme");
                                header.Cell().Element(HeaderCellStyle).Text("Toplam");
                            });

                            foreach (var item in items)
                            {
                                var lineTotal = (item.UnitPrice + item.CustomizationPrice) * item.Quantity;

                                var customization = string.IsNullOrWhiteSpace(item.CustomizationName)
                                    ? "Özelleştirme yok"
                                    : item.CustomizationName;

                                table.Cell().Element(BodyCellStyle).Text(item.MenuItem?.Name ?? "Menü Ürünü");
                                table.Cell().Element(BodyCellStyle).Text(item.Quantity.ToString());
                                table.Cell().Element(BodyCellStyle).Text(customization);
                                table.Cell().Element(BodyCellStyle).Text("₺" + lineTotal);
                            }
                        });

                        column.Item().AlignRight().Text("Toplam Hizmet Bedeli: ₺" + agreementTotal)
                            .FontSize(13)
                            .Bold()
                            .FontColor(Colors.Pink.Medium);

                        column.Item().PaddingTop(15).Row(row =>
                        {
                            row.RelativeItem().Column(left =>
                            {
                                left.Item().Text("HİZMET SAĞLAYICI").Bold();
                                left.Item().Text("Adı-Soyadı / Unvanı: " + providerName);
                                left.Item().PaddingTop(20).Text("İmza: ____________________");
                            });

                            row.RelativeItem().Column(right =>
                            {
                                right.Item().Text("MÜŞTERİ").Bold();
                                right.Item().Text("Adı-Soyadı / Unvanı: " + customerName);
                                right.Item().PaddingTop(20).Text("İmza: ____________________");
                            });
                        });
                    });

                    page.Footer().AlignCenter().Text(
                        "Bu belge GOGOFOOD sistemi tarafından elektronik olarak oluşturulmuştur.")
                        .FontSize(8)
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

        private static IContainer AgreementSectionTitle(IContainer container)
        {
            return container
                .PaddingTop(7)
                .PaddingBottom(3)
                .BorderBottom(1)
                .BorderColor(Colors.Pink.Lighten2);
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

        private string BuildUserAddress(User? user)
        {
            if (user == null)
            {
                return "No address information";
            }

            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(user.AddressDetail)) parts.Add(user.AddressDetail);
            if (!string.IsNullOrWhiteSpace(user.Street)) parts.Add(user.Street);
            if (!string.IsNullOrWhiteSpace(user.Neighborhood)) parts.Add(user.Neighborhood);
            if (!string.IsNullOrWhiteSpace(user.District)) parts.Add(user.District);
            if (!string.IsNullOrWhiteSpace(user.City)) parts.Add(user.City);

            return parts.Count == 0 ? "No address information" : string.Join(", ", parts);
        }

        private string BuildUserOrderEmailBody(Order order)
        {
            var body = new StringBuilder();

            body.Append("<h2>Thank you for your order!</h2>");
            body.Append("<p>Your GOGOFOOD order has been completed successfully.</p>");
            body.Append("<p>Your receipt PDF and catering agreement PDF are attached to this email.</p>");

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
            body.Append("<p>The catering agreement PDF for this order is attached to this email.</p>");

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