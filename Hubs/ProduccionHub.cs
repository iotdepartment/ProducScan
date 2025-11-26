using Microsoft.AspNetCore.SignalR;
using ProducScan.ViewModels;

public class ProduccionHub : Hub
{
    // Método que el servidor puede invocar para mandar datos a los clientes
    public async Task EnviarActualizacion(InspeccionTMViewModel data)
    {
        await Clients.All.SendAsync("RecibirActualizacion", data);
    }
}
