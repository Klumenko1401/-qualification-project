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
using Microsoft.AspNetCore.Hosting;
using Aspose.Words;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace HouseRent.Controllers
{
    public class OrdersController : Controller
    {
        private readonly HouseRentContext _context;
        private readonly IHostingEnvironment _environment;

        public OrdersController(HouseRentContext context, IHostingEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // POST: Orders/UploadContract/5
        [HttpPost]
        public async Task<IActionResult> UploadContract(int id, IFormFile contractFile)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            if (contractFile != null && contractFile.Length > 0)
            {
                string contractPath = Path.Combine(_environment.WebRootPath, "Images/Contracts", order.ID.ToString());
                if (!Directory.Exists(contractPath))
                {
                    Directory.CreateDirectory(contractPath);
                }

                string docFileName = Guid.NewGuid().ToString() + Path.GetExtension(contractFile.FileName);
                string docFullPath = Path.Combine(contractPath, docFileName);
                using (var stream = new FileStream(docFullPath, FileMode.Create))
                {
                    await contractFile.CopyToAsync(stream);
                }

                // Конвертація у PDF за допомогою Aspose.Words
                string pdfFileName = Guid.NewGuid().ToString() + ".pdf";
                string pdfFullPath = Path.Combine(contractPath, pdfFileName);
                Document doc = new Document(docFullPath);
                doc.Save(pdfFullPath, SaveFormat.Pdf);

                // Зберігаємо шлях до PDF
                order.ContractFilePath = $"/Images/Contracts/{order.ID}/{pdfFileName}";
                order.OrderStatusID = 6; // "Перевірка" (ID 6), вважаємо це "В процесі"
                var poster = await _context.Posters.FindAsync(order.PosterID);
                poster.PosterStatusID = 6; // "В процесі" (ID 6)
                _context.Posters.Update(poster);
                _context.Orders.Update(order);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", new { id = order.ID });
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
            var currentUser = _context.Users.Include(x => x.Role).First(x => x.Login.Equals(HttpContext.User.Identity.Name));

            var houseRentContext = _context.Orders.Include(o => o.OrderStatus).Include(o => o.Poster).Include(o => o.User).ToList();

            if (currentUser.Role.Name.Equals("Продавець"))
            {
                houseRentContext = _context.Orders.Include(o => o.OrderStatus).Include(o => o.Poster).Include(o => o.User).Include(x => x.Poster).Where(x => x.Poster.OwnerID == currentUser.ID).ToList();
            }
            else
            {
                houseRentContext = _context.Orders.Include(o => o.OrderStatus).Include(o => o.Poster).Include(o => o.User).Where(x => x.UserID == currentUser.ID).ToList();
            }

            return View(houseRentContext);
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.Poster)
                    .ThenInclude(p => p.Owner)
                .Include(o => o.User)
                .Include(o => o.Payments)
                    .ThenInclude(p => p.PaymentStatus)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (order == null)
            {
                return NotFound();
            }

            // Перевірка, чи є цей договір найновішим для цього оголошення
            var latestOrder = await _context.Orders
                .Where(o => o.PosterID == order.PosterID)
                .OrderByDescending(o => o.ID)
                .FirstOrDefaultAsync();
            ViewBag.IsLatestOrder = (latestOrder != null && latestOrder.ID == order.ID);

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            var user = _context.Users
                .Include(u => u.VerificationStatus)
                .FirstOrDefault(u => u.Login == HttpContext.User.Identity.Name);
            var verifiedStatusId = _context.DocumentStatuses.FirstOrDefault(s => s.Name == "Перевірений")?.ID;
            if (user.VerificationStatusID != verifiedStatusId)
            {
                TempData["Error"] = "Ви не можете подавати заявки, доки ваші документи не будуть перевірені.";
                return RedirectToAction("Index");
            }

            ViewData["OrderStatusID"] = new SelectList(_context.OrderStatuses, "ID", "ID");
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID");
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID");
            return View();
        }

        // POST: Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,PosterID,UserID,OrderDate,OrderStatusID")] Order order)
        {
            var user = await _context.Users
                .Include(u => u.VerificationStatus)
                .FirstAsync(u => u.Login == HttpContext.User.Identity.Name);
            var verifiedStatusId = _context.DocumentStatuses.First(s => s.Name == "Перевірений").ID;
            if (user.VerificationStatusID != verifiedStatusId)
            {
                ModelState.AddModelError("", "Ви не можете подавати заявки, доки ваші документи не будуть перевірені.");
                ViewData["OrderStatusID"] = new SelectList(_context.OrderStatuses, "ID", "ID", order.OrderStatusID);
                ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", order.PosterID);
                ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID", order.UserID);
                return View();
            }

            if (ModelState.IsValid)
            {
                _context.Add(order);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["OrderStatusID"] = new SelectList(_context.OrderStatuses, "ID", "ID", order.OrderStatusID);
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", order.PosterID);
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID", order.UserID);
            return View(order);
        }

        public async Task<IActionResult> CreateNew(int? id)
        {
            var currentUser = _context.Users
                .Include(x => x.Role)
                .Include(u => u.VerificationStatus)
                .First(x => x.Login.Equals(HttpContext.User.Identity.Name));
            var verifiedStatusId = _context.DocumentStatuses.First(s => s.Name == "Перевірений").ID;
            if (currentUser.VerificationStatusID != verifiedStatusId)
            {
                TempData["Error"] = "Ви не можете подавати заявки, доки ваші документи не будуть перевірені.";
                return RedirectToAction("Index");
            }

            Order order = new Order();
            order.PosterID = (int)id;
            order.UserID = currentUser.ID;
            order.OrderDate = DateTime.Now;
            order.OrderStatusID = _context.OrderStatuses.First(x => x.Name.Equals("Заявка")).ID;

            _context.Add(order);
            await _context.SaveChangesAsync();

            return Redirect("~/Orders/Index");
        }

        public async Task<IActionResult> Return(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatusID = _context.OrderStatuses.First(x => x.Name.Equals("Відхилене")).ID;
            order.OrderDate = DateTime.Now;
            _context.Orders.Update(order);

            var poster = await _context.Posters.FindAsync(order.PosterID);
            poster.PosterStatusID = _context.PosterStatuses.First(x => x.Name.Equals("Активне")).ID; // Повертаємо статус 3
            _context.Posters.Update(poster);
            await _context.SaveChangesAsync();

            return Redirect("~/Orders/Index");
        }

        public async Task<IActionResult> Accept(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Змінюємо статус на "Прийнятий" (ID 1) і оголошення на "В процесі оформлення" (ID 5)
            order.OrderStatusID = 1; // Прийнятий
            var poster = await _context.Posters.FindAsync(order.PosterID);
            poster.PosterStatusID = 5; // В процесі оформлення
            _context.Posters.Update(poster);
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();

            return Redirect("~/Orders/Details/" + order.ID);
        }

        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            ViewData["OrderStatusID"] = new SelectList(_context.OrderStatuses, "ID", "ID", order.OrderStatusID);
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", order.PosterID);
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID", order.UserID);
            return View(order);
        }

        // POST: Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,PosterID,UserID,OrderDate,OrderStatusID")] Order order)
        {
            if (id != order.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(order);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OrderExists(order.ID))
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
            ViewData["OrderStatusID"] = new SelectList(_context.OrderStatuses, "ID", "ID", order.OrderStatusID);
            ViewData["PosterID"] = new SelectList(_context.Posters, "ID", "ID", order.PosterID);
            ViewData["UserID"] = new SelectList(_context.Users, "ID", "ID", order.UserID);
            return View(order);
        }

        // GET: Orders/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.OrderStatus)
                .Include(o => o.Poster)
                .Include(o => o.User)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.ID == id);
        }
    }
}