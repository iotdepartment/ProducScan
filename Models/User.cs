using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProducScan.Models;


[Table("User")]
public partial class User
{
    [Key]
    public string? NumerodeEmpleado { get; set; }

    public string? Nombre { get; set; }

    public int? Id { get; set; }
}
