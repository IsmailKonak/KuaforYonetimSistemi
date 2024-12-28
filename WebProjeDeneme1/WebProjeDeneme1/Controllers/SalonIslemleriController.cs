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
using Microsoft.AspNetCore.Authorization;

namespace WebProjeDeneme1.Controllers
{
    [Route("[controller]")]
    public class SalonIslemleriController : Controller
    {
        private readonly ApplicationDbContext _context;

        public SalonIslemleriController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Index")]
        public async Task<IActionResult> Index(string konum, string baslangicSaat, string bitisSaat)
        {
            var salonlar = await _context.Salonlar
                .Include(s => s.Konum)
                .ToListAsync();

            if (!string.IsNullOrEmpty(konum))
            {
                salonlar = salonlar.Where(s => s.Konum.KonumAdi == konum).ToList();
            }

            if (!string.IsNullOrEmpty(baslangicSaat))
            {
                var baslangic = TimeSpan.Parse(baslangicSaat);
                salonlar = salonlar.Where(s => s.BaslangicSaat <= baslangic).ToList();
            }

            if (!string.IsNullOrEmpty(bitisSaat))
            {
                var bitis = TimeSpan.Parse(bitisSaat);
                salonlar = salonlar.Where(s => s.BitisSaat >= bitis).ToList();
            }

            ViewBag.Konumlar = await _context.Konumlar.Select(k => k.KonumAdi).ToListAsync();

            var salonIslemleri = new Dictionary<int, List<string>>();

            foreach (var salon in salonlar)
            {
                var personeller = await _context.Personeller
                    .Where(p => p.SalonId == salon.SalonId)
                    .ToListAsync();

                var islemler = new List<string>();

                foreach (var personel in personeller)
                {
                    var personelIslemleri = await _context.YapilabilenIslemler
                        .Where(i => personel.UzmanlikAlanlari.Contains(i.UzmanlikAlaniId))
                        .Select(i => i.IslemAdi)
                        .ToListAsync();

                    islemler.AddRange(personelIslemleri);
                }

                salonIslemleri[salon.SalonId] = islemler.Distinct().ToList();
            }

            ViewBag.SalonIslemleri = salonIslemleri;

            return View(salonlar);
        }

        [HttpGet("RandevuOlustur")]
        [Authorize]
        public IActionResult RandevuOlustur(int salonId)
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("RandevuOlustur", "Admin", new { SalonId = salonId });
            }
            else
            {
                return RedirectToAction("RandevuOlustur", "Uye", new { SalonId = salonId });
            }
        }
    }
}