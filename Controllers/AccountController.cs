using GOGOFOOD.Data;
using GOGOFOOD.Models;
using GOGOFOOD.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace GOGOFOOD.Controllers
{
    public class AccountController : Controller
    {
        private readonly GogoFoodContext _context;
        private readonly LogService _logService;
        private readonly EmailService _emailService;
        private readonly PasswordHasher<User> _passwordHasher;
        private readonly IWebHostEnvironment _environment;

        public AccountController(
            GogoFoodContext context,
            LogService logService,
            EmailService emailService,
            IWebHostEnvironment environment)
        {
            _context = context;
            _logService = logService;
            _emailService = emailService;
            _environment = environment;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(User user)
        {
            if (!IsPasswordValid(user.Password))
            {
                ViewBag.Error = "Password must be at least 6 characters long and include at least one punctuation mark.";

                _logService.AddLog(
                    "Register Failed",
                    "Registration failed because password did not meet security rules.",
                    user.Email,
                    user.Role
                );

                return View(user);
            }

            if (ModelState.IsValid)
            {
                var existingUser = _context.Users.FirstOrDefault(x => x.Email == user.Email);

                if (existingUser != null)
                {
                    ViewBag.Error = "This email is already registered.";

                    _logService.AddLog(
                        "Register Failed",
                        "Registration failed because email already exists.",
                        user.Email,
                        user.Role
                    );

                    return View(user);
                }

                user.IsDeleted = false;
                user.Password = _passwordHasher.HashPassword(user, user.Password);

                _context.Users.Add(user);
                _context.SaveChanges();

                if (user.Role == "Caretaker")
                {
                    var caretaker = new Caretaker
                    {
                        Name = user.Name,
                        RestaurantName = user.Name + "'s Restaurant",
                        Address = "Address not entered yet",
                        City = user.City ?? string.Empty,
                        District = user.District ?? string.Empty,
                        Neighborhood = user.Neighborhood ?? string.Empty,
                        Street = user.Street ?? string.Empty,
                        AddressDetail = user.AddressDetail ?? string.Empty,
                        Latitude = user.Latitude,
                        Longitude = user.Longitude,
                        IsDeleted = false,
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
            var user = _context.Users.FirstOrDefault(x => x.Email == email && !x.IsDeleted);

            if (user != null)
            {
                var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.Password, password);

                if (passwordResult == PasswordVerificationResult.Success ||
                    passwordResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    if (IsDemoAccount(user))
                    {
                        return CompleteLogin(
                            user,
                            "Login Success",
                            "Demo account logged in without two-factor authentication."
                        );
                    }

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

            var user = _context.Users.FirstOrDefault(x =>
                x.UserId == pendingUserId.Value &&
                !x.IsDeleted);

            if (user == null)
            {
                ClearTwoFactorSession();
                return RedirectToAction("Login");
            }

            return CompleteLogin(
                user,
                "Login Success",
                "User logged in successfully with two-factor authentication."
            );
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

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ForgotPassword(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                ViewBag.Error = "Please enter your email address.";
                return View();
            }

            var trimmedEmail = email.Trim();

            var user = _context.Users.FirstOrDefault(x =>
                x.Email == trimmedEmail &&
                !x.IsDeleted);

            if (user == null)
            {
                _logService.AddLog(
                    "Password Reset Failed",
                    "Password reset requested for non-existing or deleted email: " + trimmedEmail,
                    trimmedEmail,
                    "Unknown"
                );

                ViewBag.Error = "No active account was found with this email.";
                return View();
            }

            if (IsDemoEmail(user.Email))
            {
                _logService.AddLog(
                    "Password Reset Failed",
                    "Password reset email was not sent because this is a demo email address: " + user.Email,
                    user.Email,
                    user.Role
                );

                ViewBag.Error = "Password reset email cannot be sent to demo accounts. Please use a real email address.";
                return View();
            }

            var random = new Random();
            var resetCode = random.Next(100000, 999999).ToString();

            HttpContext.Session.SetInt32("ResetUserId", user.UserId);
            HttpContext.Session.SetString("ResetUserEmail", user.Email);
            HttpContext.Session.SetString("ResetUserRole", user.Role);
            HttpContext.Session.SetString("ResetPasswordCode", resetCode);
            HttpContext.Session.SetString("ResetPasswordExpireTime", DateTime.Now.AddMinutes(5).ToString());

            string emailBody = BuildPasswordResetEmailBody(user.Name, resetCode);

            _emailService.SendEmail(
                user.Email,
                "GOGOFOOD Password Reset Code",
                emailBody
            );

            _logService.AddLog(
                "Password Reset Code Sent",
                "Password reset code sent to: " + user.Email,
                user.Email,
                user.Role
            );

            TempData["ResetMessage"] = "A password reset code was sent to your email.";

            return RedirectToAction("ResetPassword");
        }

        [HttpGet]
        public IActionResult ResetPassword()
        {
            var resetEmail = HttpContext.Session.GetString("ResetUserEmail");

            if (string.IsNullOrWhiteSpace(resetEmail))
            {
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = resetEmail;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(string code, string newPassword, string confirmPassword)
        {
            var resetUserId = HttpContext.Session.GetInt32("ResetUserId");
            var resetEmail = HttpContext.Session.GetString("ResetUserEmail");
            var resetRole = HttpContext.Session.GetString("ResetUserRole");
            var savedCode = HttpContext.Session.GetString("ResetPasswordCode");
            var expireTimeText = HttpContext.Session.GetString("ResetPasswordExpireTime");

            if (resetUserId == null ||
                string.IsNullOrWhiteSpace(resetEmail) ||
                string.IsNullOrWhiteSpace(resetRole) ||
                string.IsNullOrWhiteSpace(savedCode) ||
                string.IsNullOrWhiteSpace(expireTimeText))
            {
                return RedirectToAction("ForgotPassword");
            }

            ViewBag.Email = resetEmail;

            DateTime expireTime = DateTime.Parse(expireTimeText);

            if (DateTime.Now > expireTime)
            {
                ClearResetPasswordSession();

                _logService.AddLog(
                    "Password Reset Failed",
                    "Password reset failed because the reset code expired.",
                    resetEmail,
                    resetRole
                );

                ViewBag.Error = "Reset code expired. Please request a new code.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(code) || code.Trim() != savedCode)
            {
                _logService.AddLog(
                    "Password Reset Failed",
                    "Password reset failed because the reset code was incorrect.",
                    resetEmail,
                    resetRole
                );

                ViewBag.Error = "Invalid reset code.";
                return View();
            }

            if (!IsPasswordValid(newPassword))
            {
                ViewBag.Error = "New password must be at least 6 characters long and include at least one punctuation mark.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ViewBag.Error = "New password and confirm password do not match.";
                return View();
            }

            var user = _context.Users.FirstOrDefault(x =>
                x.UserId == resetUserId.Value &&
                !x.IsDeleted);

            if (user == null)
            {
                ClearResetPasswordSession();
                return RedirectToAction("ForgotPassword");
            }

            user.Password = _passwordHasher.HashPassword(user, newPassword);

            _context.Users.Update(user);
            _context.SaveChanges();

            _logService.AddLog(
                "Password Reset Success",
                "Password was reset successfully for: " + user.Email,
                user.Email,
                user.Role
            );

            ClearResetPasswordSession();

            TempData["LoginMessage"] = "Your password was changed successfully. You can login with your new password.";

            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResendPasswordResetCode()
        {
            var resetUserId = HttpContext.Session.GetInt32("ResetUserId");
            var resetEmail = HttpContext.Session.GetString("ResetUserEmail");
            var resetRole = HttpContext.Session.GetString("ResetUserRole");

            if (resetUserId == null ||
                string.IsNullOrWhiteSpace(resetEmail) ||
                string.IsNullOrWhiteSpace(resetRole))
            {
                return RedirectToAction("ForgotPassword");
            }

            var user = _context.Users.FirstOrDefault(x =>
                x.UserId == resetUserId.Value &&
                !x.IsDeleted);

            if (user == null)
            {
                ClearResetPasswordSession();
                return RedirectToAction("ForgotPassword");
            }

            if (IsDemoEmail(user.Email))
            {
                ClearResetPasswordSession();

                _logService.AddLog(
                    "Password Reset Failed",
                    "Password reset code was not resent because this is a demo email address: " + user.Email,
                    user.Email,
                    user.Role
                );

                return RedirectToAction("ForgotPassword");
            }

            var random = new Random();
            var resetCode = random.Next(100000, 999999).ToString();

            HttpContext.Session.SetString("ResetPasswordCode", resetCode);
            HttpContext.Session.SetString("ResetPasswordExpireTime", DateTime.Now.AddMinutes(5).ToString());

            string emailBody = BuildPasswordResetEmailBody(user.Name, resetCode);

            _emailService.SendEmail(
                user.Email,
                "GOGOFOOD New Password Reset Code",
                emailBody
            );

            _logService.AddLog(
                "Password Reset Code Resent",
                "Password reset code resent to: " + user.Email,
                user.Email,
                user.Role
            );

            TempData["ResetMessage"] = "A new password reset code was sent to your email.";

            return RedirectToAction("ResetPassword");
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

        private IActionResult CompleteLogin(User user, string logTitle, string logMessage)
        {
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserEmail", user.Email);
            HttpContext.Session.SetString("UserRole", user.Role);
            HttpContext.Session.SetString("UserName", user.Name);

            if (user.Role == "Caretaker")
            {
                var caretaker = _context.Caretakers
                    .FirstOrDefault(x => x.UserId == user.UserId && !x.IsDeleted);

                if (caretaker == null)
                {
                    ClearTwoFactorSession();

                    _logService.AddLog(
                        "Login Blocked",
                        "Caretaker login blocked because caretaker profile is inactive. Email: " + user.Email,
                        user.Email,
                        user.Role
                    );

                    ViewBag.Error = "Your caretaker profile is inactive. Please contact admin.";
                    return View("Login");
                }

                HttpContext.Session.SetInt32("CaretakerId", caretaker.CaretakerId);
            }

            ClearTwoFactorSession();

            _logService.AddLog(
                logTitle,
                logMessage + " Email: " + user.Email,
                user.Email,
                user.Role
            );

            if (user.Role == "Caretaker")
            {
                return RedirectToAction("Create", "MenuItem");
            }

            return RedirectToAction("Index", "Home");
        }

        private bool IsPasswordValid(string? password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                return false;
            }

            if (password.Length < 6)
            {
                return false;
            }

            return password.Any(char.IsPunctuation);
        }

        private bool IsDemoAccount(User user)
        {
            if (string.IsNullOrEmpty(user.Email))
            {
                return false;
            }

            return _environment.IsDevelopment() &&
                   user.Email.EndsWith("@gogofood.test", StringComparison.OrdinalIgnoreCase);
        }

        private bool IsDemoEmail(string? email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return true;
            }

            return email.Trim().EndsWith("@gogofood.test", StringComparison.OrdinalIgnoreCase);
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

        private string BuildPasswordResetEmailBody(string name, string code)
        {
            return @"
                <div style='font-family: Arial, sans-serif; padding: 24px;'>
                    <h2 style='color:#FF0569;'>GOGOFOOD Password Reset</h2>
                    <p>Hello " + name + @",</p>
                    <p>You requested to reset your password. Please use the verification code below:</p>
                    <div style='font-size: 32px; font-weight: bold; color:#FF0569; background:#fff0f6; padding: 18px; border-radius: 14px; width: fit-content;'>
                        " + code + @"
                    </div>
                    <p>This code will expire in 5 minutes.</p>
                    <p>If you did not request a password reset, you can ignore this email.</p>
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

        private void ClearResetPasswordSession()
        {
            HttpContext.Session.Remove("ResetUserId");
            HttpContext.Session.Remove("ResetUserEmail");
            HttpContext.Session.Remove("ResetUserRole");
            HttpContext.Session.Remove("ResetPasswordCode");
            HttpContext.Session.Remove("ResetPasswordExpireTime");
        }
    }
}