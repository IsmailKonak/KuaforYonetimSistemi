using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProjeDeneme1.Data;
using WebProjeDeneme1.Models.Kullanicilar;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using WebProjeDeneme1.Models.Randevular;

namespace WebProjeDeneme1.Controllers
{
    [Route("[controller]")]
    public class UyeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public UyeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login(Uye model)
        {
            var user = await _context.Uyeler
                .SingleOrDefaultAsync(x => x.Email == model.Email && x.Password == model.Password);
            if (user == null)
            {
                ViewBag.Message = "Giriş başarısız!";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.AdminMi ? "Admin" : "User")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            if (user.AdminMi)
            {
                return RedirectToAction("Index", "Admin");
            }

            return RedirectToAction("Menu", "Uye");
        }

        [HttpGet("Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(Uye model)
        {
            var user = new Uye
            {
                Email = model.Email,
                Password = model.Password,
                AdminMi = false
            };

            _context.Uyeler.Add(user);
            await _context.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, "User")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            ViewBag.Message = "Kayıt başarılı!";
            return RedirectToAction("Menu", "Uye");
        }

        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Uye");
        }

        [HttpGet("Menu")]
        [Authorize]
        public IActionResult Menu()
        {
            return View();
        }

        [HttpGet("Randevularim")]
        [Authorize]
        public async Task<IActionResult> Randevularim()
        {
            var userEmail = User.Identity?.Name;
            if (userEmail == null)
            {
                return Unauthorized();
            }

            var user = await _context.Uyeler.SingleOrDefaultAsync(x => x.Email == userEmail);
            if (user == null)
            {
                return NotFound();
            }

            var randevular = await _context.Randevular
                .Where(r => r.UyeId == user.UyeId)
                .Include(r => r.Salon)
                .Include(r => r.Personel)
                .Include(r => r.Islem)
                .ToListAsync();

            return View(randevular);
        }

        [HttpGet("Profil")]
        [Authorize]
        public async Task<IActionResult> Profil()
        {
            var userEmail = User.Identity?.Name;
            if (userEmail == null)
            {
                return Unauthorized();
            }

            var user = await _context.Uyeler.SingleOrDefaultAsync(x => x.Email == userEmail);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        [HttpGet("RandevuOlustur")]
        [Authorize]
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
                    if (personel != null)
                    {
                        var islemler = await _context.YapilabilenIslemler
                            .Where(i => personel.UzmanlikAlanlari.Contains(i.UzmanlikAlaniId))
                            .ToListAsync();
                        ViewBag.Islemler = islemler;
                        ViewBag.SelectedPersonel = personel; // Çalışma saatlerini göstermek için
                    }
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
        [Authorize]
        public async Task<IActionResult> RandevuOlustur(Randevu model, int SalonId, int PersonelId)
        {
            // Gün ve Saat alanlarını UTC olarak ayarla
            model.Gun = DateTime.SpecifyKind(model.Gun, DateTimeKind.Utc);
            model.Saat = TimeSpan.FromTicks(model.Saat.Ticks);

            // UyeId'yi geçerli bir değerle ayarla (örneğin, oturum açmış kullanıcı)
            var userEmail = User.Identity?.Name;
            if (userEmail == null)
            {
                return Unauthorized();
            }

            var user = await _context.Uyeler.SingleOrDefaultAsync(x => x.Email == userEmail);
            if (user == null)
            {
                return NotFound();
            }

            model.UyeId = user.UyeId;
            model.SalonId = SalonId;
            model.PersonelId = PersonelId;

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
                return View("~/Views/Uye/RandevuOlustur.cshtml");
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

            return View("~/Views/Uye/RandevuOlustur.cshtml");
        }

        [HttpGet("AccessDenied")]
        public IActionResult AccessDenied()
        {
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