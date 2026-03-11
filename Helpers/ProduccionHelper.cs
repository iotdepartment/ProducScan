namespace ProducScan.Helpers
{
    public static class ProduccionHelper
    {
        // Hora local de Matamoros con reglas personalizadas
        public static DateTime GetMatamorosTime()
        {
            DateTime utcNow = DateTime.UtcNow;
            TimeSpan offset = GetMatamorosOffset(utcNow);
            return utcNow.Add(offset);
        }

        // Determina la fecha de producción según la hora
        public static DateTime GetFechaProduccion(DateTime fechaEvento)
        {
            TimeSpan hora = fechaEvento.TimeOfDay;

            // Turno 3: 23:50 → 23:59 (día actual)
            if (hora >= new TimeSpan(23, 50, 0) && hora <= new TimeSpan(23, 59, 59))
                return fechaEvento.Date;

            // Turno 3: 00:00 → 07:09:59 (día anterior)
            if (hora >= TimeSpan.Zero && hora <= new TimeSpan(7, 9, 59))
                return fechaEvento.Date.AddDays(-1);

            // Turno 1 y 2: 07:10 → 23:49:59 (día actual)
            if (hora >= new TimeSpan(7, 10, 0) && hora <= new TimeSpan(23, 49, 59))
                return fechaEvento.Date;

            return fechaEvento.Date;
        }

        // Determina el turno según la hora
        public static string GetTurno(DateTime fechaLocal)
        {
            var horaActual = fechaLocal.TimeOfDay;

            if (horaActual >= new TimeSpan(7, 0, 0) && horaActual <= new TimeSpan(15, 44, 59))
                return "1";
            else if (horaActual >= new TimeSpan(15, 45, 0) && horaActual <= new TimeSpan(23, 49, 59))
                return "2";
            else
                return "3";
        }

        // Reglas de horario de verano en Matamoros
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