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

        public AccountController(GogoFoodContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
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
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserName", user.Name);

                if (user.Role == "Caretaker")
                {
                    var caretaker = _context.Caretakers.FirstOrDefault(x => x.UserId == user.UserId);

                    if (caretaker == null)
                    {
                        caretaker = new Caretaker
                        {
                            Name = user.Name,
                            RestaurantName = user.Name + "'s Restaurant",
                            Address = "Address not entered yet",
                            UserId = user.UserId
                        };

                        _context.Caretakers.Add(caretaker);
                        _context.SaveChanges();
                    }

                    HttpContext.Session.SetInt32("CaretakerId", caretaker.CaretakerId);
                }

                _logService.AddLog(
                    "Login Success",
                    "User logged in successfully: " + user.Email,
                    user.Email,
                    user.Role
                );

                if (user.Role == "Caretaker")
                {
                    return RedirectToAction("Create", "MenuItem");
                }
                else if (user.Role == "User")
                {
                    return RedirectToAction("Index", "Home");
                }
                else if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Home");
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
    }
}