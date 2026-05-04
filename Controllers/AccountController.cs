using GOGOFOOD.Data;
using GOGOFOOD.Models;
using GOGOFOOD.Services;
using Microsoft.AspNetCore.Mvc;

namespace GOGOFOOD.Controllers
{
    public class AccountController : Controller
    {
        private readonly GogoFoodContext _context;
        private readonly LogService _logService;
        private readonly EmailService _emailService;

        public AccountController(
            GogoFoodContext context,
            LogService logService,
            EmailService emailService)
        {
            _context = context;
            _logService = logService;
            _emailService = emailService;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (ModelState.IsValid)
            {
                _context.Users.Add(user);
                _context.SaveChanges();

                if (user.Role == "Caretaker")
                {
                    var caretaker = new Caretaker
                    {
                        Name = user.Name,
                        RestaurantName = user.Name + "'s Restaurant",
                        Address = "Address not entered yet",
                        UserId = user.UserId
                    };

                    _context.Caretakers.Add(caretaker);
                    _context.SaveChanges();
                }

                _logService.AddLog(
                    "Register",
                    "New user registered: " + user.Email,
                    user.Email,
                    user.Role
                );

                return RedirectToAction("Login");
            }

            _logService.AddLog(
                "Register Failed",
                "User registration failed.",
                user.Email,
                user.Role
            );

            return View(user);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(x => x.Email == email && x.Password == password);

            if (user != null)
            {
                var random = new Random();
                var verificationCode = random.Next(100000, 999999).ToString();

                HttpContext.Session.SetInt32("PendingUserId", user.UserId);
                HttpContext.Session.SetString("PendingUserEmail", user.Email);
                HttpContext.Session.SetString("PendingUserRole", user.Role);
                HttpContext.Session.SetString("PendingUserName", user.Name);
                HttpContext.Session.SetString("TwoFactorCode", verificationCode);
                HttpContext.Session.SetString("TwoFactorExpireTime", DateTime.Now.AddMinutes(5).ToString());

                string emailBody = BuildTwoFactorEmailBody(user.Name, verificationCode);

                _emailService.SendEmail(
                    user.Email,
                    "GOGOFOOD Login Verification Code",
                    emailBody
                );

                _logService.AddLog(
                    "Two Factor Code Sent",
                    "Two-factor authentication code sent to: " + user.Email,
                    user.Email,
                    user.Role
                );

                return RedirectToAction("VerifyTwoFactor");
            }

            _logService.AddLog(
                "Login Failed",
                "Failed login attempt for email: " + email,
                email,
                "Unknown"
            );

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        [HttpGet]
        public IActionResult VerifyTwoFactor()
        {
            var pendingEmail = HttpContext.Session.GetString("PendingUserEmail");

            if (string.IsNullOrEmpty(pendingEmail))
            {
                return RedirectToAction("Login");
            }

            ViewBag.Email = pendingEmail;

            return View();
        }

        [HttpPost]
        public IActionResult VerifyTwoFactor(string code)
        {
            var pendingUserId = HttpContext.Session.GetInt32("PendingUserId");
            var pendingEmail = HttpContext.Session.GetString("PendingUserEmail");
            var pendingRole = HttpContext.Session.GetString("PendingUserRole");
            var pendingName = HttpContext.Session.GetString("PendingUserName");
            var savedCode = HttpContext.Session.GetString("TwoFactorCode");
            var expireTimeText = HttpContext.Session.GetString("TwoFactorExpireTime");

            if (pendingUserId == null ||
                string.IsNullOrEmpty(pendingEmail) ||
                string.IsNullOrEmpty(pendingRole) ||
                string.IsNullOrEmpty(pendingName) ||
                string.IsNullOrEmpty(savedCode) ||
                string.IsNullOrEmpty(expireTimeText))
            {
                return RedirectToAction("Login");
            }

            DateTime expireTime = DateTime.Parse(expireTimeText);

            if (DateTime.Now > expireTime)
            {
                ClearTwoFactorSession();

                _logService.AddLog(
                    "Two Factor Failed",
                    "Two-factor authentication failed because the code expired.",
                    pendingEmail,
                    pendingRole
                );

                ViewBag.Error = "Verification code expired. Please login again.";
                return View();
            }

            if (code != savedCode)
            {
                _logService.AddLog(
                    "Two Factor Failed",
                    "Two-factor authentication failed because the code was incorrect.",
                    pendingEmail,
                    pendingRole
                );

                ViewBag.Email = pendingEmail;
                ViewBag.Error = "Invalid verification code.";
                return View();
            }

            HttpContext.Session.SetInt32("UserId", pendingUserId.Value);
            HttpContext.Session.SetString("UserEmail", pendingEmail);
            HttpContext.Session.SetString("UserRole", pendingRole);
            HttpContext.Session.SetString("UserName", pendingName);

            if (pendingRole == "Caretaker")
            {
                var caretaker = _context.Caretakers.FirstOrDefault(x => x.UserId == pendingUserId.Value);

                if (caretaker == null)
                {
                    caretaker = new Caretaker
                    {
                        Name = pendingName,
                        RestaurantName = pendingName + "'s Restaurant",
                        Address = "Address not entered yet",
                        UserId = pendingUserId.Value
                    };

                    _context.Caretakers.Add(caretaker);
                    _context.SaveChanges();
                }

                HttpContext.Session.SetInt32("CaretakerId", caretaker.CaretakerId);
            }

            ClearTwoFactorSession();

            _logService.AddLog(
                "Login Success",
                "User logged in successfully with two-factor authentication: " + pendingEmail,
                pendingEmail,
                pendingRole
            );

            if (pendingRole == "Caretaker")
            {
                return RedirectToAction("Create", "MenuItem");
            }
            else if (pendingRole == "Admin")
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        public IActionResult ResendTwoFactorCode()
        {
            var pendingUserId = HttpContext.Session.GetInt32("PendingUserId");
            var pendingEmail = HttpContext.Session.GetString("PendingUserEmail");
            var pendingRole = HttpContext.Session.GetString("PendingUserRole");
            var pendingName = HttpContext.Session.GetString("PendingUserName");

            if (pendingUserId == null ||
                string.IsNullOrEmpty(pendingEmail) ||
                string.IsNullOrEmpty(pendingRole) ||
                string.IsNullOrEmpty(pendingName))
            {
                return RedirectToAction("Login");
            }

            var random = new Random();
            var verificationCode = random.Next(100000, 999999).ToString();

            HttpContext.Session.SetString("TwoFactorCode", verificationCode);
            HttpContext.Session.SetString("TwoFactorExpireTime", DateTime.Now.AddMinutes(5).ToString());

            string emailBody = BuildTwoFactorEmailBody(pendingName, verificationCode);

            _emailService.SendEmail(
                pendingEmail,
                "GOGOFOOD New Login Verification Code",
                emailBody
            );

            _logService.AddLog(
                "Two Factor Code Resent",
                "Two-factor authentication code resent to: " + pendingEmail,
                pendingEmail,
                pendingRole
            );

            TempData["TwoFactorMessage"] = "A new verification code was sent to your email.";

            return RedirectToAction("VerifyTwoFactor");
        }

        public IActionResult Logout()
        {
            var email = HttpContext.Session.GetString("UserEmail");
            var role = HttpContext.Session.GetString("UserRole");

            _logService.AddLog(
                "Logout",
                "User logged out: " + email,
                email,
                role
            );

            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        private string BuildTwoFactorEmailBody(string name, string code)
        {
            return @"
                <div style='font-family: Arial, sans-serif; padding: 24px;'>
                    <h2 style='color:#FF0569;'>GOGOFOOD Login Verification</h2>
                    <p>Hello " + name + @",</p>
                    <p>Please use the verification code below to complete your login:</p>
                    <div style='font-size: 32px; font-weight: bold; color:#FF0569; background:#fff0f6; padding: 18px; border-radius: 14px; width: fit-content;'>
                        " + code + @"
                    </div>
                    <p>This code will expire in 5 minutes.</p>
                    <p>If you did not try to login, you can ignore this email.</p>
                </div>";
        }

        private void ClearTwoFactorSession()
        {
            HttpContext.Session.Remove("PendingUserId");
            HttpContext.Session.Remove("PendingUserEmail");
            HttpContext.Session.Remove("PendingUserRole");
            HttpContext.Session.Remove("PendingUserName");
            HttpContext.Session.Remove("TwoFactorCode");
            HttpContext.Session.Remove("TwoFactorExpireTime");
        }
    }
}