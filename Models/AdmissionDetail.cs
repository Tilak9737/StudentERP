using System;
using System.Collections.Generic;

namespace StudentERP.Models;

public partial class AdmissionDetail
{
    public Guid StudentId { get; set; }

    public string? AdmissionNumber { get; set; }

    public DateOnly? AdmissionDate { get; set; }

    public string? Course { get; set; }

    public virtual StudentLogin Student { get; set; } = null!;
}
