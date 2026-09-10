using CurrecyTracking.Interfaces;
using CurrecyTracking.Models;
using System.ComponentModel.DataAnnotations;

namespace CurrecyTracking.Services;

public class CurrencyMasterService : ICurrencyMaster
{
    private readonly object _currencyLock = new();
    private readonly List<CurrencyModel> _currencies = new()
    {
        new() { CurrencyCode = "THB", CurrencyName = "Thai Baht", IsActive = true },
        new() { CurrencyCode = "USD", CurrencyName = "United States Dollar", IsActive = true },
        new() { CurrencyCode = "SGD", CurrencyName = "Singapore Dollar", IsActive = false }
    };

    public CurrencyModel? GetCurrency(string currencyId)
    {
        lock (_currencyLock)
        {
            var currency = _currencies.Find(c => c.CurrencyCode.Equals(currencyId, StringComparison.OrdinalIgnoreCase));
            return currency is null ? null : Copy(currency);
        }
    }

    public List<CurrencyModel> GetCurrencies()
    {
        lock (_currencyLock)
        {
            return _currencies.Select(Copy).ToList();
        }
    }

    // Returns false when the code already exists.
    public bool AddCurrency(CurrencyModel currency)
    {
        var normalized = Normalize(currency);
        lock (_currencyLock)
        {
            if (_currencies.Any(c => c.CurrencyCode == normalized.CurrencyCode)) return false;
            _currencies.Add(normalized);
            return true;
        }
    }

    // Returns false when the currency no longer exists.
    public bool UpdateCurrency(CurrencyModel currency)
    {
        var normalized = Normalize(currency);
        lock (_currencyLock)
        {
            var existing = _currencies.Find(c => c.CurrencyCode == normalized.CurrencyCode);
            if (existing is null) return false;
            existing.CurrencyName = normalized.CurrencyName;
            existing.IsActive = normalized.IsActive;
            return true;
        }
    }

    public bool DeleteCurrency(string currencyId)
    {
        lock (_currencyLock)
        {
            return _currencies.RemoveAll(c => c.CurrencyCode.Equals(currencyId, StringComparison.OrdinalIgnoreCase)) > 0;
        }
    }

    private static CurrencyModel Normalize(CurrencyModel currency)
    {
        ArgumentNullException.ThrowIfNull(currency);
        Validator.ValidateObject(currency, new ValidationContext(currency), validateAllProperties: true);
        return new CurrencyModel
        {
            CurrencyCode = currency.CurrencyCode.ToUpperInvariant(),
            CurrencyName = currency.CurrencyName.Trim(),
            IsActive = currency.IsActive
        };
    }

    private static CurrencyModel Copy(CurrencyModel currency) => new()
    {
        CurrencyCode = currency.CurrencyCode,
        CurrencyName = currency.CurrencyName,
        IsActive = currency.IsActive
    };
}
