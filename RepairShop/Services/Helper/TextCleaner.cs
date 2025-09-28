using System.Text.RegularExpressions;

namespace RepairShop.Services.Helper
{
    public class TextCleaner
    {
        public static string CleanText(string rawHtml)
        {
            // Use Regex to replace all HTML tags with an empty string
            // This pattern matches anything between < and >
            string cleanText = Regex.Replace(rawHtml, "<[^>]*>", string.Empty);

            // Clean up extra line breaks left behind by the tags (optional)
            cleanText = cleanText.Replace("\r", "").Replace("\n", "").Trim();

            return cleanText;
        }
    }
}
