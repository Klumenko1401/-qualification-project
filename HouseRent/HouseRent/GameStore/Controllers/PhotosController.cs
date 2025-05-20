using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using GameStore.Models;
using Microsoft.AspNetCore.Http;
using System.IO;
using Microsoft.AspNetCore.Hosting;

namespace HouseRent.Controllers
{
    public class PhotosController : Controller
    {
        private readonly HouseRentContext _context;
        private readonly IHostingEnvironment _environment;

        public PhotosController(HouseRentContext context, IHostingEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Photos
        public async Task<IActionResult> Index(int? id)
        {
            var houseRentContext = _context.Photos.Include(p => p.Poster).Where(x => x.PosterID == id);
            return View(await houseRentContext.ToListAsync());
        }

        // GET: Photos/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var photo = await _context.Photos
                .Include(p => p.Poster)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (photo == null)
            {
                return NotFound();
            }

            return View(photo);
        }

        // GET: Photos/Create
        public IActionResult Create()
        {
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID");
            return View();
        }

        // POST: Photos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int PosterID, IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("Image", "Будь ласка, виберіть файл для завантаження.");
                ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", PosterID);
                return View();
            }

            string path = Path.Combine(_environment.WebRootPath, "Images/Posters", PosterID.ToString());
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            string fullPath = Path.Combine(path, fileName);
            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var photo = new Photo { PosterID = PosterID, Image = $"/Images/Posters/{PosterID}/{fileName}" };
            _context.Add(photo);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Posters", new { id = PosterID });
        }

        // GET: Photos/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var photo = await _context.Photos.FindAsync(id);
            if (photo == null)
            {
                return NotFound();
            }
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", photo.PosterID);
            return View(photo);
        }

        // POST: Photos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, int PosterID, IFormFile file)
        {
            if (id == 0)
            {
                return NotFound();
            }

            var photo = await _context.Photos.FindAsync(id);
            if (photo == null)
            {
                return NotFound();
            }

            if (file != null && file.Length > 0)
            {
                // Видалимо старий файл, якщо він існує
                if (!string.IsNullOrEmpty(photo.Image))
                {
                    string oldFullPath = Path.Combine(_environment.WebRootPath, photo.Image.TrimStart('/'));
                    if (System.IO.File.Exists(oldFullPath))
                    {
                        System.IO.File.Delete(oldFullPath);
                    }
                }

                string path = Path.Combine(_environment.WebRootPath, "Images/Posters", PosterID.ToString());
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                }

                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                string fullPath = Path.Combine(path, fileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                photo.Image = $"/Images/Posters/{PosterID}/{fileName}";
                _context.Update(photo);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Posters", new { id = PosterID });
        }

        // GET: Photos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var photo = await _context.Photos
                .Include(p => p.Poster)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (photo == null)
            {
                return NotFound();
            }

            return View(photo);
        }

        // POST: Photos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var photo = await _context.Photos.FindAsync(id);
            if (photo == null)
            {
                return NotFound();
            }

            // Видалимо файл із диску
            if (!string.IsNullOrEmpty(photo.Image))
            {
                string fullPath = Path.Combine(_environment.WebRootPath, photo.Image.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }

            _context.Photos.Remove(photo);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Posters", new { id = photo.PosterID });
        }

        private bool PhotoExists(int id)
        {
            return _context.Photos.Any(e => e.ID == id);
        }
    }
}
