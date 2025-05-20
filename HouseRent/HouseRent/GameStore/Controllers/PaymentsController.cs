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

namespace HouseRent.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly HouseRentContext _context;

        public PaymentsController(HouseRentContext context)
        {
            _context = context;
        }

        // GET: Payments
        public async Task<IActionResult> Index()
        {
            var houseRentContext = _context.Payments.Include(p => p.Order).Include(p => p.PaymentStatus);
            return View(await houseRentContext.ToListAsync());
        }

        // GET: Payments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Order)
                .Include(p => p.PaymentStatus)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // GET: Payments/Create
        public IActionResult Create()
        {
            ViewData["OrderID"] = new SelectList(_context.Orders, "ID", "ID");
            ViewData["PaymentStatusID"] = new SelectList(_context.PaymentStatuses, "ID", "ID");
            return View();
        }

        // POST: Payments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,OrderID,PaymentStatusID,PaymentDate,Amount")] Payment payment)
        {
            if (ModelState.IsValid)
            {
                _context.Add(payment);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["OrderID"] = new SelectList(_context.Orders, "ID", "ID", payment.OrderID);
            ViewData["PaymentStatusID"] = new SelectList(_context.PaymentStatuses, "ID", "ID", payment.PaymentStatusID);
            return View(payment);
        }

        // GET: Payments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }
            ViewData["OrderID"] = new SelectList(_context.Orders, "ID", "ID", payment.OrderID);
            ViewData["PaymentStatusID"] = new SelectList(_context.PaymentStatuses, "ID", "ID", payment.PaymentStatusID);
            return View(payment);
        }

        // GET: Payments/Edit/5
        public async Task<IActionResult> Pay(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null)
            {
                return NotFound();
            }

            var paymentStatus = _context.PaymentStatuses.FirstOrDefault(x => x.Name.Equals("Виконана"));
            if (paymentStatus == null)
            {
                return NotFound("Статус 'Виконана' не знайдено.");
            }

            payment.PaymentStatusID = paymentStatus.ID;
            payment.PaymentDate = DateTime.Now; // Встановлюємо дату оплати

            _context.Payments.Update(payment);
            await _context.SaveChangesAsync();

            // Перевірка, чи всі платежі для замовлення виконані
            int allCount = _context.Payments
                .Where(x => x.OrderID == payment.OrderID)
                .Count();
            int successCount = _context.Payments
                .Where(x => x.PaymentStatusID == paymentStatus.ID && x.OrderID == payment.OrderID)
                .Count();

            if (allCount == successCount)
            {
                var order = await _context.Orders.FirstOrDefaultAsync(x => x.ID == payment.OrderID);
                if (order == null)
                {
                    return NotFound("Замовлення не знайдено.");
                }

                var orderStatus = _context.OrderStatuses.FirstOrDefault(x => x.Name.Equals("Завершене"));
                if (orderStatus == null)
                {
                    return NotFound("Статус 'Завершене' не знайдено.");
                }

                order.OrderDate = DateTime.Now;
                order.OrderStatusID = orderStatus.ID;

                _context.Orders.Update(order);
                await _context.SaveChangesAsync();

                var poster = await _context.Posters.FirstOrDefaultAsync(x => x.ID == order.PosterID);
                if (poster == null)
                {
                    return NotFound("Оголошення не знайдено.");
                }

                var posterStatus = _context.PosterStatuses.FirstOrDefault(x => x.Name.Equals("Неактивне"));
                if (posterStatus == null)
                {
                    return NotFound("Статус 'Неактивне' не знайдено.");
                }

                poster.PosterStatusID = posterStatus.ID;

                _context.Posters.Update(poster);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Details", "Orders", new { id = payment.OrderID });
        }


        // POST: Payments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,OrderID,PaymentStatusID,PaymentDate,Amount")] Payment payment)
        {
            if (id != payment.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(payment);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PaymentExists(payment.ID))
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
            ViewData["OrderID"] = new SelectList(_context.Orders, "ID", "ID", payment.OrderID);
            ViewData["PaymentStatusID"] = new SelectList(_context.PaymentStatuses, "ID", "ID", payment.PaymentStatusID);
            return View(payment);
        }

        // GET: Payments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var payment = await _context.Payments
                .Include(p => p.Order)
                .Include(p => p.PaymentStatus)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (payment == null)
            {
                return NotFound();
            }

            return View(payment);
        }

        // POST: Payments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            _context.Payments.Remove(payment);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool PaymentExists(int id)
        {
            return _context.Payments.Any(e => e.ID == id);
        }
    }
}
