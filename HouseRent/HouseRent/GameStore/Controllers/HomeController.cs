using GameStore.Data;
using GameStore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace GameStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly HouseRentContext _context;

        public HomeController(ILogger<HomeController> logger, HouseRentContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IActionResult Index(string? KeyWords, int? PosterTypeID, int? MinPrice, int? MaxPrice, int? Days)
        {
            var houseRentContext = _context.Posters.Include(p => p.Owner).Include(p => p.PosterStatus).Include(p => p.PosterType).Where(x => x.PosterStatus.Name.Equals("Активне"));

            if (KeyWords != null)
            {
                houseRentContext = houseRentContext.Where(x => x.Name.ToLower().Contains(KeyWords.ToLower()) || x.Description.ToLower().Contains(KeyWords.ToLower()));
            }

            if (PosterTypeID != null)
            {
                houseRentContext = houseRentContext.Where(x => x.PosterTypeID == PosterTypeID);
            }

            if (MinPrice != null)
            {
                houseRentContext = houseRentContext.Where(x => x.Price >= MinPrice); // Змінено на >=
            }

            if (MaxPrice != null)
            {
                houseRentContext = houseRentContext.Where(x => x.Price <= MaxPrice); // Змінено на <=
            }

            if (Days != null)
            {
                houseRentContext = houseRentContext.Where(x => x.MinRentDays <= Days);
            }

            ViewData["PosterTypeID"] = new SelectList(_context.PosterType, "ID", "Name");

            ViewBag.Posters = houseRentContext.ToList();

            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}