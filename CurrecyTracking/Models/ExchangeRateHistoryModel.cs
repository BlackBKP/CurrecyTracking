using System.Text.Json.Serialization;

namespace CurrecyTracking.Models;

public class ExchangeRateHistoryModel
{
    [JsonPropertyName("base")]
    public string Base { get; set; } = string.Empty;

    [JsonPropertyName("start_date")]
    public DateOnly StartDate { get; set; }

    [JsonPropertyName("end_date")]
    public DateOnly EndDate { get; set; }

    [JsonPropertyName("rates")]
    public Dictionary<DateOnly, Dictionary<string, decimal>> Rates { get; set; } = new();
}
