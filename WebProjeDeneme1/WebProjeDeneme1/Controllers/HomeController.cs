using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebProjeDeneme1.Models;

namespace WebProjeDeneme1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Kuafor()
        {
            return View();
        }

        public IActionResult Kuaforler()
        {
            return View();
        }
        public IActionResult LoginSignup()
        {
            return View();
        }   
        public IActionResult BSignup()
        {
            return View();
        }
        public IActionResult IsletmemDashboard()
        {
            return View();
        }
        public IActionResult IsletmemRezervasyon()
        {
            return View();
        }
        public IActionResult IsletmemCalisan()
        {
            return View();
        }
        public IActionResult IsletmemHizmet()
        {
            return View();
        }
        public IActionResult Profil()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
