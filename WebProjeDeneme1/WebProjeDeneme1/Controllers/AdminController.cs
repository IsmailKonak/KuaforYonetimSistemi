using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProjeDeneme1.Data;
using WebProjeDeneme1.Models.Salonlar;
using WebProjeDeneme1.Models.Personeller;
using WebProjeDeneme1.Models.Uzmanlik;
using WebProjeDeneme1.Models.Randevular;
using System;
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

        [HttpGet("RandevuOnayla")]
        public async Task<IActionResult> RandevuOnayla()
        {
            var randevular = await _context.Randevular
                .Include(r => r.Salon)
                .ThenInclude(s => s.Konum)
                .Include(r => r.Personel)
                .Include(r => r.Islem)
                .ToListAsync();

            return View(randevular);
        }

        [HttpPost("Onayla/{id}")]
        public async Task<IActionResult> Onayla(int id)
        {
            var randevu = await _context.Randevular.FindAsync(id);
            if (randevu != null)
            {
                randevu.Onaylandi = true;
                await _context.SaveChangesAsync();

                // Aynı salon, personel ve zaman dilimindeki diğer çakışan randevuları sil
                var cakisanRandevular = await _context.Randevular
                    .Where(r => r.SalonId == randevu.SalonId && r.PersonelId == randevu.PersonelId && r.Gun == randevu.Gun && r.Saat == randevu.Saat && r.RandevuId != randevu.RandevuId)
                    .ToListAsync();

                _context.Randevular.RemoveRange(cakisanRandevular);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("RandevuOnayla");
        }

        [HttpPost("IptalEt/{id}")]
        public async Task<IActionResult> IptalEt(int id)
        {
            var randevu = await _context.Randevular.FindAsync(id);
            if (randevu != null)
            {
                randevu.Onaylandi = false;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("RandevuOnayla");
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

            return View("~/Views/Admin/RandevuOlustur.cshtml");
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
                    return View("~/Views/Admin/RandevuOlustur.cshtml");
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

            return View("~/Views/Admin/RandevuOlustur.cshtml");
        }

        [HttpGet("GunlukCiro")]
        public IActionResult GunlukCiro()
        {
            return View();
        }

        [HttpPost("GunlukCiro")]
        public async Task<IActionResult> GunlukCiro(DateTime Tarih)
        {
            try
            {
                // Tarihi UTC olarak ayarla
                Tarih = DateTime.SpecifyKind(Tarih, DateTimeKind.Utc);

                var ciro = await _context.Randevular
                    .Where(r => r.Gun == Tarih && r.Onaylandi)
                    .SumAsync(r => r.Islem.IslemUcreti);

                ViewBag.Ciro = ciro;
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ciro hesaplanamadı: {ex.Message}";
            }

            return View();
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