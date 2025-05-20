using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using GameStore.Models;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using HouseRent.Models;
using System.Transactions;
using Microsoft.AspNetCore.Authorization;

namespace GameStore.Controllers
{
    public class UsersController : Controller
    {
        private readonly HouseRentContext _context;
        private IHostingEnvironment Environment;

        public UsersController(HouseRentContext context, IHostingEnvironment _environment)
        {
            _context = context;
            Environment = _environment;
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            var users = _context.Users
                .Include(u => u.Role)
                .Include(u => u.VerificationStatus)
                .Include(u => u.UserDocuments);

            var noDocumentsStatusId = GetOrCreateDefaultStatusId("Немає документів", 4);
            foreach (var user in users)
            {
                if (!user.UserDocuments.Any())
                {
                    user.VerificationStatusID = noDocumentsStatusId;
                    user.VerificationStatus = _context.DocumentStatuses.FirstOrDefault(s => s.ID == noDocumentsStatusId);
                }
                else
                {
                    var latestDocument = user.UserDocuments
                        .OrderByDescending(d => d.UploadDate)
                        .First();
                    user.VerificationStatusID = latestDocument.DocumentStatusID;
                    user.VerificationStatus = _context.DocumentStatuses.FirstOrDefault(s => s.ID == latestDocument.DocumentStatusID);
                }
            }
            await _context.SaveChangesAsync();

            return View(await users.ToListAsync());
        }

        public async Task<IActionResult> Owners()
        {
            var users = _context.Users
                .Include(u => u.Role)
                .Include(u => u.VerificationStatus)
                .Include(u => u.UserDocuments)
                .Where(x => x.Role.Name.Equals("Продавець"));

            var noDocumentsStatusId = GetOrCreateDefaultStatusId("Немає документів", 4);
            foreach (var user in users)
            {
                if (!user.UserDocuments.Any())
                {
                    user.VerificationStatusID = noDocumentsStatusId;
                    user.VerificationStatus = _context.DocumentStatuses.FirstOrDefault(s => s.ID == noDocumentsStatusId);
                }
                else
                {
                    var latestDocument = user.UserDocuments
                        .OrderByDescending(d => d.UploadDate)
                        .First();
                    user.VerificationStatusID = latestDocument.DocumentStatusID;
                    user.VerificationStatus = _context.DocumentStatuses.FirstOrDefault(s => s.ID == latestDocument.DocumentStatusID);
                }
            }
            await _context.SaveChangesAsync();

            return View("Index", await users.ToListAsync());
        }

        public async Task<IActionResult> Clients()
        {
            var users = _context.Users
                .Include(u => u.Role)
                .Include(u => u.VerificationStatus)
                .Include(u => u.UserDocuments)
                .Where(x => x.Role.Name.Equals("Покупець"));

            var noDocumentsStatusId = GetOrCreateDefaultStatusId("Немає документів", 4);
            foreach (var user in users)
            {
                if (!user.UserDocuments.Any())
                {
                    user.VerificationStatusID = noDocumentsStatusId;
                    user.VerificationStatus = _context.DocumentStatuses.FirstOrDefault(s => s.ID == noDocumentsStatusId);
                }
                else
                {
                    var latestDocument = user.UserDocuments
                        .OrderByDescending(d => d.UploadDate)
                        .First();
                    user.VerificationStatusID = latestDocument.DocumentStatusID;
                    user.VerificationStatus = _context.DocumentStatuses.FirstOrDefault(s => s.ID == latestDocument.DocumentStatusID);
                }
            }
            await _context.SaveChangesAsync();

            return View("Index", await users.ToListAsync());
        }

        // GET: Users/Details/5
        [Authorize]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                ViewBag.My = true;
                var currentUser = _context.Users
                    .Include(x => x.Role)
                    .Include(x => x.Orders)
                    .Include(u => u.Posters)
                    .Include(u => u.VerificationStatus)
                    .Include(u => u.UserDocuments)
                    .ThenInclude(d => d.DocumentType)
                    .Include(u => u.UserDocuments)
                    .ThenInclude(d => d.DocumentStatus)
                    .First(x => x.Login.Equals(HttpContext.User.Identity.Name));
                foreach (Order o in currentUser.Orders)
                {
                    o.Poster = _context.Posters.First(x => x.ID == o.PosterID);
                }

                ViewBag.Raiting = GetRaiting(currentUser);
                ViewBag.DocumentTypes = await _context.DocumentTypes.ToListAsync();
                return View(currentUser);
            }

            ViewBag.My = false;
            var user = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Orders)
                .Include(u => u.Posters)
                .Include(u => u.VerificationStatus)
                .Include(u => u.UserDocuments)
                .ThenInclude(d => d.DocumentType)
                .Include(u => u.UserDocuments)
                .ThenInclude(d => d.DocumentStatus)
                .FirstOrDefaultAsync(m => m.ID == id);

            foreach (Order o in user.Orders)
            {
                o.Poster = _context.Posters.First(x => x.ID == o.PosterID);
            }

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.Raiting = GetRaiting(user);
            ViewBag.DocumentTypes = await _context.DocumentTypes.ToListAsync();
            return View(user);
        }

        private double GetRaiting(User user)
        {
            double raiting = 0;
            int count = 0;

            foreach (Poster o in user.Posters)
            {
                raiting += o.Raiting;
                if (o.Raiting != 0)
                    count++;
            }

            if (count == 0)
            {
                raiting = 0;
            }
            else
                raiting = raiting / count;

            return raiting;
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            var noDocumentsStatusId = GetOrCreateDefaultStatusId("Немає документів", 4);
            ViewBag.DefaultStatusId = noDocumentsStatusId;

            ViewData["RoleID"] = new SelectList(_context.Roles, "ID", "Name");
            return View();
        }

        // POST: Users/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Login,Password,FirstName,LastName,Email,Card,RoleID,VerificationStatusID")] User user, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                user.VerificationStatusID = GetOrCreateDefaultStatusId("Немає документів", 4);

                _context.Add(user);
                await _context.SaveChangesAsync();

                string wwwPath = this.Environment.WebRootPath;
                string contentPath = this.Environment.ContentRootPath;

                User user2 = _context.Users.First(x => x.Login.Equals(user.Login));

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

                _context.Users.Update(user2);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            ViewData["RoleID"] = new SelectList(_context.Roles, "ID", "Name", user.RoleID);
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.UserDocuments)
                .ThenInclude(d => d.DocumentType)
                .Include(u => u.UserDocuments)
                .ThenInclude(d => d.DocumentStatus)
                .FirstOrDefaultAsync(u => u.ID == id);
            if (user == null)
            {
                return NotFound();
            }
            ViewData["RoleID"] = new SelectList(_context.Roles, "ID", "Name", user.RoleID);
            ViewData["DocumentTypes"] = new SelectList(_context.DocumentTypes, "ID", "Name");
            return View(user);
        }

        // POST: Users/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Login,Password,FirstName,LastName,Email,Card,RoleID,VerificationStatusID")] User user, IFormFile avatarFile, IFormFile documentFile, int? documentTypeId)
        {
            if (id != user.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (avatarFile != null)
                    {
                        string path = Path.Combine(this.Environment.WebRootPath, "Images/Users/" + user.ID);
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        string fileName = Path.GetFileName(avatarFile.FileName);
                        using (FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                        {
                            avatarFile.CopyTo(stream);
                        }

                        user.Avatar = "/Images/Users/" + user.ID + "/" + fileName;
                    }

                    if (documentFile != null)
                    {
                        if (documentFile.Length > 5 * 1024 * 1024)
                        {
                            ModelState.AddModelError("documentFile", "Файл занадто великий. Максимальний розмір — 5 МБ.");
                            ViewData["RoleID"] = new SelectList(_context.Roles, "ID", "Name", user.RoleID);
                            ViewData["DocumentTypes"] = new SelectList(_context.DocumentTypes, "ID", "Name");
                            return View(user);
                        }

                        var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                        var extension = Path.GetExtension(documentFile.FileName).ToLower();
                        if (!allowedExtensions.Contains(extension))
                        {
                            ModelState.AddModelError("documentFile", "Дозволені формати: PDF, JPG, JPEG, PNG.");
                            ViewData["RoleID"] = new SelectList(_context.Roles, "ID", "Name", user.RoleID);
                            ViewData["DocumentTypes"] = new SelectList(_context.DocumentTypes, "ID", "Name");
                            return View(user);
                        }

                        string path = Path.Combine(this.Environment.WebRootPath, "Documents", user.ID.ToString());
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        string fileName = Guid.NewGuid().ToString() + extension;
                        using (FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                        {
                            documentFile.CopyTo(stream);
                        }

                        var document = new UserDocument
                        {
                            UserID = user.ID,
                            DocumentTypeID = documentTypeId ?? 1,
                            FilePath = $"/Documents/{user.ID}/{fileName}",
                            DocumentStatusID = GetOrCreateDefaultStatusId("На перевірці", 1), // Гарантуємо ID 1
                            UploadDate = DateTime.Now
                        };
                        _context.UserDocuments.Add(document);

                        // Оновлюємо статус користувача на "На перевірці" (ID 1)
                        user.VerificationStatusID = GetOrCreateDefaultStatusId("На перевірці", 1);
                    }

                    var documents = await _context.UserDocuments
                        .Where(d => d.UserID == user.ID)
                        .ToListAsync();
                    if (documents.Any())
                    {
                        var latestDocument = documents
                            .OrderByDescending(d => d.UploadDate)
                            .First();
                        user.VerificationStatusID = latestDocument.DocumentStatusID;
                    }
                    else
                    {
                        user.VerificationStatusID = GetOrCreateDefaultStatusId("Немає документів", 4);
                    }

                    _context.Update(user);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return Redirect("~/Users/Details/" + user.ID);
            }
            ViewData["RoleID"] = new SelectList(_context.Roles, "ID", "Name", user.RoleID);
            ViewData["DocumentTypes"] = new SelectList(_context.DocumentTypes, "ID", "Name");
            return View(user);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(IFormFile documentFile, int? documentTypeId)
        {
            var user = await _context.Users
                .Include(u => u.UserDocuments)
                .FirstOrDefaultAsync(u => u.Login == HttpContext.User.Identity.Name);
            if (user == null)
            {
                return NotFound();
            }

            if (documentFile != null)
            {
                if (documentFile.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Файл занадто великий. Максимальний розмір — 5 МБ.";
                    return RedirectToAction("Details");
                }

                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                var extension = Path.GetExtension(documentFile.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                {
                    TempData["Error"] = "Дозволені формати: PDF, JPG, JPEG, PNG.";
                    return RedirectToAction("Details");
                }

                string path = Path.Combine(this.Environment.WebRootPath, "Documents", user.ID.ToString());
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = Guid.NewGuid().ToString() + extension;
                using (FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                {
                    documentFile.CopyTo(stream);
                }

                var document = new UserDocument
                {
                    UserID = user.ID,
                    DocumentTypeID = documentTypeId ?? 1,
                    FilePath = $"/Documents/{user.ID}/{fileName}",
                    UploadDate = DateTime.Now,
                    DocumentStatusID = GetOrCreateDefaultStatusId("На перевірці", 1) // Гарантуємо ID 1
                };
                _context.UserDocuments.Add(document);

                // Оновлюємо статус користувача на "На перевірці" (ID 1)
                user.VerificationStatusID = GetOrCreateDefaultStatusId("На перевірці", 1);

                await _context.SaveChangesAsync();
            }
            else
            {
                TempData["Error"] = "Будь ласка, виберіть файл для завантаження.";
            }

            return RedirectToAction("Details");
        }

        [Authorize(Roles = "Адміністратор")]
        public async Task<IActionResult> ResetVerificationStatuses()
        {
            var users = await _context.Users
                .Include(u => u.UserDocuments)
                .ToListAsync();
            var noDocumentsStatusId = GetOrCreateDefaultStatusId("Немає документів", 4);

            foreach (var user in users)
            {
                if (!user.UserDocuments.Any())
                {
                    user.VerificationStatusID = noDocumentsStatusId;
                }
                else
                {
                    var latestDocument = user.UserDocuments
                        .OrderByDescending(d => d.UploadDate)
                        .First();
                    user.VerificationStatusID = latestDocument.DocumentStatusID;
                }
            }
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private int GetOrCreateDefaultStatusId(string statusName, int expectedId)
        {
            var status = _context.DocumentStatuses.FirstOrDefault(x => x.Name == statusName);
            if (status == null)
            {
                status = new DocumentStatus { Name = statusName, ID = expectedId }; // Встановлюємо бажаний ID
                _context.DocumentStatuses.Add(status);
                _context.SaveChanges();
            }
            else if (status.ID != expectedId)
            {
                // Якщо ID не збігається, оновлюємо його (потрібно вручну перевірити базу)
                status.ID = expectedId;
                _context.SaveChanges();
            }
            return status.ID;
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.FindAsync(id);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.ID == id);
        }
    }
}