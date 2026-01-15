using System.Globalization;

namespace Ticketing.FrontOffice.Mvc.Extensions
{
    public static class DateExtensions
    {
        public static string ToFrenchDateString(this DateTime date, string format = "dd MMMM yyyy à HH:mm")
        {
            var culture = new CultureInfo("fr-FR");
            return date.ToString(format, culture);
        }

        public static string ToFrenchDateString(this DateTime? date, string format = "dd MMMM yyyy à HH:mm")
        {
            if (!date.HasValue) return string.Empty;
            return date.Value.ToFrenchDateString(format);
        }
    }
}

