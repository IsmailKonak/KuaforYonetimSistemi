using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProjeDeneme1.Data;
using WebProjeDeneme1.Models.Salonlar;
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
        public async Task<IActionResult> Index([FromQuery] string konum, [FromQuery] string baslangicSaat, [FromQuery] string bitisSaat)
        {
            ViewBag.Konumlar = await _context.Konumlar.Select(k => k.KonumAdi).ToListAsync();

            var salonlar = _context.Salonlar.Include(s => s.Konum).AsQueryable();

            if (!string.IsNullOrEmpty(konum))
            {
                salonlar = salonlar.Where(s => s.Konum.KonumAdi == konum);
            }

            // TimeSpan? tipinde değişkenler tanımlıyoruz
            TimeSpan? parsedBaslangicSaat = null;
            TimeSpan? parsedBitisSaat = null;

            // String olarak gelen baslangicSaat'i TimeSpan'e çeviriyoruz
            if (!string.IsNullOrEmpty(baslangicSaat))
            {
                if (TimeSpan.TryParse(baslangicSaat, out TimeSpan tempBaslangicSaat))
                {
                    parsedBaslangicSaat = tempBaslangicSaat;
                }
            }

            // String olarak gelen bitisSaat'i TimeSpan'e çeviriyoruz
            if (!string.IsNullOrEmpty(bitisSaat))
            {
                if (TimeSpan.TryParse(bitisSaat, out TimeSpan tempBitisSaat))
                {
                    parsedBitisSaat = tempBitisSaat;
                }
            }

            // Filtreleme işlemlerinde çevrilmiş değerleri kullanıyoruz
            if (parsedBaslangicSaat.HasValue)
            {
                salonlar = salonlar.Where(s => s.BaslangicSaat >= parsedBaslangicSaat.Value);
            }

            if (parsedBitisSaat.HasValue)
            {
                salonlar = salonlar.Where(s => s.BitisSaat <= parsedBitisSaat.Value);
            }

            return View(await salonlar.ToListAsync());
        }
    }
}