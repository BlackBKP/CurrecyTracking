using CurrecyTracking.Interfaces;
using CurrecyTracking.Models;
using System.Globalization;
using System.Text.Json;

namespace CurrecyTracking.Services;

public class FrankfurterService : IFrankfurterService
{
    private readonly HttpClient _client;

    public FrankfurterService(HttpClient client)
    {
        _client = client;
    }

    public async Task<ExchangeRatesModel> GetLatestAsync(CancellationToken cancellationToken)
    {
        var rates = await _client.GetFromJsonAsync<ExchangeRatesModel>("latest?base=THB", cancellationToken);
        return rates ?? throw new JsonException("No exchange rates returned.");
    }

    public async Task<List<FxRateModel>> GetMonitorRatesAsync(IEnumerable<string> codes, CancellationToken cancellationToken)
    {
        var latest = await GetLatestAsync(cancellationToken);
        if (latest.Base != "THB" || latest.Date == default || latest.Rates is null)
            throw new JsonException("Invalid latest rates response.");

        var results = new List<FxRateModel>();
        foreach (var code in codes.Distinct())
        {
            var row = new FxRateModel { Code = code };
            if (code == "THB")
            {
                row.Rate = 1m;
                row.Date = latest.Date;
                row.Message = "Base currency — tolerance check not applicable.";
            }
            else if (latest.Rates.TryGetValue(code, out var rate) && rate > 0)
            {
                row.Rate = rate;
                row.Date = latest.Date;
            }
            else
            {
                row.Message = "Currency is unsupported or its rate is unavailable.";
            }
            results.Add(row);
        }

        var targets = results.Where(r => r.Rate.HasValue && r.Code != "THB").ToList();
        if (targets.Count == 0) return results;

        ExchangeRateHistoryModel history;
        try
        {
            // Anchor to the published rate date, even on weekends or before today's update.
            history = await GetHistoryAsync(latest.Date.AddDays(-7), latest.Date,
                string.Join(',', targets.Select(r => r.Code)), cancellationToken);
            if (history.Base != "THB" || history.Rates is null)
                throw new JsonException("Invalid history response.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException ||
            ex is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            foreach (var row in targets)
                row.Message = "Latest rate loaded, but history is unavailable. Tolerance was not checked. Please refresh again.";
            return results;
        }

        foreach (var row in targets)
        {
            foreach (var day in history.Rates.OrderByDescending(r => r.Key))
            {
                if (day.Key >= latest.Date || day.Key < latest.Date.AddDays(-7)) continue;
                if (day.Value is null || !day.Value.TryGetValue(row.Code, out var baseline) || baseline <= 0) continue;
                row.Baseline = baseline;
                row.BaselineDate = day.Key;
                row.ChangePercent = (row.Rate!.Value - baseline) / baseline * 100m;
                break;
            }
            if (!row.Baseline.HasValue)
                row.Message = "No previous working-day rate found within seven days. Tolerance was not checked.";
        }
        return results;
    }

    public async Task<ExchangeRatesModel> GetSelectedLatestAsync(string symbols, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(symbols))
            throw new ArgumentException("Specify at least one currency code.");

        var url = "latest?base=THB" + SymbolsQuery(symbols);
        var rates = await _client.GetFromJsonAsync<ExchangeRatesModel>(url, cancellationToken);
        return rates ?? throw new JsonException("No exchange rates returned.");
    }

    public async Task<Dictionary<string, string>> GetCurrenciesAsync(CancellationToken cancellationToken)
    {
        var currencies = await _client.GetFromJsonAsync<Dictionary<string, string>>("currencies", cancellationToken);
        return currencies ?? throw new JsonException("No currencies returned.");
    }

    public async Task<ExchangeRatesModel> GetHistoricalAsync(DateOnly date, string? symbols, CancellationToken cancellationToken)
    {
        ValidateDate(date);
        var dateText = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var url = dateText + "?base=THB" + SymbolsQuery(symbols);
        var rates = await _client.GetFromJsonAsync<ExchangeRatesModel>(url, cancellationToken);
        return rates ?? throw new JsonException("No historical rates returned.");
    }

    public async Task<ExchangeRateHistoryModel> GetHistoryAsync(DateOnly start, DateOnly end, string? symbols, CancellationToken cancellationToken)
    {
        ValidateDate(start);
        ValidateDate(end);
        if (start > end)
            throw new ArgumentException("Start date must not be after end date.");

        var startText = start.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var endText = end.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var url = startText + ".." + endText + "?base=THB" + SymbolsQuery(symbols);
        var history = await _client.GetFromJsonAsync<ExchangeRateHistoryModel>(url, cancellationToken);
        return history ?? throw new JsonException("No rate history returned.");
    }

    private static string SymbolsQuery(string? symbols)
    {
        if (string.IsNullOrWhiteSpace(symbols))
            return string.Empty;

        var codes = symbols.ToUpperInvariant().Split(',');
        for (var i = 0; i < codes.Length; i++)
        {
            codes[i] = codes[i].Trim();
            if (codes[i].Length != 3 || codes[i].Any(letter => letter < 'A' || letter > 'Z'))
                throw new ArgumentException("Use three-letter currency codes separated by commas, such as USD,EUR,JPY.");

            if (codes[i] == "THB")
                throw new ArgumentException("THB is the base currency. Select other currencies for the symbols filter.");
        }

        return "&symbols=" + Uri.EscapeDataString(string.Join(',', codes));
    }

    private static void ValidateDate(DateOnly date)
    {
        if (date == default || date > DateOnly.FromDateTime(DateTime.UtcNow))
            throw new ArgumentException("Provide a valid date that is not in the future.");
    }
}
