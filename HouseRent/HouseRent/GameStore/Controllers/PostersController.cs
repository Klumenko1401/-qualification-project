using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GameStore.Data;
using HouseRent.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System.IO;
using GameStore.Models;
using Aspose.Words;
using Microsoft.AspNetCore.Authorization;

namespace HouseRent.Controllers
{
    public class PostersController : Controller
    {
        private readonly HouseRentContext _context;
        private readonly IHostingEnvironment _environment;

        public PostersController(HouseRentContext context, IHostingEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: Posters
        public async Task<IActionResult> Index(string? KeyWords, int? PosterTypeID, int? MinPrice, int? MaxPrice, int? Days)
        {
            if (User.IsInRole("Покупець"))
            {
                return Forbid();
            }

            var houseRentContext = _context.Posters
                .Include(p => p.Owner)
                .Include(p => p.PosterStatus)
                .Include(p => p.PosterType)
                .Where(x => x.PosterStatusID == 3); // Активне (ID 3)

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
                houseRentContext = houseRentContext.Where(x => x.Price >= MinPrice);
            }

            if (MaxPrice != null)
            {
                houseRentContext = houseRentContext.Where(x => x.Price <= MaxPrice);
            }

            if (Days != null && Days.HasValue)
            {
                houseRentContext = houseRentContext.Where(x => x.MinRentDays.HasValue && x.MinRentDays < Days);
            }

            ViewData["PosterTypeID"] = new SelectList(_context.PosterType, "ID", "Name");
            return View("Index", await houseRentContext.ToListAsync());
        }

        public async Task<IActionResult> All()
        {
            var houseRentContext = _context.Posters
                .Include(p => p.Owner)
                .Include(p => p.PosterStatus)
                .Include(p => p.PosterType);
            return View("Index", await houseRentContext.ToListAsync());
        }

        public async Task<IActionResult> Modering()
        {
            var houseRentContext = _context.Posters
                .Include(p => p.Owner)
                .Include(p => p.PosterStatus)
                .Include(p => p.PosterType)
                .Where(x => x.PosterStatusID == 4); // Модерація (ID 4)
            return View("Index", await houseRentContext.ToListAsync());
        }

        public async Task<IActionResult> MyPosters(string? statusFilter)
        {
            var currentUser = _context.Users.Include(x => x.Role)
                .Include(u => u.VerificationStatus)
                .FirstOrDefault(x => x.Login.Equals(HttpContext.User.Identity.Name));
            if (currentUser == null)
            {
                return Unauthorized();
            }

            var houseRentContext = _context.Posters
                .Include(p => p.Owner)
                .Include(p => p.PosterStatus)
                .Include(p => p.PosterType)
                .Where(x => x.OwnerID == currentUser.ID);

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                int statusId = statusFilter switch
                {
                    "Відхилене" => 1,
                    "Неактивне" => 2,
                    "Активне" => 3,
                    "Модерація" => 4,
                    "В процесі оформлення" => 5,
                    "В процесі" => 6,
                    _ => 0 // Якщо статус невідомий, показуємо всі
                };
                if (statusId != 0)
                {
                    houseRentContext = houseRentContext.Where(x => x.PosterStatusID == statusId);
                }
            }

            // Перевірка статусу верифікації для створення оголошень
            bool canCreatePoster = true;
            string verificationMessage = null;
            if (currentUser.Role.Name == "Продавець")
            {
                var verifiedStatusId = _context.DocumentStatuses.First(s => s.Name == "Перевірений").ID;
                if (currentUser.VerificationStatusID != verifiedStatusId)
                {
                    canCreatePoster = false;
                    verificationMessage = "Ви не можете створювати оголошення, доки ваші документи не будуть перевірені.";
                }
            }

            ViewBag.CanCreatePoster = canCreatePoster;
            ViewBag.VerificationMessage = verificationMessage;

            // Додано для дебагу
            var posterCount = await houseRentContext.CountAsync();
            var allPosters = await houseRentContext.ToListAsync();
            if (posterCount == 0)
            {
                var totalPostersForUser = await _context.Posters
                    .Include(p => p.PosterStatus)
                    .Where(x => x.OwnerID == currentUser.ID)
                    .ToListAsync();
                if (totalPostersForUser.Any())
                {
                    var statuses = totalPostersForUser.Select(p => p.PosterStatus.Name).Distinct().ToList();
                    ViewBag.DebugStatuses = string.Join(", ", statuses);
                }
                else
                {
                    ViewBag.DebugMessage = "У користувача немає оголошень.";
                }
            }

            return View("Index", allPosters);
        }

        // GET: Posters/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var poster = await _context.Posters
                .Include(p => p.Owner)
                .Include(p => p.PosterStatus)
                .Include(p => p.PosterType)
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(m => m.ID == id);

            if (User.Identity.IsAuthenticated)
            {
                var currentUser = _context.Users
                    .Include(x => x.Role)
                    .Include(u => u.VerificationStatus)
                    .First(x => x.Login.Equals(HttpContext.User.Identity.Name));

                if (currentUser.Role.Name.Equals("Продавець") && currentUser.ID == poster.OwnerID && poster.PosterStatusID != 5 && poster.PosterStatusID != 6)
                {
                    ViewBag.Editable = true;
                }
                else
                {
                    ViewBag.Editable = false;
                }

                bool canCreateOrder = true;
                string verificationMessage = null;
                if (currentUser.Role.Name == "Покупець")
                {
                    var verifiedStatusId = _context.DocumentStatuses.First(s => s.Name == "Перевірений").ID;
                    if (currentUser.VerificationStatusID != verifiedStatusId)
                    {
                        canCreateOrder = false;
                        verificationMessage = "Ви не можете оформити договір, доки ваші документи не будуть перевірені.";
                    }
                }

                ViewBag.CanCreateOrder = canCreateOrder;
                ViewBag.VerificationMessage = verificationMessage;
            }
            else
            {
                ViewBag.Editable = false;
                ViewBag.CanCreateOrder = false;
                ViewBag.VerificationMessage = "Увійдіть, щоб оформити договір.";
            }

            if (poster == null)
            {
                return NotFound();
            }

            return View(poster);
        }

        // GET: Posters/Create
        public IActionResult Create()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = _context.Users
                    .Include(u => u.VerificationStatus)
                    .Include(u => u.Role)
                    .FirstOrDefault(u => u.Login == HttpContext.User.Identity.Name);

                if (user != null && user.Role.Name == "Продавець")
                {
                    var verifiedStatusId = _context.DocumentStatuses.First(s => s.Name == "Перевірений").ID;
                    if (user.VerificationStatusID != verifiedStatusId)
                    {
                        TempData["Error"] = "Ви не можете створювати оголошення, доки ваші документи не будуть перевірені.";
                        return RedirectToAction("MyPosters");
                    }
                }
            }

            ViewData["OwnerID"] = new SelectList(_context.Users, "ID", "FullName");
            ViewData["PosterStatusID"] = new SelectList(_context.PosterStatuses, "ID", "Name");
            ViewData["PosterTypeID"] = new SelectList(_context.PosterType, "ID", "Name");
            return View();
        }

        // POST: Posters/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,Name,Description,PosterTypeID,MinRentDays,Price,MaxPayTerms,ContractTemplateImage,ContactDetails,PaymentAccount,IsRental,IsSale,IsLongTermRental,IsLongTermWithBuyout,IsShortTermRental,IsFullPayment,IsInstallmentPayment")] Poster poster, IFormFile file, IFormFile contractTemplateFile, List<IFormFile> additionalPhotos)
        {
            var user = await _context.Users
                .Include(u => u.VerificationStatus)
                .FirstAsync(u => u.Login == HttpContext.User.Identity.Name);
            var verifiedStatusId = _context.DocumentStatuses.First(s => s.Name == "Перевірений").ID;
            if (user.VerificationStatusID != verifiedStatusId)
            {
                TempData["Error"] = "Ви не можете створювати оголошення, доки ваші документи не будуть перевірені.";
                return RedirectToAction("MyPosters");
            }

            if (ModelState.IsValid)
            {
                var currentUser = _context.Users.Include(x => x.Role).First(x => x.Login.Equals(HttpContext.User.Identity.Name));

                poster.OwnerID = currentUser.ID;
                poster.Raiting = 0;
                poster.PosterStatusID = 4;

                if (poster.IsRental)
                {
                    poster.IsSale = false;
                    if (poster.IsLongTermRental)
                    {
                        poster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Оренда (довгострокова)")).ID;
                    }
                    else if (poster.IsLongTermWithBuyout)
                    {
                        poster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Оренда (довгострокова з викупом)")).ID;
                    }
                    else if (poster.IsShortTermRental)
                    {
                        poster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Оренда (на день)")).ID;
                    }
                    else
                    {
                        poster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Оренда (на день)")).ID;
                    }
                }
                else if (poster.IsSale)
                {
                    poster.IsRental = false;
                    if (poster.IsInstallmentPayment)
                    {
                        poster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Продаж (Частями)")).ID;
                        poster.IsFullPayment = false;
                    }
                    else
                    {
                        poster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Продаж (Повна оплата)")).ID;
                        poster.IsInstallmentPayment = false;
                        poster.IsFullPayment = true;
                    }
                }
                else
                {
                    poster.IsSale = true;
                    poster.IsRental = false;
                    poster.IsFullPayment = true;
                    poster.IsInstallmentPayment = false;
                    poster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Продаж (Повна оплата)")).ID;
                }

                _context.Add(poster);
                await _context.SaveChangesAsync();

                Poster poster2 = _context.Posters.OrderByDescending(p => p.ID).First();

                if (file != null && file.Length > 0)
                {
                    string path = Path.Combine(_environment.WebRootPath, "Images/Posters", poster2.ID.ToString());
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

                    poster2.Image = $"/Images/Posters/{poster2.ID}/{fileName}";
                }
                else
                {
                    poster2.Image = "/img/logo.jpg";
                }

                if (contractTemplateFile != null && contractTemplateFile.Length > 0)
                {
                    string contractPath = Path.Combine(_environment.WebRootPath, "Images/Contracts", poster2.ID.ToString());
                    if (!Directory.Exists(contractPath))
                    {
                        Directory.CreateDirectory(contractPath);
                    }

                    string docFileName = Guid.NewGuid().ToString() + Path.GetExtension(contractTemplateFile.FileName);
                    string docFullPath = Path.Combine(contractPath, docFileName);
                    using (var stream = new FileStream(docFullPath, FileMode.Create))
                    {
                        await contractTemplateFile.CopyToAsync(stream);
                    }

                    string pdfFileName = Guid.NewGuid().ToString() + ".pdf";
                    string pdfFullPath = Path.Combine(contractPath, pdfFileName);
                    Document doc = new Document(docFullPath);
                    doc.Save(pdfFullPath, SaveFormat.Pdf);

                    poster2.ContractTemplateImage = $"/Images/Contracts/{poster2.ID}/{docFileName}";
                    poster2.ContractTemplatePdf = $"/Images/Contracts/{poster2.ID}/{pdfFileName}";
                }

                if (additionalPhotos != null && additionalPhotos.Any())
                {
                    string photosPath = Path.Combine(_environment.WebRootPath, "Images/Posters", poster2.ID.ToString());
                    if (!Directory.Exists(photosPath))
                    {
                        Directory.CreateDirectory(photosPath);
                    }

                    foreach (var photo in additionalPhotos)
                    {
                        if (photo != null && photo.Length > 0)
                        {
                            string additionalFileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                            string additionalFullPath = Path.Combine(photosPath, additionalFileName);
                            using (var stream = new FileStream(additionalFullPath, FileMode.Create))
                            {
                                await photo.CopyToAsync(stream);
                            }

                            var newPhoto = new Photo
                            {
                                PosterID = poster2.ID,
                                Image = $"/Images/Posters/{poster2.ID}/{additionalFileName}"
                            };
                            _context.Photos.Add(newPhoto);
                        }
                    }
                    await _context.SaveChangesAsync();
                }

                _context.Update(poster2);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", new { id = poster2.ID });
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            ViewBag.ModelStateErrors = errors.Any() ? string.Join("; ", errors) : "No ModelState errors";

            ViewData["OwnerID"] = new SelectList(_context.Users, "ID", "FullName", poster.OwnerID);
            ViewData["PosterStatusID"] = new SelectList(_context.PosterStatuses, "ID", "Name", poster.PosterStatusID);
            ViewData["PosterTypeID"] = new SelectList(_context.PosterType, "ID", "Name", poster.PosterTypeID);
            return View(poster);
        }

        // GET: Posters/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var poster = await _context.Posters
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (poster == null)
            {
                return NotFound();
            }
            ViewData["OwnerID"] = new SelectList(_context.Users, "ID", "FullName", poster.OwnerID);
            ViewData["PosterStatusID"] = new SelectList(_context.PosterStatuses, "ID", "Name", poster.PosterStatusID);
            ViewData["PosterTypeID"] = new SelectList(_context.PosterType, "ID", "Name", poster.PosterTypeID);
            return View(poster);
        }

        // POST: Posters/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,Name,Description,PosterTypeID,MinRentDays,Price,MaxPayTerms,Image,ContractTemplateImage,ContractTemplatePdf,ContactDetails,PaymentAccount,IsRental,IsSale,IsLongTermRental,IsLongTermWithBuyout,IsShortTermRental,IsFullPayment,IsInstallmentPayment,Raiting,OwnerID")] Poster poster, IFormFile file, IFormFile contractTemplateFile, List<IFormFile> additionalPhotos)
        {
            if (id != poster.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var existingPoster = await _context.Posters
                        .Include(p => p.Photos)
                        .FirstOrDefaultAsync(p => p.ID == id);
                    if (existingPoster == null)
                    {
                        return NotFound();
                    }

                    existingPoster.Name = poster.Name;
                    existingPoster.Description = poster.Description;
                    existingPoster.IsRental = poster.IsRental;
                    existingPoster.IsSale = poster.IsSale;
                    existingPoster.IsLongTermRental = poster.IsLongTermRental;
                    existingPoster.IsLongTermWithBuyout = poster.IsLongTermWithBuyout;
                    existingPoster.IsShortTermRental = poster.IsShortTermRental;
                    existingPoster.IsFullPayment = poster.IsFullPayment;
                    existingPoster.IsInstallmentPayment = poster.IsInstallmentPayment;
                    existingPoster.MinRentDays = poster.MinRentDays;
                    existingPoster.Price = poster.Price;
                    existingPoster.MaxPayTerms = poster.MaxPayTerms;
                    existingPoster.ContactDetails = poster.ContactDetails;
                    existingPoster.PaymentAccount = poster.PaymentAccount;
                    existingPoster.Raiting = poster.Raiting;
                    existingPoster.OwnerID = poster.OwnerID;

                    if (existingPoster.IsRental)
                    {
                        existingPoster.IsSale = false;
                        if (existingPoster.IsLongTermRental)
                        {
                            existingPoster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Оренда (довгострокова)")).ID;
                        }
                        else if (existingPoster.IsLongTermWithBuyout)
                        {
                            existingPoster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Оренда (довгострокова з викупом)")).ID;
                        }
                        else if (existingPoster.IsShortTermRental)
                        {
                            existingPoster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Оренда (на день)")).ID;
                        }
                        else
                        {
                            existingPoster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Оренда (на день)")).ID;
                        }
                    }
                    else if (existingPoster.IsSale)
                    {
                        existingPoster.IsRental = false;
                        if (existingPoster.IsInstallmentPayment)
                        {
                            existingPoster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Продаж (Частями)")).ID;
                            existingPoster.IsFullPayment = false;
                        }
                        else
                        {
                            existingPoster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Продаж (Повна оплата)")).ID;
                            existingPoster.IsInstallmentPayment = false;
                            existingPoster.IsFullPayment = true;
                        }
                    }
                    else
                    {
                        existingPoster.IsSale = true;
                        existingPoster.IsRental = false;
                        existingPoster.IsFullPayment = true;
                        existingPoster.IsInstallmentPayment = false;
                        existingPoster.PosterTypeID = _context.PosterType.First(x => x.Name.Equals("Продаж (Повна оплата)")).ID;
                    }

                    if (existingPoster.PosterStatusID == 2)
                    {
                        existingPoster.PosterStatusID = 4;
                    }

                    if (file != null && file.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(existingPoster.Image) && existingPoster.Image != "/img/logo.jpg")
                        {
                            string oldImagePath = Path.Combine(_environment.WebRootPath, existingPoster.Image.TrimStart('/'));
                            if (System.IO.File.Exists(oldImagePath))
                            {
                                System.IO.File.Delete(oldImagePath);
                            }
                        }

                        string path = Path.Combine(_environment.WebRootPath, "Images/Posters", existingPoster.ID.ToString());
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

                        existingPoster.Image = $"/Images/Posters/{existingPoster.ID}/{fileName}";
                    }

                    if (contractTemplateFile != null && contractTemplateFile.Length > 0)
                    {
                        if (!string.IsNullOrEmpty(existingPoster.ContractTemplateImage))
                        {
                            string oldContractPath = Path.Combine(_environment.WebRootPath, existingPoster.ContractTemplateImage.TrimStart('/'));
                            if (System.IO.File.Exists(oldContractPath))
                            {
                                System.IO.File.Delete(oldContractPath);
                            }
                        }
                        if (!string.IsNullOrEmpty(existingPoster.ContractTemplatePdf))
                        {
                            string oldPdfPath = Path.Combine(_environment.WebRootPath, existingPoster.ContractTemplatePdf.TrimStart('/'));
                            if (System.IO.File.Exists(oldPdfPath))
                            {
                                System.IO.File.Delete(oldPdfPath);
                            }
                        }

                        string contractPath = Path.Combine(_environment.WebRootPath, "Images/Contracts", existingPoster.ID.ToString());
                        if (!Directory.Exists(contractPath))
                        {
                            Directory.CreateDirectory(contractPath);
                        }

                        string docFileName = Guid.NewGuid().ToString() + Path.GetExtension(contractTemplateFile.FileName);
                        string docFullPath = Path.Combine(contractPath, docFileName);
                        using (var stream = new FileStream(docFullPath, FileMode.Create))
                        {
                            await contractTemplateFile.CopyToAsync(stream);
                        }

                        string pdfFileName = Guid.NewGuid().ToString() + ".pdf";
                        string pdfFullPath = Path.Combine(contractPath, pdfFileName);
                        Document doc = new Document(docFullPath);
                        doc.Save(pdfFullPath, SaveFormat.Pdf);

                        existingPoster.ContractTemplateImage = $"/Images/Contracts/{existingPoster.ID}/{docFileName}";
                        existingPoster.ContractTemplatePdf = $"/Images/Contracts/{existingPoster.ID}/{pdfFileName}";
                    }

                    if (additionalPhotos != null && additionalPhotos.Any())
                    {
                        string photosPath = Path.Combine(_environment.WebRootPath, "Images/Posters", existingPoster.ID.ToString());
                        if (!Directory.Exists(photosPath))
                        {
                            Directory.CreateDirectory(photosPath);
                        }

                        foreach (var photo in additionalPhotos)
                        {
                            if (photo != null && photo.Length > 0)
                            {
                                string additionalFileName = Guid.NewGuid().ToString() + Path.GetExtension(photo.FileName);
                                string additionalFullPath = Path.Combine(photosPath, additionalFileName);
                                using (var stream = new FileStream(additionalFullPath, FileMode.Create))
                                {
                                    await photo.CopyToAsync(stream);
                                }

                                var newPhoto = new Photo
                                {
                                    PosterID = existingPoster.ID,
                                    Image = $"/Images/Posters/{existingPoster.ID}/{additionalFileName}"
                                };
                                _context.Photos.Add(newPhoto);
                            }
                        }
                        await _context.SaveChangesAsync();
                    }

                    _context.Update(existingPoster);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Details", new { id = existingPoster.ID });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PosterExists(poster.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            ViewBag.ModelStateErrors = errors.Any() ? string.Join("; ", errors) : "No ModelState errors";

            ViewData["OwnerID"] = new SelectList(_context.Users, "ID", "FullName", poster.OwnerID);
            ViewData["PosterStatusID"] = new SelectList(_context.PosterStatuses, "ID", "Name", poster.PosterStatusID);
            ViewData["PosterTypeID"] = new SelectList(_context.PosterType, "ID", "Name", poster.PosterTypeID);
            return View(poster);
        }

        // GET: Posters/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var poster = await _context.Posters
                .Include(p => p.Owner)
                .Include(p => p.PosterStatus)
                .Include(p => p.PosterType)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (poster == null)
            {
                return NotFound();
            }

            return View(poster);
        }

        // POST: Posters/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var poster = await _context.Posters
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(m => m.ID == id);
            if (poster == null)
            {
                return NotFound();
            }

            if (!string.IsNullOrEmpty(poster.Image) && poster.Image != "/img/logo.jpg")
            {
                string imagePath = Path.Combine(_environment.WebRootPath, poster.Image.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            if (!string.IsNullOrEmpty(poster.ContractTemplateImage))
            {
                string contractPath = Path.Combine(_environment.WebRootPath, poster.ContractTemplateImage.TrimStart('/'));
                if (System.IO.File.Exists(contractPath))
                {
                    System.IO.File.Delete(contractPath);
                }
            }

            if (!string.IsNullOrEmpty(poster.ContractTemplatePdf))
            {
                string pdfPath = Path.Combine(_environment.WebRootPath, poster.ContractTemplatePdf.TrimStart('/'));
                if (System.IO.File.Exists(pdfPath))
                {
                    System.IO.File.Delete(pdfPath);
                }
            }

            if (poster.Photos != null && poster.Photos.Any())
            {
                foreach (var photo in poster.Photos)
                {
                    if (!string.IsNullOrEmpty(photo.Image))
                    {
                        string photoPath = Path.Combine(_environment.WebRootPath, photo.Image.TrimStart('/'));
                        if (System.IO.File.Exists(photoPath))
                        {
                            System.IO.File.Delete(photoPath);
                        }
                    }
                }
                _context.Photos.RemoveRange(poster.Photos);
            }

            _context.Posters.Remove(poster);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(MyPosters));
        }

        // POST: Posters/DeleteMainPhoto
        [HttpPost]
        public async Task<IActionResult> DeleteMainPhoto(int posterId)
        {
            var poster = await _context.Posters.FindAsync(posterId);
            if (poster == null)
            {
                return Json(new { success = false, message = "Оголошення не знайдено." });
            }

            if (!string.IsNullOrEmpty(poster.Image) && poster.Image != "/img/logo.jpg")
            {
                string imagePath = Path.Combine(_environment.WebRootPath, poster.Image.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
                poster.Image = "/img/logo.jpg";
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true });
        }

        public IActionResult ViewContractTemplate(int id)
        {
            var poster = _context.Posters.Find(id);
            if (poster == null || string.IsNullOrEmpty(poster.ContractTemplatePdf))
            {
                return NotFound();
            }

            var pdfPath = Path.Combine(_environment.WebRootPath, poster.ContractTemplatePdf.TrimStart('/'));
            if (!System.IO.File.Exists(pdfPath))
            {
                return NotFound("PDF-файл не знайдено за шляхом: " + pdfPath);
            }

            var docxPath = Path.Combine(_environment.WebRootPath, poster.ContractTemplateImage.TrimStart('/'));
            if (!System.IO.File.Exists(docxPath))
            {
            }

            ViewBag.PosterId = id;
            return View("ContractTemplate", poster);
        }

        [HttpGet]
        [Authorize(Roles = "Адміністратор")]
        public async Task<IActionResult> Accept(int id)
        {
            var poster = await _context.Posters
                .Include(p => p.PosterStatus)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (poster == null)
            {
                return NotFound("Оголошення не знайдено.");
            }

            if (poster.PosterStatusID != 4)
            {
                return BadRequest("Оголошення не перебуває на модерації.");
            }

            poster.PosterStatusID = 3;
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = poster.ID });
        }

        [HttpGet]
        [Authorize(Roles = "Адміністратор")]
        public async Task<IActionResult> Return(int id)
        {
            var poster = await _context.Posters
                .Include(p => p.PosterStatus)
                .FirstOrDefaultAsync(p => p.ID == id);

            if (poster == null)
            {
                return NotFound("Оголошення не знайдено.");
            }

            if (poster.PosterStatusID != 4)
            {
                return BadRequest("Оголошення не перебуває на модерації.");
            }

            poster.PosterStatusID = 1;
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", new { id = poster.ID });
        }

        private bool PosterExists(int id)
        {
            return _context.Posters.Any(e => e.ID == id);
        }

        // Метод для отримання або створення статусу
        private int GetOrCreateDefaultStatusId(string statusName)
        {
            var status = _context.DocumentStatuses.FirstOrDefault(x => x.Name == statusName);
            if (status == null)
            {
                status = new DocumentStatus { Name = statusName };
                _context.DocumentStatuses.Add(status);
                _context.SaveChanges();
            }
            return status.ID;
        }

        [HttpPost]
        [Authorize(Roles = "Адміністратор")]
        public async Task<IActionResult> ResetDatabase()
        {
            // Очищення таблиць
            /*await _context.Database.ExecuteSqlRawAsync("DELETE FROM Payment");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM [Order]");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Photo");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM Poster");
            await _context.Database.ExecuteSqlRawAsync("DELETE FROM UserDocument");

            // Скидання лічильників
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Payment', RESEED, 0)");
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('[Order]', RESEED, 0)");
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Photo', RESEED, 0)");
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('Poster', RESEED, 0)");
            await _context.Database.ExecuteSqlRawAsync("DBCC CHECKIDENT ('UserDocument', RESEED, 0)");*/

            // Скидання VerificationStatusID у всіх користувачів на "Немає документів"
            var noDocumentsStatusId = GetOrCreateDefaultStatusId("Немає документів");
            var users = await _context.Users
                .Include(u => u.UserDocuments)
                .ToListAsync();
            foreach (var user in users)
            {
                user.VerificationStatusID = noDocumentsStatusId;
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("Index", "Home");
        }
    }
}