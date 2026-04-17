using GOGOFOOD.Data;
using GOGOFOOD.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GOGOFOOD.Controllers
{
    public class MenuItemController : Controller
    {
        private readonly GogoFoodContext _context;

        public MenuItemController(GogoFoodContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var menuItems = _context.MenuItems.ToList();
            return View(menuItems);
        }

        [HttpGet]
        public IActionResult Create()
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            if (role != "Caretaker")
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        [HttpPost]
        public IActionResult Create(MenuItem item)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            if (role != "Caretaker")
            {
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                _context.MenuItems.Add(item);
                _context.SaveChanges();

                return RedirectToAction("Index", "MenuItem");
            }

            return View(item);
        }
    }
}