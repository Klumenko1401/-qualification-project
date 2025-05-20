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
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace GameStore.Controllers
{
    public class GamesController : Controller
    {
        private readonly HouseRentContext _context;
        IHostingEnvironment Environment;
        public GamesController(HouseRentContext context, IHostingEnvironment _environment)
        {
            _context = context;
            Environment = _environment;
        }

        /*// GET: Games
        public async Task<IActionResult> Index(string? GameName, int? GenreID, DateTime? PublicationDate)
        {
            var gameStoreContext = _context.Posters.Include(g => g.Genre).Include(g => g.Modereator).ToList();

            if (GameName != null)
            {
                gameStoreContext = gameStoreContext.Where(x => x.Name.ToLower().Contains(GameName.ToLower())).ToList();
            }

            if (GenreID!= null)
            {
                gameStoreContext = gameStoreContext.Where(x => x.GenreID == GenreID).ToList();
            }
            if(PublicationDate!= null)
            {
                gameStoreContext = gameStoreContext.Where(x => x.ProductionDate.CompareTo(PublicationDate)>0).ToList();
            }

            ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "Name");
            return View(gameStoreContext.ToList());
        }

        // GET: Games/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Modereator)
                .Include(g => g.Screenshots)
                .Include(g => g.Requirements)
                .Include(g =>g.Comments)
                .FirstOrDefaultAsync(m => m.ID == id);

            game.Comments = game.Comments.OrderByDescending(x=>x.PublicationDate).ToList();

            foreach(Comment comment in game.Comments)
            {
                comment.User = _context.Users.First(x => x.ID == comment.UserID);
            }

            if (User.Identity.IsAuthenticated)
            {
                var currentUser = _context.Users.Include(x => x.Role).First(x => x.Login.Equals(HttpContext.User.Identity.Name));

                if (currentUser.Role.Name.Equals("Модератор") && currentUser.ID == game.ModereatorID)
                {
                    ViewBag.Editable = true;
                }
                else
                    ViewBag.Editable = false;

                ViewBag.Return = _context.Orders.Where(x => x.PosterID == game.ID && x.UserID == currentUser.ID).Count() > 0 ? true : false;
                ViewBag.Rate = _context.Raitings.Where(x => x.PosterID == game.ID && x.UserID == currentUser.ID).Count() == 0 ? true : false;
            }

            else
            {
                ViewBag.Editable = false;
                ViewBag.Return = false;
                ViewBag.Rate = false;
            }

            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        // GET: Games/Create
        public IActionResult Create()
        {
            var currentUser = _context.Users.First(x => x.Login.Equals(HttpContext.User.Identity.Name));
            ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "Name");
            ViewData["ModereatorID"] = new SelectList(_context.Users, "ID", "FullName",currentUser.ID);
            return View();
        }

        // POST: Games/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,ProductionDate,Developer,Platforms,Language,ModereatorID,Description,GenreID")] House game, IFormFile file)
        {
            if (ModelState.IsValid)
            {
                if (file == null)
                {
                    game.Image = "/img/game.png";
                }

                _context.Add(game);
                await _context.SaveChangesAsync();

                if (file != null)
                {
                    string wwwPath = this.Environment.WebRootPath;
                    string contentPath = this.Environment.ContentRootPath;

                    House game2 = _context.Games.First(x => x.Name.Equals(game.Name));

                    string path = Path.Combine(this.Environment.WebRootPath, "Images/Games/" + game2.ID + "/");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }

                    string fileName = Path.GetFileName(file.FileName);
                    using (FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                    {
                        file.CopyTo(stream);
                    }

                    game2.Image = "/Images/Games/" + game2.ID + "/" + fileName;

                    _context.Games.Update(game2);
                    _context.SaveChanges();
                    return RedirectToAction(nameof(Index));
                }
            }
            ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "Name", game.GenreID);
            ViewData["ModereatorID"] = new SelectList(_context.Users, "ID", "FullName", game.ModereatorID);
            return View(game);
        }

        // GET: Games/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _context.Games.FindAsync(id);
            if (game == null)
            {
                return NotFound();
            }
            ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "Name", game.GenreID);
            ViewData["ModereatorID"] = new SelectList(_context.Users, "ID", "FullName", game.ModereatorID);
            return View(game);
        }

        // POST: Games/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,ProductionDate,Developer,Platforms,Language,ModereatorID,Raiting,Description,GenreID")] House game, IFormFile file)
        {
            if (id != game.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (file != null)
                    {
                        string wwwPath = this.Environment.WebRootPath;
                        string contentPath = this.Environment.ContentRootPath;

                        string path = Path.Combine(this.Environment.WebRootPath, "Images/Games/" + game.ID);
                        if (!Directory.Exists(path))
                        {
                            Directory.CreateDirectory(path);
                        }

                        string fileName = Path.GetFileName(file.FileName);
                        using (FileStream stream = new FileStream(Path.Combine(path, fileName), FileMode.Create))
                        {
                            file.CopyTo(stream);
                        }
                        
                        game.Image = "/Images/Games/" + game.ID + "/" + fileName;
                    }
                    _context.Update(game);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GameExists(game.ID))
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
            ViewData["GenreID"] = new SelectList(_context.Genres, "ID", "ID", game.GenreID);
            ViewData["ModereatorID"] = new SelectList(_context.Users, "ID", "ID", game.ModereatorID);
            return View(game);
        }

        // GET: Games/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var game = await _context.Games
                .Include(g => g.Genre)
                .Include(g => g.Modereator)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        // POST: Games/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var game = await _context.Games.FindAsync(id);
            _context.Games.Remove(game);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GameExists(int id)
        {
            return _context.Games.Any(e => e.ID == id);
        }*/
    }
}
