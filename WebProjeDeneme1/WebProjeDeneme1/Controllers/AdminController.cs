﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProjeDeneme1.Data;
using WebProjeDeneme1.Models.Salonlar;
using WebProjeDeneme1.Models.Personeller;
using WebProjeDeneme1.Models.Uzmanlik;
using WebProjeDeneme1.Models.Randevular;
using WebProjeDeneme1.Models.Kullanicilar;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WebProjeDeneme1.Controllers
{
    [Authorize(Roles = "Admin")]
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

        [HttpGet]
        public IActionResult IsAdmin(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest("Email cannot be null or empty.");
            }

            // Find the user by email
            var user = _context.Uyeler.FirstOrDefault(u => u.Email == email);

            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Return the AdminMi value
            return Ok(new { AdminMi = user.AdminMi });
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
            var randevu = await _context.Randevular
                .Include(r => r.Islem) // Islem nesnesini de yükle
                .FirstOrDefaultAsync(r => r.RandevuId == id);
            if (randevu != null)
            {
                // Randevu uygunluk kontrolleri
                var uygunlukSonucu = await KontrolRandevuUygunlugu(randevu);
                if (!uygunlukSonucu.Item1)
                {
                    ViewBag.ErrorMessage = uygunlukSonucu.Item2;
                    return RedirectToAction("RandevuOnayla");
                }

                randevu.Onaylandi = true;
                await _context.SaveChangesAsync();

                // Aynı salon, personel ve zaman dilimindeki diğer çakışan randevuları sil
                var cakisanRandevular = await _context.Randevular
                    .Where(r => r.SalonId == randevu.SalonId && r.PersonelId == randevu.PersonelId && r.Gun == randevu.Gun && r.RandevuId != randevu.RandevuId)
                    .Include(r => r.Islem) // Islem nesnesini de yükle
                    .ToListAsync();

                foreach (var cakisanRandevu in cakisanRandevular)
                {
                    var cakisanRandevuBitisSaati = cakisanRandevu.Saat + cakisanRandevu.Islem.IslemSuresi;
                    var randevuBitisSaati = randevu.Saat + randevu.Islem.IslemSuresi;

                    if ((randevu.Saat >= cakisanRandevu.Saat && randevu.Saat < cakisanRandevuBitisSaati) ||
                        (randevuBitisSaati > cakisanRandevu.Saat && randevuBitisSaati <= cakisanRandevuBitisSaati) ||
                        (randevu.Saat <= cakisanRandevu.Saat && randevuBitisSaati >= cakisanRandevuBitisSaati))
                    {
                        _context.Randevular.Remove(cakisanRandevu);
                    }
                }

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
                    ViewBag.SelectedPersonel = personel; // Çalışma saatlerini göstermek için
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
        public async Task<IActionResult> RandevuOlustur(Randevu model, int SalonId, int PersonelId)
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
                ViewBag.Salonlar = await _context.Salonlar.Include(s => s.Konum).ToListAsync();
                ViewBag.MevcutRandevular = await _context.Randevular
                    .Include(r => r.Salon)
                    .Include(r => r.Personel)
                    .Include(r => r.Islem)
                    .ToListAsync();
                return View("~/Views/Admin/RandevuOlustur.cshtml");
            }

            _context.Randevular.Add(model);
            await _context.SaveChangesAsync();
            ViewBag.Message = "Randevu başarıyla oluşturuldu!";

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
            // İşlemin süresi ve randevunun bitiş saati hesaplanır
            var islem = await _context.YapilabilenIslemler.FindAsync(model.IslemId);
            if (islem == null)
            {
                return (false, "Geçersiz işlem seçimi.");
            }
            var randevuBitisSaati = model.Saat + islem.IslemSuresi;

            // Tarih ve saat kontrolü: sadece bugünden ve şu anki saatten sonraki tarihler için randevu alınabilir
            var now = DateTime.UtcNow;
            if (model.Gun < now.Date || (model.Gun == now.Date && model.Saat <= now.TimeOfDay))
            {
                return (false, "Randevu tarihi ve saati bugünden ve şu anki saatten sonra olmalıdır.");
            }

            // Aynı salon, personel ve saat için başka onaylanmış bir randevu olmadığını kontrol et
            var mevcutRandevular = await _context.Randevular
                .Where(r => r.SalonId == model.SalonId && r.PersonelId == model.PersonelId && r.Gun == model.Gun && r.Onaylandi)
                .Include(r => r.Islem) // Islem nesnesini de yükle
                .ToListAsync();

            foreach (var randevu in mevcutRandevular)
            {
                var mevcutRandevuBitisSaati = randevu.Saat + randevu.Islem.IslemSuresi;
                if ((model.Saat >= randevu.Saat && model.Saat < mevcutRandevuBitisSaati) ||
                    (randevuBitisSaati > randevu.Saat && randevuBitisSaati <= mevcutRandevuBitisSaati) ||
                    (model.Saat <= randevu.Saat && randevuBitisSaati >= mevcutRandevuBitisSaati))
                {
                    return (false, "Aynı salon, personel ve saat için başka bir onaylanmış randevu bulunmaktadır.");
                }
            }

            // Salonun çalışma saatleriyle uyum kontrolü yapılır
            var salon = await _context.Salonlar.FindAsync(model.SalonId);
            if (salon == null)
            {
                return (false, "Geçersiz salon seçimi.");
            }
            if (randevuBitisSaati > salon.BitisSaat || model.Saat < salon.BaslangicSaat)
            {
                return (false, "Randevu saati salonun çalışma saatleri dışında.");
            }

            // Personelin çalışma saatleriyle uyum kontrolü yapılır
            var personel = await _context.Personeller.FindAsync(model.PersonelId);
            if (personel == null)
            {
                return (false, "Geçersiz personel seçimi.");
            }
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