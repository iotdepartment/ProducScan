namespace ProducScan.Services
{
    public interface ILogService
    {
        void Registrar(string accion, string detalles, string nivel = "Info", string usuario = null, string ip = null, string categoria = "Sistema");
    }
}