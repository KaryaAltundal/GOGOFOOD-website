using GOGOFOOD.Data;
using GOGOFOOD.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
            var role = HttpContext.Session.GetString("UserRole");
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");

            var menuItemsQuery = _context.MenuItems
                .Include(m => m.CustomizationOptions)
                .Include(m => m.MenuItemRatings)
                .Include(m => m.Caretaker)
                .ThenInclude(c => c.CaretakerRatings)
                .AsQueryable();

            if (role == "Caretaker")
            {
                if (caretakerId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                menuItemsQuery = menuItemsQuery.Where(m => m.CaretakerId == caretakerId.Value);
            }

            var menuItems = menuItemsQuery.ToList();

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
            var caretakerId = HttpContext.Session.GetInt32("CaretakerId");

            if (string.IsNullOrEmpty(role))
            {
                return RedirectToAction("Login", "Account");
            }

            if (role != "Caretaker")
            {
                return RedirectToAction("Index", "Home");
            }

            if (caretakerId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            item.CaretakerId = caretakerId.Value;

            _context.MenuItems.Add(item);
            _context.SaveChanges();

            return RedirectToAction("Index", "MenuItem");
        }
    }
}