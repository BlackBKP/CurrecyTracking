using System.Diagnostics;
using CurrecyTracking.Models;
using Microsoft.AspNetCore.Mvc;

namespace CurrecyTracking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        static List<CurrencyModel> masterCurrencies;

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
            masterCurrencies = new List<CurrencyModel>();
            masterCurrencies.Add(new CurrencyModel { CurrencyCode = "THB", CurrencyName = "Thai Baht", IsActive = true });
            masterCurrencies.Add(new CurrencyModel { CurrencyCode = "USD", CurrencyName = "United States Dollar", IsActive = true });
            masterCurrencies.Add(new CurrencyModel { CurrencyCode = "SGD", CurrencyName = "Singapore Dollar", IsActive = false });
            return View(masterCurrencies);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
