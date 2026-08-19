using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DataModels;
using HotelSys.ViewModel;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HotelSys.Controllers
{
    public class authController : Controller
    {
        private readonly HotelAlkheerDB _db;
        private readonly PasswordHasher<DataModels.AspNetUser> _passwordHasher = new PasswordHasher<DataModels.AspNetUser>();

        public authController(HotelAlkheerDB db)
        {
            _db = db;
        }

        [AllowAnonymous]
        public IActionResult login()
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login([Bind("Username,Password")] LoginViewModel user)
        {
            if (!ModelState.IsValid)
                return View("login", user);

            string username = user.Username?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(user.Password))
            {
                ModelState.AddModelError(string.Empty, "يرجى إدخال اسم المستخدم وكلمة المرور.");
                return View("login", user);
            }

            var identityUser = _db.AspNetUsers.FirstOrDefault(x =>
                x.UserName == username ||
                x.NormalizedUserName == username.ToUpperInvariant());

            bool valid = false;
            if (identityUser != null && !string.IsNullOrWhiteSpace(identityUser.PasswordHash))
            {
                var result = _passwordHasher.VerifyHashedPassword(identityUser, identityUser.PasswordHash, user.Password);
                valid = result != PasswordVerificationResult.Failed;
            }

            // توافق مع الحسابات القديمة الموجودة في admin_table إذا كانت النسخة الأصلية تستخدمها.
            if (!valid)
            {
                var legacyUser = _db.AdminTables.FirstOrDefault(x =>
                    x.Username == username &&
                    x.Password == user.Password &&
                    (x.Status == null || x.Status == true));
                if (legacyUser != null)
                {
                    identityUser = null;
                    valid = true;
                }
            }

            if (!valid)
            {
                ModelState.AddModelError(string.Empty, "اسم المستخدم أو كلمة المرور غير صحيحة.");
                return View("login", user);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.NameIdentifier, identityUser?.Id ?? username),
                new Claim(ClaimTypes.Role, "admin")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity));

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(login));
        }
    }
}
