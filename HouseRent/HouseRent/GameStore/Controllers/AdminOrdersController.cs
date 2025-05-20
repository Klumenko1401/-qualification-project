using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using GameStore.Models;
using Microsoft.AspNetCore.Authorization;

namespace HouseRent.Controllers
{
    [Authorize(Roles = "Адміністратор")]
    public class AdminOrdersController : Controller
    {
        private readonly HouseRentContext _context;

        public AdminOrdersController(HouseRentContext context)
        {
            _context = context;
        }

        // GET: AdminOrders/Index
        public async Task<IActionResult> Index()
        {
            var orders = await _context.Orders
                .Include(o => o.Poster)
                .ThenInclude(p => p.Owner) // Для доступу до Poster.Owner.FullName
                .Include(o => o.User)
                .Include(o => o.OrderStatus)
                .ToListAsync(); // Завантажуємо всі договори, незалежно від статусу

            ViewBag.OrderStatuses = await _context.OrderStatuses.ToListAsync(); // Передаємо статуси
            return View(orders);
        }

        // POST: AdminOrders/Confirm
        [HttpPost]
        public async Task<IActionResult> Confirm(int id, decimal amount, int paymentCount, DateTime firstPaymentDate, DateTime lastPaymentDate)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Рівномірний розподіл платежів
            var totalDays = (lastPaymentDate - firstPaymentDate).TotalDays;
            var interval = paymentCount > 1 ? totalDays / (paymentCount - 1) : 0;

            for (int i = 0; i < paymentCount; i++)
            {
                var payment = new Payment
                {
                    OrderID = order.ID,
                    PaymentStatusID = _context.PaymentStatuses.First(x => x.Name.Equals("Невиконана")).ID,
                    PaymentDueDate = firstPaymentDate.AddDays(interval * i),
                    Amount = (int)(amount /*/ paymentCount*/)
                    // PaymentDate залишається null, оскільки оплата ще не виконана
                };
                _context.Payments.Add(payment);
            }

            order.OrderStatusID = 7; // "В процесі" (ID 7)
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Orders", new { id = order.ID });
        }

        // POST: AdminOrders/UpdateOrderStatus
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, int statusId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatusID = statusId;
            await _context.SaveChangesAsync();

            return RedirectToAction("Index");
        }
    }
}