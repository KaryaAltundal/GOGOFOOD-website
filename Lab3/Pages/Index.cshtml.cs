using Lab3.Data;
using Lab3.Models.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace Lab3.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public List<ApplicationUser> Users { get; set; } = new();
        public List<string> CarouselImages { get; set; } = new();

        public IndexModel(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        public async Task OnGetAsync()
        {
            Users = await _context.Users.ToListAsync();

            string carouselFolder = Path.Combine(_environment.WebRootPath, "carousel");

            if (Directory.Exists(carouselFolder))
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };

                CarouselImages = Directory.GetFiles(carouselFolder)
                    .Where(file => allowedExtensions.Contains(Path.GetExtension(file).ToLower()))
                    .Select(file => "/carousel/" + Path.GetFileName(file))
                    .ToList();
            }
        }
    }
}