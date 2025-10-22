using System;
using System.Collections.Generic;

namespace Financa.Data.Models;

public partial class User
{
    public int IdUsuario { get; set; }

    public string Email { get; set; }

    public string Senha { get; set; }
}
