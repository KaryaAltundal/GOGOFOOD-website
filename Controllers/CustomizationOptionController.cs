using GOGOFOOD.Data;
using GOGOFOOD.Models;
using Microsoft.AspNetCore.Mvc;
using System.Linq;

namespace GOGOFOOD.Controllers
{
    public class CustomizationOptionController : Controller
    {
        private readonly GogoFoodContext _context;

        public CustomizationOptionController(GogoFoodContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Create(int menuItemId)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Caretaker")
            {
                return RedirectToAction("Login", "Account");
            }

            if (menuItemId == 0)
            {
                return RedirectToAction("Index", "MenuItem");
            }

            ViewBag.MenuItemId = menuItemId;

            return View();
        }

        [HttpPost]
        public IActionResult Create(CustomizationOption option)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Caretaker")
            {
                return RedirectToAction("Login", "Account");
            }

            if (option.MenuItemId == 0)
            {
                return RedirectToAction("Index", "MenuItem");
            }

            _context.CustomizationOptions.Add(option);
            _context.SaveChanges();

            return RedirectToAction("Index", "CustomizationOption", new { menuItemId = option.MenuItemId });
        }

        public IActionResult Index(int menuItemId)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Caretaker")
            {
                return RedirectToAction("Login", "Account");
            }

            var options = _context.CustomizationOptions
                .Where(x => x.MenuItemId == menuItemId)
                .ToList();

            ViewBag.MenuItemId = menuItemId;

            return View(options);
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Caretaker")
            {
                return RedirectToAction("Login", "Account");
            }

            var option = _context.CustomizationOptions
                .FirstOrDefault(x => x.CustomizationOptionId == id);

            if (option == null)
            {
                return RedirectToAction("Index", "MenuItem");
            }

            return View(option);
        }

        [HttpPost]
        public IActionResult Edit(CustomizationOption option)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Caretaker")
            {
                return RedirectToAction("Login", "Account");
            }

            _context.CustomizationOptions.Update(option);
            _context.SaveChanges();

            return RedirectToAction("Index", "CustomizationOption", new { menuItemId = option.MenuItemId });
        }

        public IActionResult Delete(int id)
        {
            var role = HttpContext.Session.GetString("UserRole");

            if (role != "Caretaker")
            {
                return RedirectToAction("Login", "Account");
            }

            var option = _context.CustomizationOptions
                .FirstOrDefault(x => x.CustomizationOptionId == id);

            if (option != null)
            {
                int menuItemId = option.MenuItemId;

                _context.CustomizationOptions.Remove(option);
                _context.SaveChanges();

                return RedirectToAction("Index", "CustomizationOption", new { menuItemId = menuItemId });
            }

            return RedirectToAction("Index", "MenuItem");
        }
    }
}