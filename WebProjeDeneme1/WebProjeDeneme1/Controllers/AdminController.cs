using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProjeDeneme1.Data;
using WebProjeDeneme1.Models.Salonlar;
using WebProjeDeneme1.Models.Personeller;
using WebProjeDeneme1.Models.Uzmanlik;
using System.Linq;
using System.Threading.Tasks;

namespace WebProjeDeneme1.Controllers
{
    [Route("[controller]")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Index")]
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet("SalonTanimla")]
        public IActionResult SalonTanimla()
        {
            // Sadece salonu olmayan konumları listele
            var konumlar = _context.Konumlar
                .Where(k => !_context.Salonlar.Any(s => s.KonumId == k.KonumId))
                .ToList();
            ViewBag.Konumlar = konumlar;
            return View();
        }

        [HttpPost("SalonTanimla")]
        public async Task<IActionResult> SalonTanimla(Salon model)
        {
            if (ModelState.IsValid)
            {
                _context.Salonlar.Add(model);
                await _context.SaveChangesAsync();
                ViewBag.Message = "Salon başarıyla tanımlandı!";
            }
            // Sadece salonu olmayan konumları listele
            var konumlar = _context.Konumlar
                .Where(k => !_context.Salonlar.Any(s => s.KonumId == k.KonumId))
                .ToList();
            ViewBag.Konumlar = konumlar;
            return View();
        }

        [HttpGet("IslemTanimla")]
        public IActionResult IslemTanimla()
        {
            ViewBag.UzmanlikAlanlari = _context.UzmanlikAlanlari.ToList();
            return View();
        }

        [HttpPost("IslemTanimla")]
        public async Task<IActionResult> IslemTanimla(YapilabilenIslem model)
        {
            if (ModelState.IsValid)
            {
                _context.YapilabilenIslemler.Add(model);
                await _context.SaveChangesAsync();
                ViewBag.Message = "İşlem başarıyla tanımlandı!";
            }
            else
            {
                ViewBag.ErrorMessage = "İşlem tanımlama başarısız. Lütfen tüm alanları doğru doldurduğunuzdan emin olun.";
            }
            ViewBag.UzmanlikAlanlari = _context.UzmanlikAlanlari.ToList();
            return View();
        }

        [HttpGet("PersonelTanimla")]
        public IActionResult PersonelTanimla()
        {
            ViewBag.Salonlar = _context.Salonlar.Include(s => s.Konum).ToList();
            ViewBag.UzmanlikAlanlari = _context.UzmanlikAlanlari.ToList();
            return View();
        }

        [HttpPost("PersonelTanimla")]
        public async Task<IActionResult> PersonelTanimla(Personel model)
        {
            var salon = await _context.Salonlar.FindAsync(model.SalonId);
            if (salon == null)
            {
                ModelState.AddModelError("", "Geçersiz salon seçimi.");
            }
            else if (model.BaslangicSaat < salon.BaslangicSaat || model.BitisSaat > salon.BitisSaat)
            {
                ModelState.AddModelError("", "Personelin çalışma saatleri salonun çalışma saatleri dışında olamaz.");
            }
            else
            {
                _context.Personeller.Add(model);
                await _context.SaveChangesAsync();
                ViewBag.Message = "Personel başarıyla tanımlandı!";
            }
            ViewBag.Salonlar = _context.Salonlar.Include(s => s.Konum).ToList();
            ViewBag.UzmanlikAlanlari = _context.UzmanlikAlanlari.ToList();
            return View();
        }

        [HttpGet("SalonIslemleri")]
        public async Task<IActionResult> SalonIslemleri(int? SalonId)
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