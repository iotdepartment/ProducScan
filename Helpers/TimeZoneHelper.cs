using System;

namespace ProducScan.Helpers
{
    public static class TimeZoneHelper
    {
        // Obtiene la hora correcta de Matamoros aplicando reglas personalizadas
        public static DateTime GetMatamorosTime()
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeSpan offset = GetMatamorosOffset(utcNow);
            return utcNow.Add(offset);
        }

        // Determina el turno según la hora
        public static string GetTurno(DateTime fechaLocal)
        {
            var horaActual = fechaLocal.TimeOfDay;

            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual <= new TimeSpan(15, 44, 59))
                return "Turno 1";
            else if (horaActual >= new TimeSpan(15, 45, 0) && horaActual <= new TimeSpan(23, 49, 59))
                return "Turno 2";
            else
                return "Turno 3";
        }

        // Reglas de horario de verano en Matamoros:
        // - Inicia: segundo domingo de marzo (UTC-5)
        // - Termina: primer domingo de noviembre (UTC-6)
        private static TimeSpan GetMatamorosOffset(DateTime date)
        {
            int year = date.Year;

            DateTime startDST = GetSecondSundayOfMarch(year);
            DateTime endDST = GetFirstSundayOfNovember(year);

            if (date >= startDST && date < endDST)
                return TimeSpan.FromHours(-5); // Horario de verano
            else
                return TimeSpan.FromHours(-6); // Horario estándar
        }

        private static DateTime GetSecondSundayOfMarch(int year)
        {
            DateTime marchFirst = new DateTime(year, 3, 1);
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)marchFirst.DayOfWeek + 7) % 7;
            DateTime firstSunday = marchFirst.AddDays(daysUntilSunday);
            return firstSunday.AddDays(7); // segundo domingo
        }

        private static DateTime GetFirstSundayOfNovember(int year)
        {
            DateTime novemberFirst = new DateTime(year, 11, 1);
            int daysUntilSunday = ((int)DayOfWeek.Sunday - (int)novemberFirst.DayOfWeek + 7) % 7;
            return novemberFirst.AddDays(daysUntilSunday); // primer domingo
        }
    }
}