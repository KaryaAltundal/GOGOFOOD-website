using Microsoft.AspNetCore.Mvc;

namespace GOGOFOOD.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}