using ProducScan.ViewModels;

public class ProduccionFiltroViewModel
{
    public int? Año { get; set; }
    public int? Mes { get; set; }
    public int? Dia { get; set; }

    public DateOnly Fecha { get; set; }

    public TimeOnly Hora { get;  set; }
    public string? Turno { get; set; }
    public string? Mesa { get; set; }
    public List<ProduccionPorMesaViewModel> Reporte { get; set; } = new();
}