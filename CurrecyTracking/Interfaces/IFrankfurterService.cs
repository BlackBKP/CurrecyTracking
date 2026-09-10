using CurrecyTracking.Models;

namespace CurrecyTracking.Interfaces;

public interface IFrankfurterService
{
    Task<List<FxRateModel>> GetMonitorRatesAsync(IEnumerable<string> codes, CancellationToken cancellationToken);
    Task<ExchangeRatesModel> GetLatestAsync(CancellationToken cancellationToken);
    Task<ExchangeRatesModel> GetSelectedLatestAsync(string symbols, CancellationToken cancellationToken);
    Task<Dictionary<string, string>> GetCurrenciesAsync(CancellationToken cancellationToken);
    Task<ExchangeRatesModel> GetHistoricalAsync(DateOnly date, string? symbols, CancellationToken cancellationToken);
    Task<ExchangeRateHistoryModel> GetHistoryAsync(DateOnly start, DateOnly end, string? symbols, CancellationToken cancellationToken);
}
