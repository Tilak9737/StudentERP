using System;
using System.Collections.Generic;

namespace StudentERP.Models;

public partial class ParentsDetail
{
    public Guid StudentId { get; set; }

    public string? FatherName { get; set; }

    public string? MotherName { get; set; }

    public string? ParentPhoneNumber { get; set; }

    public virtual StudentLogin Student { get; set; } = null!;
}
