namespace ProducScan.Helpers
{
    public static class ProduccionHelper
    {
        // Definición de turnos
        private static readonly List<TurnoDefinicion> Turnos = new()
    {
        new TurnoDefinicion { Nombre = "Turno 1", HoraInicio = new TimeSpan(7, 0, 0), HoraFin = new TimeSpan(15, 44, 59), OffsetDia = 0 },
        new TurnoDefinicion { Nombre = "Turno 2", HoraInicio = new TimeSpan(15, 45, 0), HoraFin = new TimeSpan(23, 49, 59), OffsetDia = 0 },
        new TurnoDefinicion { Nombre = "Turno 3", HoraInicio = new TimeSpan(23, 50, 0), HoraFin = new TimeSpan(6, 59, 59), OffsetDia = 1 }
    };

        public static DateTime GetFechaProduccion(DateTime fechaEvento)
        {
            var hora = fechaEvento.TimeOfDay;
            var turno = ObtenerTurno(hora);

            // Fecha base
            DateTime fechaProduccion = fechaEvento.Date;

            // Si es turno con OffsetDia = 1 (ej. Turno 3), retrocede un día
            if (turno.OffsetDia == 1 && hora < turno.HoraFin)
            {
                fechaProduccion = fechaProduccion.AddDays(-1);
            }

            return fechaProduccion;
        }

        private static TurnoDefinicion ObtenerTurno(TimeSpan hora)
        {
            foreach (var t in Turnos)
            {
                if (t.HoraInicio < t.HoraFin)
                {
                    if (hora >= t.HoraInicio && hora < t.HoraFin)
                        return t;
                }
                else
                {
                    if (hora >= t.HoraInicio || hora < t.HoraFin)
                        return t;
                }
            }

            // fallback
            return new TurnoDefinicion { Nombre = "Desconocido", HoraInicio = TimeSpan.Zero, HoraFin = TimeSpan.Zero, OffsetDia = 0 };
        }


        private class TurnoDefinicion
        {
            public string Nombre { get; set; }
            public TimeSpan HoraInicio { get; set; }
            public TimeSpan HoraFin { get; set; }
            public int OffsetDia { get; set; }
        }
    }
}
