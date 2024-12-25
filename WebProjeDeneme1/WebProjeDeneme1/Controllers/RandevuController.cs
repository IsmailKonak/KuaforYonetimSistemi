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

                // Randevu uygunluk kontrolleri
                var uygunlukSonucu = await KontrolRandevuUygunlugu(model);
                if (!uygunlukSonucu.Item1)
                {
                    ViewBag.ErrorMessage = uygunlukSonucu.Item2;
                    return View("~/Views/Uye/RandevuOlustur.cshtml");
                }

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

        private async Task<(bool, string)> KontrolRandevuUygunlugu(Randevu model)
        {
            // Aynı salon, personel ve saat için başka onaylanmış bir randevu olmadığını kontrol et
            var mevcutRandevu = await _context.Randevular
                .Where(r => r.SalonId == model.SalonId && r.PersonelId == model.PersonelId && r.Gun == model.Gun && r.Saat == model.Saat && r.Onaylandi)
                .FirstOrDefaultAsync();

            if (mevcutRandevu != null)
            {
                return (false, "Aynı salon, personel ve saat için başka bir onaylanmış randevu bulunmaktadır.");
            }

            // İşlemin süresi ve randevunun bitiş saati hesaplanır
            var islem = await _context.YapilabilenIslemler.FindAsync(model.IslemId);
            var randevuBitisSaati = model.Saat + islem.IslemSuresi;

            // Salonun çalışma saatleriyle uyum kontrolü yapılır
            var salon = await _context.Salonlar.FindAsync(model.SalonId);
            if (randevuBitisSaati > salon.BitisSaat || model.Saat < salon.BaslangicSaat)
            {
                return (false, "Randevu saati salonun çalışma saatleri dışında.");
            }

            // Personelin çalışma saatleriyle uyum kontrolü yapılır
            var personel = await _context.Personeller.FindAsync(model.PersonelId);
            if (randevuBitisSaati > personel.BitisSaat || model.Saat < personel.BaslangicSaat)
            {
                return (false, "Randevu saati personelin çalışma saatleri dışında.");
            }

            // Personelin ilgili işlem için yeterliliği kontrol edilir
            if (!personel.UzmanlikAlanlari.Contains(islem.UzmanlikAlaniId))
            {
                return (false, "Personel bu işlem için yeterli değil.");
            }

            return (true, "Randevu uygun.");
        }
    }
}