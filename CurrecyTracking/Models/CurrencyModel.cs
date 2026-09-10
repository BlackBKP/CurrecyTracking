using System.Text.Json.Serialization;

namespace CurrecyTracking.Models
{
    public class CurrencyModel
    {
        [JsonPropertyName("currency_code")]
        public string CurrencyCode { get; set; }

        [JsonPropertyName("currency_name")]
        public string CurrencyName { get; set; }

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }
    }
}
