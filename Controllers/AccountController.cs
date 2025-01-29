using Mailo.Data;
using Mailo.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Net;
using System.Security.Claims;
using Mailoo.Models;
using Mailoo.Data.Enums;
using Mailo.Data.Enums;

namespace Mailo.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View(_context.Users.ToList());
        }

        public IActionResult Registration()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Registration(RegistrationViewModel model)
        {
            if (ModelState.IsValid)
            {
                var account = new User
                {
                    Email = model.Email,
                    FName = model.FName,
                    LName = model.LName,
                    Password = model.Password,
                    Username = model.Username,
                    PhoneNumber = model.PhoneNumber,
                    Governorate = model.Governorate,
                    Address = model.Address,
                    Gender = model.Gender,
                    UserType = model.UserType,
                };

                try
                {
                    _context.Users.Add(account);
                    _context.SaveChanges();
                    ModelState.Clear();

                    ViewBag.Message = $"{account.FName} {account.LName} registered successfully. Please Login.";
                    return View("Login");
                }
                catch (DbUpdateException)
                {
                    ModelState.AddModelError("", "Please enter unique Email or Username.");
                }
            }
            return View(model);
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(x => (x.Username == model.UsernameOrEmail || x.Email == model.UsernameOrEmail));
                if (user != null && user.Password == model.Password)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Email),
                        new Claim("Name", user.FName),
                        new Claim(ClaimTypes.Role, user.UserType.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Username/Email or Password is not correct.");
                }
            }
            return View(model);
        }

        public IActionResult LogOut()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        public IActionResult Edit()
        {
            var email = User.Identity.Name;
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            if (user != null)
            {
                var model = new EditUserViewModel
                {
                    FName = user.FName,
                    LName = user.LName,
                    PhoneNumber = user.PhoneNumber,
                    Address = user.Address,
                    Governorate = user.Governorate,
                    Email = user.Email, // Displayed as read-only
                    Username = user.Username, // Displayed as read-only
                    Gender = user.Gender,
                    UserType = user.UserType
                };
                return View(model);
            }
            return NotFound();
        }


        [HttpPost]
        [Authorize]
        public IActionResult Edit(EditUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var email = User.Identity.Name;
                var user = _context.Users.FirstOrDefault(u => u.Email == email);

                if (user != null)
                {
                    user.FName = model.FName;
                    user.LName = model.LName;
                    user.PhoneNumber = model.PhoneNumber;
                    user.Address = model.Address;
                    user.Governorate = model.Governorate;
                    user.Gender = model.Gender;

                    _context.SaveChanges();

                    TempData["SuccessMessage"] = "Your data has been successfully modified!";

                    return RedirectToAction("Edit");
                }
            }

            return View(model);
        }







        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email);
                if (user != null)
                {
                    user.PasswordResetToken = Guid.NewGuid().ToString();
                    user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
                    _context.SaveChanges();

                    await SendResetPasswordEmail(user.Email, user.PasswordResetToken);

                    ViewBag.Message = "A password reset link has been sent to your email.";
                    return View();
                }
                ModelState.AddModelError("", "Email not found.");
            }
            return View(model);
        }

        public IActionResult ResetPassword(string token)
        {
            var user = _context.Users.FirstOrDefault(u => u.PasswordResetToken == token && u.PasswordResetTokenExpiry > DateTime.UtcNow);

            if (user == null)
            {
                ViewBag.Message = "Invalid or expired token.";
                return View("Error");
            }

            return View(new ResetPasswordViewModel { Token = token });
        }

        [HttpPost]
        [HttpPost]
        public IActionResult ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = _context.Users.FirstOrDefault(u => u.PasswordResetToken == model.Token && u.PasswordResetTokenExpiry > DateTime.UtcNow);

                if (user != null)
                {
                    user.Password = model.NewPassword; 
                    user.PasswordResetToken = null;
                    user.PasswordResetTokenExpiry = null;

                    _context.SaveChanges(); 

                    ViewBag.Message = "Your password has been reset successfully.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", "Invalid or expired token.");
            }
            return View(model);
        }


        private async Task SendResetPasswordEmail(string email, string token)
        {
            var resetLink = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);

            var message = new MailMessage();
            message.From = new MailAddress("mailostoreee@gmail.com"); 
            message.To.Add(email);
            message.Subject = "Reset Password";
            message.Body = $"Please reset your password by clicking here: <a href=\"{resetLink}\">link</a>";
            message.IsBodyHtml = true;

            using (var smtp = new SmtpClient())
            {
                smtp.Host = "smtp.gmail.com"; 
                smtp.Port = 587;
                smtp.Credentials = new NetworkCredential("mailostoreee@gmail.com", "zrck gmqn cwzh bveq");
                smtp.EnableSsl = true;
                await smtp.SendMailAsync(message);
            }
        }
    }
}
