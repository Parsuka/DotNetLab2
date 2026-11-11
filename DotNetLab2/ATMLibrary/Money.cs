using System.Globalization;

namespace ATMLibrary
{
    /// <summary>Форматування та розбір грошових сум.</summary>
    public static class Money
    {
        private static readonly CultureInfo Culture = CultureInfo.GetCultureInfo("uk-UA");

        public static string Format(decimal amount)
        {
            return amount.ToString("N2", Culture) + " грн";
        }

        /// <summary>Розбирає суму, приймаючи і крапку, і кому як десятковий роздільник.</summary>
        public static bool TryParse(string text, out decimal amount)
        {
            amount = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;
            string normalized = text.Trim().Replace(" ", "").Replace(",", ".");
            return decimal.TryParse(normalized, NumberStyles.Number, CultureInfo.InvariantCulture, out amount);
        }
    }
}
