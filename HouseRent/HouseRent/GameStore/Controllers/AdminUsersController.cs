using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using GameStore.Models;
using Microsoft.AspNetCore.Authorization;

namespace GameStore.Controllers
{
    [Authorize(Roles = "Адміністратор")]
    public class AdminUsersController : Controller
    {
        private readonly HouseRentContext _context;

        public AdminUsersController(HouseRentContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.VerificationStatus)
                .ToListAsync();

            // Перевірка та оновлення статусу верифікації для всіх користувачів
            foreach (var user in users)
            {
                var documentCount = await _context.UserDocuments
                    .CountAsync(ud => ud.UserID == user.ID);
                if (documentCount == 0)
                {
                    user.VerificationStatusID = null; // або ID статусу "Немає документів"
                    var noDocumentsStatus = await _context.DocumentStatuses
                        .FirstOrDefaultAsync(ds => ds.Name == "Немає документів");
                    if (noDocumentsStatus != null)
                    {
                        user.VerificationStatusID = noDocumentsStatus.ID;
                    }
                }
                else
                {
                    // Оновлюємо статус на основі останнього документа, якщо вони є
                    var latestDocument = await _context.UserDocuments
                        .Where(ud => ud.UserID == user.ID)
                        .OrderByDescending(d => d.UploadDate)
                        .FirstOrDefaultAsync();
                    if (latestDocument != null)
                    {
                        user.VerificationStatusID = latestDocument.DocumentStatusID;
                    }
                }
            }
            await _context.SaveChangesAsync();

            return View(users);
        }

        public async Task<IActionResult> VerifyDocuments(int id)
        {
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
            ViewBag.Statuses = await _context.DocumentStatuses.ToListAsync();
            ViewBag.UserId = id; // Передаємо UserId для коректної роботи посилань
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDocumentStatus(int documentId, int statusId, string comment)
        {
            var document = await _context.UserDocuments.FindAsync(documentId);
            if (document == null)
            {
                return NotFound();
            }
            document.DocumentStatusID = statusId;
            document.Comment = comment;
            await _context.SaveChangesAsync();

            // Оновлення загального статусу користувача на основі статусу останнього документа
            var user = await _context.Users
                .Include(u => u.UserDocuments)
                .FirstAsync(u => u.ID == document.UserID);

            if (user.UserDocuments.Any())
            {
                // Знаходимо останній документ за датою завантаження
                var latestDocument = user.UserDocuments
                    .OrderByDescending(d => d.UploadDate)
                    .First();

                // Встановлюємо статус користувача відповідно до статусу останнього документа
                user.VerificationStatusID = latestDocument.DocumentStatusID;
            }
            else
            {
                // Якщо документів немає, скидаємо статус верифікації
                user.VerificationStatusID = null;
                var noDocumentsStatus = await _context.DocumentStatuses
                    .FirstOrDefaultAsync(ds => ds.Name == "Немає документів");
                if (noDocumentsStatus != null)
                {
                    user.VerificationStatusID = noDocumentsStatus.ID;
                }
            }

            await _context.SaveChangesAsync();

            return RedirectToAction("VerifyDocuments", new { id = document.UserID });
        }

        [HttpPost]
        [Authorize(Roles = "Адміністратор")]
        public async Task<IActionResult> ClearUserDocuments(int[] userIds)
        {
            if (userIds == null || !userIds.Any())
            {
                return BadRequest("Не вказано UserID для видалення.");
            }

            var documentsToDelete = _context.UserDocuments.Where(ud => userIds.Contains(ud.UserID));
            _context.UserDocuments.RemoveRange(documentsToDelete);

            // Оновлення статусу верифікації для всіх видалених користувачів
            foreach (var userId in userIds)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user != null)
                {
                    user.VerificationStatusID = null;
                    var noDocumentsStatus = await _context.DocumentStatuses
                        .FirstOrDefaultAsync(ds => ds.Name == "Немає документів");
                    if (noDocumentsStatus != null)
                    {
                        user.VerificationStatusID = noDocumentsStatus.ID;
                    }
                }
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}