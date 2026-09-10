using CurrecyTracking.Models;

namespace CurrecyTracking.Interfaces;

public interface ICurrencyMaster
{
    CurrencyModel? GetCurrency(string currencyId);
    List<CurrencyModel> GetCurrencies();
    bool AddCurrency(CurrencyModel currency);
    bool UpdateCurrency(CurrencyModel currency);
    bool DeleteCurrency(string currencyId);
}
