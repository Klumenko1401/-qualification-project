using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using GameStore.Models;
using HouseRent.Models;

namespace GameStore.Controllers
{
    public class RaitingsController : Controller
    {
        private readonly HouseRentContext _context;

        public RaitingsController(HouseRentContext context)
        {
            _context = context;
        }

        // GET: Raitings
        public async Task<IActionResult> Index()
        {
            var gameStoreContext = _context.Raitings.Include(r => r.Poster).Include(r => r.User);
            return View(await gameStoreContext.ToListAsync());
        }

        // GET: Raitings/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var raiting = await _context.Raitings
                .Include(r => r.Poster)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (raiting == null)
            {
                return NotFound();
            }

            return View(raiting);
        }

        // GET: Raitings/Create
        public IActionResult Create()
        {
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID");
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID");
            return View();
        }

        // GET: Raitings/Rate
        public IActionResult Rate(int? PosterID, int? Value)
        {
            // Логування вхідних параметрів
            Console.WriteLine($"Rate called with PosterID: {PosterID}, Value: {Value}");

            // Перевірка, чи авторизований користувач
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            // Перевірка, чи передані параметри
            if (!PosterID.HasValue || !Value.HasValue)
            {
                return BadRequest("PosterID і Value мають бути вказані.");
            }

            // Перевірка, чи існує користувач
            var currentUser = _context.Users.FirstOrDefault(x => x.Login.Equals(HttpContext.User.Identity.Name));
            if (currentUser == null)
            {
                return Unauthorized("Користувача не знайдено.");
            }

            // Перевірка, чи існує оголошення
            var poster = _context.Posters.FirstOrDefault(x => x.ID == PosterID);
            if (poster == null)
            {
                return NotFound("Оголошення з таким ID не знайдено.");
            }

            // Перевірка, чи оцінка в межах допустимого діапазону (наприклад, 1-5)
            if (Value < 1 || Value > 5)
            {
                return BadRequest("Оцінка має бути в межах від 1 до 5.");
            }

            // Перевірка, чи користувач вже оцінював це оголошення
            var existingRating = _context.Raitings.FirstOrDefault(x => x.PosterID == PosterID && x.UserID == currentUser.ID);
            if (existingRating != null)
            {
                return BadRequest("Ви вже оцінили це оголошення.");
            }

            try
            {
                // Створюємо нову оцінку
                Raiting raiting = new Raiting
                {
                    PosterID = PosterID.Value,
                    UserID = currentUser.ID,
                    Value = Value.Value
                };
                _context.Add(raiting);
                _context.SaveChanges();

                // Оновлюємо середній рейтинг оголошення
                var ratings = _context.Raitings.Where(x => x.PosterID == PosterID).ToList();
                if (ratings.Any())
                {
                    poster.Raiting = ratings.Average(x => x.Value);
                }
                else
                {
                    poster.Raiting = 0;
                }

                _context.Update(poster);
                _context.SaveChanges();

                return RedirectToAction("Details", "Posters", new { id = PosterID });
            }
            catch (Exception ex)
            {
                // Логування помилки
                return StatusCode(500, $"Помилка при збереженні оцінки: {ex.Message}");
            }
        }

        // POST: Raitings/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,PosterID,UserID,Value")] Raiting raiting)
        {
            if (ModelState.IsValid)
            {
                _context.Add(raiting);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", raiting.PosterID);
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID", raiting.UserID);
            return View(raiting);
        }

        // GET: Raitings/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var raiting = await _context.Raitings.FindAsync(id);
            if (raiting == null)
            {
                return NotFound();
            }
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", raiting.PosterID);
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID", raiting.UserID);
            return View(raiting);
        }

        // POST: Raitings/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,PosterID,UserID,Value")] Raiting raiting)
        {
            if (id != raiting.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(raiting);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!RaitingExists(raiting.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", raiting.PosterID);
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID", raiting.UserID);
            return View(raiting);
        }

        // GET: Raitings/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var raiting = await _context.Raitings
                .Include(r => r.Poster)
                .Include(r => r.User)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (raiting == null)
            {
                return NotFound();
            }

            return View(raiting);
        }

        // POST: Raitings/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var raiting = await _context.Raitings.FindAsync(id);
            _context.Raitings.Remove(raiting);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool RaitingExists(int id)
        {
            return _context.Raitings.Any(e => e.ID == id);
        }
    }
}