using System;

namespace ProducScan.Helpers
{
    public static class TimeZoneHelper
    {
        // Obtiene la hora local del sistema con offset
        public static DateTimeOffset GetLocalTime()
        {
            return DateTimeOffset.Now;
        }

        // Determina el turno según la hora
        public static string GetTurno(DateTimeOffset fechaLocal)
        {
            var horaActual = fechaLocal.TimeOfDay;

            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual <= new TimeSpan(15, 44, 59))
                return "Turno 1";
            else if (horaActual >= new TimeSpan(15, 45, 0) && horaActual <= new TimeSpan(23, 49, 59))
                return "Turno 2";
            else
                return "Turno 3";
        }
    }
}