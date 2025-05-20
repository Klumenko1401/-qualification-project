using GameStore.Data;
using GameStore.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace GameStore.Controllers
{
    public class AccountController : Controller
    {
        private HouseRentContext db;
        IHostingEnvironment Environment;
        public AccountController(HouseRentContext context, IHostingEnvironment _environment)
        {
           db = context;
           Environment = _environment;
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Login == model.Login && u.Password == model.Password);
                if (user != null)
                {
                    await Authenticate(user); // аутентификация

                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Некорректные логин и(или) пароль");
            }
            return View(model);
        }

        public IActionResult Recover(string? Email)
        {
            User user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Email.ToLower().Equals(Email.ToLower()));
                
            if (user != null)
            {

                /*MailMessage msg = new MailMessage();
                System.Net.Mail.SmtpClient client = new System.Net.Mail.SmtpClient();
                try
                {
                    var fromAddress = new MailAddress("student2025111@gmail.com", "House Rent");
                    var toAddress = new MailAddress(Email, user.FullName);
                    const string fromPassword = "1Qazxsw23456";
                    const string subject = "Password Recover";
                    string body = "Your password is : " + user.Password;

                    var smtp = new SmtpClient
                    {
                        Host = "smtp.gmail.com",
                        Port = 587,
                        EnableSsl = true,
                        DeliveryMethod = SmtpDeliveryMethod.Network,
                        //UseDefaultCredentials = false,
                        Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                    };
                    using (var message = new MailMessage(fromAddress, toAddress)
                    {
                        Subject = subject,
                        Body = body
                    })
                    {
                        smtp.Send(message);
                    }
                }
                catch (Exception ex)
                {
                    return Redirect("~/Account/TestBad");
                }*/
                try
                {
                    /*var to = new { email = Email };
                    var from = new { email = Email, name = "House Rent" };
                    var args = new
                    {
                        from = from,
                        to = new[] { to },
                        subject = "Відновлення паролю",
                        text = "Ваш пароль : " + user.Password,
                        category = "Паролі"
                    };

                    var client = new RestClient("https://send.api.mailtrap.io/api/send");
                    var request = new RestRequest("/send", RestSharp.Method.Post);

                    request.AddHeader("Authorization", "Bearer 0df8c0dc62fc6cfc15725bb6d42c2181");
                    request.AddHeader("Content-Type", "application/json");
                    request.AddParameter("application/json",
                    JsonSerializer.Serialize(args), ParameterType.RequestBody);

                    RestResponse response = client.Execute(request);*/

                    var client = new SmtpClient("live.smtp.mailtrap.io", 587)

                    {

                        Credentials = new NetworkCredential("api", "3b23326db2cc1dbcc781fc927373bc8c"),

                        EnableSsl = true

                    };

                    client.Send("hello@demomailtrap.co", user.Email, "Hello world", "testbody");

                    return Redirect("~/Account/Login");

                }
                catch (Exception ex)
                {
                    return Redirect("~/Account/Login");
                }

                
            }
            return Redirect("~/Account/Login");
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Roles = new SelectList(db.Roles, "ID", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model, IFormFile file)
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Login == model.Login);
            if (user == null)
            {
                string avatar = "";
                //TODO ADD DEFAULT VALUE FOR ROLE ID CLIENT --- NOTE REGISTER ONLY FOR CLIENTS...
                if (file == null)
                    avatar = "/img/men_avatar.png";

                User registerUser = new User {
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Avatar = avatar,
                    Login = model.Login,
                    Password = model.Password,
                    RoleID = model.RoleID,
                    Email = model.Email,
                    Card = model.Card
                    
                };

                // добавляем пользователя в бд
                db.Users.Add(registerUser);
                await db.SaveChangesAsync();

                if(file != null)
                {
                    string wwwPath = this.Environment.WebRootPath;
                    string contentPath = this.Environment.ContentRootPath;

                    User user2 = db.Users.First(x => x.Login.Equals(registerUser.Login));

                    string path = Path.Combine(this.Environment.WebRootPath, "Images/Users/" + user2.ID);
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    string fileName = Path.GetFileName(file.FileName);
                    using (FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    user2.Avatar = "/Images/Users/" + user2.ID + "/" + fileName;

                    db.Users.Update(user2);
                    db.SaveChanges();
                }

                return RedirectToAction("Login", "Account");
            }
            else
            {
                ModelState.AddModelError("", "Некорректные логин и(или) пароль");
                return View(model);
            }
        }

        private async Task Authenticate(User user)
        {
            // создаем один claim
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, user.Login),
                new Claim(ClaimsIdentity.DefaultRoleClaimType, user.Role?.Name)
            };
            // создаем объект ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}
