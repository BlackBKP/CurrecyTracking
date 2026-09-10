using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace CurrecyTracking.Models
{
    public class CurrencyModel
    {
        [JsonPropertyName("currency_code")]
        [Required]
        [RegularExpression("^[A-Za-z]{3}$", ErrorMessage = "Code must contain exactly three letters.")]
        public string CurrencyCode { get; set; } = string.Empty;

        [JsonPropertyName("currency_name")]
        [Required]
        [StringLength(100)]
        public string CurrencyName { get; set; } = string.Empty;

        [JsonPropertyName("is_active")]
        public bool IsActive { get; set; }
    }
}
