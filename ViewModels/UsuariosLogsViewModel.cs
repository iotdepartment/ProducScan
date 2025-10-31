// Models/ViewModels/UsuariosLogsViewModel.cs
using System.Collections.Generic;

namespace ProducScan.Models.ViewModels
{
    public class UsuariosLogsViewModel
    {
        public IEnumerable<Usuario> Usuarios { get; set; }
        public IEnumerable<Log> Logs { get; set; }
    }
}