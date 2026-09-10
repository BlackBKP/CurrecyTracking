using System.Text.Json.Serialization;

namespace CurrecyTracking.Models;

public class FxRateModel
{
    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("rate")]
    public decimal? Rate { get; set; }

    [JsonPropertyName("date")]
    public DateOnly? Date { get; set; }

    [JsonPropertyName("baseline")]
    public decimal? Baseline { get; set; }

    [JsonPropertyName("baseline_date")]
    public DateOnly? BaselineDate { get; set; }

    [JsonPropertyName("change_percent")]
    public decimal? ChangePercent { get; set; }

    [JsonPropertyName("exceeds_tolerence")]
    public bool ExceedsTolerance => ChangePercent.HasValue && Math.Abs(ChangePercent.Value) > 1m;

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
