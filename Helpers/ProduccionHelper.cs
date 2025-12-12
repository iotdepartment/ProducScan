namespace ProducScan.Helpers
{
    public static class ProduccionHelper
    {
        public static DateTime GetFechaProduccion(DateTime fechaEvento)
        {
            TimeSpan hora = fechaEvento.TimeOfDay;

            // ✅ Turno 3: 23:50 → 23:59 (día actual)
            if (hora >= new TimeSpan(23, 50, 0) && hora <= new TimeSpan(23, 59, 59))
            {
                return fechaEvento.Date;
            }

            // ✅ Turno 3: 00:00 → 07:09:59 (día anterior)
            if (hora >= TimeSpan.Zero && hora <= new TimeSpan(7, 9, 59))
            {
                return fechaEvento.Date.AddDays(-1);
            }

            // ✅ Turno 1 y 2: 07:10 → 23:49:59 (día actual)
            if (hora >= new TimeSpan(7, 10, 0) && hora <= new TimeSpan(23, 49, 59))
            {
                return fechaEvento.Date;
            }

            // ✅ Si cae fuera de rangos (no debería pasar), usar día actual
            return fechaEvento.Date;
        }
    }
}
