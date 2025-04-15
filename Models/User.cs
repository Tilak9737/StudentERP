using System;
using System.Collections.Generic;

namespace StudentERP.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Username { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string Role { get; set; } = null!;

    public int? StudentId { get; set; }

    public virtual Student? Student { get; set; }
}
