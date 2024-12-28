using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Include metodunu kullanmak için
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using WebProjeDeneme1.Data;
using WebProjeDeneme1.Models;
using WebProjeDeneme1.Models.Salonlar;

namespace WebProjeDeneme1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [HttpGet("api/Salonlar")]
        public async Task<ActionResult<IEnumerable<Salon>>> GetSalonlar()
        {
            // Include metodu ile Konum bilgisini de yükle
            return await _context.Salonlar.Include(s => s.Konum).ToListAsync();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}