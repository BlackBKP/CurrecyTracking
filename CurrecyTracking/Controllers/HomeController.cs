using System.Diagnostics;
using CurrecyTracking.Models;
using Microsoft.AspNetCore.Mvc;

namespace CurrecyTracking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private static readonly object CurrencyLock = new();
        private static readonly List<CurrencyModel> masterCurrencies = new()
        {
            new() { CurrencyCode = "THB", CurrencyName = "Thai Baht", IsActive = true },
            new() { CurrencyCode = "USD", CurrencyName = "United States Dollar", IsActive = true },
            new() { CurrencyCode = "SGD", CurrencyName = "Singapore Dollar", IsActive = false }
        };

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        // Master Currency
        public IActionResult CurrencyMaster()
        {
            return View(CurrencySnapshot());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCurrency(CurrencyModel currency)
        {
            if (!ModelState.IsValid)
                return View("CurrencyMaster", CurrencySnapshot());

            currency.CurrencyCode = currency.CurrencyCode.ToUpperInvariant();
            currency.CurrencyName = currency.CurrencyName.Trim();
            lock (CurrencyLock)
            {
                if (masterCurrencies.Any(c => c.CurrencyCode == currency.CurrencyCode))
                {
                    ModelState.AddModelError(nameof(currency.CurrencyCode), "A currency with this code already exists.");
                    return View("CurrencyMaster", CurrencySnapshot());
                }
                masterCurrencies.Add(currency);
            }
            TempData["Success"] = "Currency added.";
            return RedirectToAction(nameof(CurrencyMaster));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCurrency(CurrencyModel currency)
        {
            if (!ModelState.IsValid)
                return View("CurrencyMaster", CurrencySnapshot());
            lock (CurrencyLock)
            {
                var existing = masterCurrencies.Find(c => c.CurrencyCode == currency.CurrencyCode.ToUpperInvariant());
                if (existing == null)
                    return NotFound();
                existing.CurrencyName = currency.CurrencyName.Trim();
                existing.IsActive = currency.IsActive;
            }
            TempData["Success"] = "Currency updated.";
            return RedirectToAction(nameof(CurrencyMaster));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCurrency(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                return BadRequest();
            lock (CurrencyLock)
            {
                var existing = masterCurrencies.Find(c => c.CurrencyCode == currencyCode.ToUpperInvariant());
                if (existing == null)
                    return NotFound();
                masterCurrencies.Remove(existing);
            }
            TempData["Success"] = "Currency deleted.";
            return RedirectToAction(nameof(CurrencyMaster));
        }

        private static List<CurrencyModel> CurrencySnapshot()
        {
            lock (CurrencyLock)
            {
                return masterCurrencies.Select(c => new CurrencyModel
                {
                    CurrencyCode = c.CurrencyCode,
                    CurrencyName = c.CurrencyName,
                    IsActive = c.IsActive
                }).ToList();
            }
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
