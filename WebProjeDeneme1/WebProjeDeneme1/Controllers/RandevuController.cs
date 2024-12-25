using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProjeDeneme1.Data;
using WebProjeDeneme1.Models.Randevular;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebProjeDeneme1.Controllers
{
    [Route("[controller]")]
    public class RandevuController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RandevuController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("RandevuOlustur")]
        public async Task<IActionResult> RandevuOlustur(int? SalonId, int? PersonelId)
        {
            ViewBag.Salonlar = await _context.Salonlar.Include(s => s.Konum).ToListAsync();

            if (SalonId.HasValue)
            {
                ViewBag.SelectedSalonId = SalonId.Value;
                ViewBag.Personeller = await _context.Personeller
                    .Where(p => p.SalonId == SalonId.Value)
                    .ToListAsync();

                if (PersonelId.HasValue)
                {
                    ViewBag.SelectedPersonelId = PersonelId.Value;
                    var personel = await _context.Personeller.FindAsync(PersonelId.Value);
                    var islemler = await _context.YapilabilenIslemler
                        .Where(i => personel.UzmanlikAlanlari.Contains(i.UzmanlikAlaniId))
                        .ToListAsync();
                    ViewBag.Islemler = islemler;
                }
            }

            ViewBag.MevcutRandevular = await _context.Randevular
                .Include(r => r.Salon)
                .Include(r => r.Personel)
                .Include(r => r.Islem)
                .ToListAsync();

            return View("~/Views/Uye/RandevuOlustur.cshtml");
        }

        [HttpPost("RandevuOlustur")]
        public async Task<IActionResult> RandevuOlustur(Randevu model)
        {
            try
            {
                // Gün ve Saat alanlarını UTC olarak ayarla
                model.Gun = DateTime.SpecifyKind(model.Gun, DateTimeKind.Utc);
                model.Saat = TimeSpan.FromTicks(model.Saat.Ticks);

                // UyeId'yi geçerli bir değerle ayarla (örneğin, oturum açmış kullanıcı)
                model.UyeId = 1; // Bu değeri oturum açmış kullanıcıya göre ayarlayın

                _context.Randevular.Add(model);
                await _context.SaveChangesAsync();
                ViewBag.Message = "Randevu başarıyla oluşturuldu!";
            }
            catch (DbUpdateException ex)
            {
                ViewBag.ErrorMessage = $"Randevu oluşturulamadı: {ex.InnerException?.Message ?? ex.Message}";
            }

            ViewBag.Salonlar = await _context.Salonlar.Include(s => s.Konum).ToListAsync();
            ViewBag.MevcutRandevular = await _context.Randevular
                .Include(r => r.Salon)
                .Include(r => r.Personel)
                .Include(r => r.Islem)
                .ToListAsync();

            return View("~/Views/Uye/RandevuOlustur.cshtml");
        }
    }
}