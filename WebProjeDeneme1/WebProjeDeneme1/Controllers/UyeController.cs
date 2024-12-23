﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebProjeDeneme1.Data;
using WebProjeDeneme1.Models.Kullanicilar;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

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

            return RedirectToAction("Index", "Home");
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
            return RedirectToAction("Index", "Home");
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}