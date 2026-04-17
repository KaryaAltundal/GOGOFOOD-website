using GOGOFOOD.Data;
using GOGOFOOD.Models;
using Microsoft.AspNetCore.Mvc;

namespace GOGOFOOD.Controllers
{
    public class AccountController : Controller
    {
        private readonly GogoFoodContext _context;

        public AccountController(GogoFoodContext context)
        {
            _context = context;
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

                return RedirectToAction("Login");
            }

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
                HttpContext.Session.SetString("UserEmail", user.Email);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("UserName", user.Name);

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

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

       public IActionResult Logout()
{
    HttpContext.Session.Clear();
    return RedirectToAction("Index", "Home");
}
    }
}