using System.Diagnostics;
using CurrecyTracking.Interfaces;
using CurrecyTracking.Models;
using Microsoft.AspNetCore.Mvc;

namespace CurrecyTracking.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IFrankfurterService _exchangeRates;
        private readonly ICurrencyMaster _currencyMaster;

        public HomeController(ILogger<HomeController> logger, IFrankfurterService exchangeRates, ICurrencyMaster currencyMaster)
        {
            _logger = logger;
            _exchangeRates = exchangeRates;
            _currencyMaster = currencyMaster;
        }

        public IActionResult Index()
        {
            return View(_currencyMaster.GetCurrencies().Where(c => c.IsActive).ToList());
        }

        [HttpGet]
        public IActionResult Historical()
        {
            return View();
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> LatestRates(string? currencyCode, CancellationToken cancellationToken)
        {
            var currencies = _currencyMaster.GetCurrencies().Where(c => c.IsActive).ToList();
            if (!string.IsNullOrWhiteSpace(currencyCode))
            {
                currencies = currencies.Where(c => c.CurrencyCode.Equals(currencyCode, StringComparison.OrdinalIgnoreCase)).ToList();
                if (currencies.Count == 0)
                    return NotFound(new { message = "This currency is no longer active. Reload the page." });
            }
            if (currencies.Count == 0)
                return Json(Array.Empty<object>());
            try
            {
                return Json(await _exchangeRates.GetMonitorRatesAsync(currencies.Select(c => c.CurrencyCode), cancellationToken));
            }
            catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException ||
                ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Unable to fetch Frankfurter exchange rates.");
                return StatusCode(StatusCodes.Status503ServiceUnavailable,
                    new { message = "Exchange rates are temporarily unavailable. Please try refreshing again." });
            }
        }

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public Task<IActionResult> AllRates(CancellationToken cancellationToken) =>
            ApiResult(() => _exchangeRates.GetLatestAsync(cancellationToken), cancellationToken);

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public Task<IActionResult> SelectedRates(string symbols, CancellationToken cancellationToken) =>
            ApiResult(() => _exchangeRates.GetSelectedLatestAsync(symbols, cancellationToken), cancellationToken);

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public Task<IActionResult> SupportedCurrencies(CancellationToken cancellationToken) =>
            ApiResult(() => _exchangeRates.GetCurrenciesAsync(cancellationToken), cancellationToken);

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public Task<IActionResult> HistoricalRates(DateOnly date, string? symbols, CancellationToken cancellationToken) =>
            ApiResult(() => _exchangeRates.GetHistoricalAsync(date, symbols, cancellationToken), cancellationToken);

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public Task<IActionResult> RateHistory(DateOnly start, DateOnly end, string? symbols, CancellationToken cancellationToken) =>
            ApiResult(() => _exchangeRates.GetHistoryAsync(start, end, symbols, cancellationToken), cancellationToken);

        private async Task<IActionResult> ApiResult<T>(Func<Task<T>> fetch, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { message = "Provide valid dates and currency codes." });
            try
            {
                return Json(await fetch());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.NotFound or
                System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.UnprocessableEntity)
            {
                return BadRequest(new { message = "No rates are available for the requested currencies or dates." });
            }
            catch (Exception ex) when (ex is HttpRequestException or System.Text.Json.JsonException ||
                ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Unable to fetch Frankfurter data.");
                return StatusCode(503, new { message = "Frankfurter is temporarily unavailable. Please try again." });
            }
        }

        // Master Currency
        public IActionResult CurrencyMaster()
        {
            return View(_currencyMaster.GetCurrencies());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddCurrency(CurrencyModel currency)
        {
            if (!ModelState.IsValid)
                return View("CurrencyMaster", _currencyMaster.GetCurrencies());

            if (!_currencyMaster.AddCurrency(currency))
            {
                ModelState.AddModelError(nameof(currency.CurrencyCode), "A currency with this code already exists.");
                return View("CurrencyMaster", _currencyMaster.GetCurrencies());
            }
            TempData["Success"] = "Currency added.";
            return RedirectToAction(nameof(CurrencyMaster));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditCurrency(CurrencyModel currency)
        {
            if (!ModelState.IsValid)
                return View("CurrencyMaster", _currencyMaster.GetCurrencies());
            if (!_currencyMaster.UpdateCurrency(currency))
                return NotFound();
            TempData["Success"] = "Currency updated.";
            return RedirectToAction(nameof(CurrencyMaster));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCurrency(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                return BadRequest();
            if (!_currencyMaster.DeleteCurrency(currencyCode))
                return NotFound();
            TempData["Success"] = "Currency deleted.";
            return RedirectToAction(nameof(CurrencyMaster));
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
