using GOGOFOOD.Data;
using GOGOFOOD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        private bool IsCaretakerOwnerOfMenuItem(int menuItemId)
        {
            var role = HttpContext.Session.GetString("UserRole");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");

            if (role != "Caretaker" || caretakerId == null)
            {
                return false;
            }

            return _context.MenuItems.Any(x =>
                x.MenuItemId == menuItemId &&
                x.CaretakerId == caretakerId.Value);
        }

        [HttpGet]
        public IActionResult Create(int menuItemId)
        {
            if (!IsCaretakerOwnerOfMenuItem(menuItemId))
            {
                return RedirectToAction("Index", "MenuItem");
            }

            ViewBag.MenuItemId = menuItemId;

            return View();
        }

        [HttpPost]
        public IActionResult Create(CustomizationOption option)
        {
            if (!IsCaretakerOwnerOfMenuItem(option.MenuItemId))
            {
                return RedirectToAction("Index", "MenuItem");
            }

            _context.CustomizationOptions.Add(option);
            _context.SaveChanges();

            return RedirectToAction("Index", "CustomizationOption", new { menuItemId = option.MenuItemId });
        }

        public IActionResult Index(int menuItemId)
        {
            if (!IsCaretakerOwnerOfMenuItem(menuItemId))
            {
                return RedirectToAction("Index", "MenuItem");
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
            var option = _context.CustomizationOptions
                .Include(x => x.MenuItem)
                .FirstOrDefault(x => x.CustomizationOptionId == id);

            if (option == null)
            {
                return RedirectToAction("Index", "MenuItem");
            }

            if (!IsCaretakerOwnerOfMenuItem(option.MenuItemId))
            {
                return RedirectToAction("Index", "MenuItem");
            }

            return View(option);
        }

        [HttpPost]
        public IActionResult Edit(CustomizationOption option)
        {
            if (!IsCaretakerOwnerOfMenuItem(option.MenuItemId))
            {
                return RedirectToAction("Index", "MenuItem");
            }

            _context.CustomizationOptions.Update(option);
            _context.SaveChanges();

            return RedirectToAction("Index", "CustomizationOption", new { menuItemId = option.MenuItemId });
        }

        public IActionResult Delete(int id)
        {
            var option = _context.CustomizationOptions
                .FirstOrDefault(x => x.CustomizationOptionId == id);

            if (option == null)
            {
                return RedirectToAction("Index", "MenuItem");
            }

            if (!IsCaretakerOwnerOfMenuItem(option.MenuItemId))
            {
                return RedirectToAction("Index", "MenuItem");
            }

            int menuItemId = option.MenuItemId;

            _context.CustomizationOptions.Remove(option);
            _context.SaveChanges();

            return RedirectToAction("Index", "CustomizationOption", new { menuItemId = menuItemId });
        }
    }
}