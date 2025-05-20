using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using GameStore.Models;

namespace GameStore.Controllers
{
    public class CommentsController : Controller
    {
        private readonly HouseRentContext _context;

        public CommentsController(HouseRentContext context)
        {
            _context = context;
        }

        // GET: Comments
        public async Task<IActionResult> Index()
        {
            var gameStoreContext = _context.Comments.Include(c => c.Poster).Include(c => c.User);
            return View(await gameStoreContext.ToListAsync());
        }

        // GET: Comments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.Poster)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // GET: Comments/Create
        public IActionResult Create()
        {
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID");
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID");
            return View();
        }

        public IActionResult Add(int? PosterID, string? Description)
        {
            var _currentUser = _context.Users.First(x => x.Login.Equals(HttpContext.User.Identity.Name));
            Comment comment = new Comment { PosterID = (int)PosterID, UserID = _currentUser.ID, Description = (string)Description, PublicationDate = DateTime.Now};
            _context.Add(comment);
            _context.SaveChanges();

            return Redirect("~/Posters/Details/" + comment.PosterID);
        }

        // POST: Comments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Description,PosterID,UserID,PublicationDate")] Comment comment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(comment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", comment.PosterID);
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID", comment.UserID);
            return View(comment);
        }

        // GET: Comments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", comment.PosterID);
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID", comment.UserID);
            return View(comment);
        }

        // POST: Comments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Description,PosterID,UserID,PublicationDate")] Comment comment)
        {
            if (id != comment.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(comment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.ID))
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
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", comment.PosterID);
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID", comment.UserID);
            return View(comment);
        }

        // GET: Comments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.Poster)
                .Include(c => c.User)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // POST: Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var comment = await _context.Comments.FindAsync(id);
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool CommentExists(int id)
        {
            return _context.Comments.Any(e => e.ID == id);
        }
    }
}
