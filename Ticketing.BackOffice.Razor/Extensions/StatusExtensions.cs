using Ticketing.Core.Models;
using System.Globalization;

namespace Ticketing.BackOffice.Razor.Extensions
{
    public static class StatusExtensions
    {
        public static string ToFrenchString(this ReservationStatus status)
        {
            return status switch
            {
                ReservationStatus.Pending => "En attente",
                ReservationStatus.Confirmed => "Confirmé",
                ReservationStatus.Cancelled => "Annulé",
                _ => status.ToString()
            };
        }

        public static string ToFrenchString(this SeatStatus status)
        {
            return status switch
            {
                SeatStatus.Free => "Libre",
                SeatStatus.Held => "Retenu",
                SeatStatus.Reserved => "Commandé",
                SeatStatus.Taken => "Payé",
                _ => status.ToString()
            };
        }

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