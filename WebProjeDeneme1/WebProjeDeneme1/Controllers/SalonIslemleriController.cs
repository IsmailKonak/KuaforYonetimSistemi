using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProjeDeneme1.Data;
using WebProjeDeneme1.Models.Salonlar;
using WebProjeDeneme1.Models.Personeller;
using System.Linq;
using System.Threading.Tasks;

namespace WebProjeDeneme1.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class SalonIslemleriController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalonIslemleriController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int? SalonId)
        {
            ViewBag.Salonlar = await _context.Salonlar.Include(s => s.Konum).ToListAsync();

            if (SalonId.HasValue)
            {
                var selectedSalon = await _context.Salonlar
                    .Include(s => s.Konum)
                    .FirstOrDefaultAsync(s => s.SalonId == SalonId.Value);

                if (selectedSalon != null)
                {
                    var personeller = await _context.Personeller
                        .Where(p => p.SalonId == SalonId.Value)
                        .ToListAsync();

                    var islemler = await _context.YapilabilenIslemler.ToListAsync();
                    var uzmanlikIslemleri = islemler.GroupBy(i => i.UzmanlikAlaniId)
                        .ToDictionary(g => g.Key, g => g.ToList());

                    ViewBag.Personeller = personeller;
                    ViewBag.Islemler = uzmanlikIslemleri;
                    return View(selectedSalon);
                }
            }

            return View();
        }
    }
}