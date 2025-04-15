using System;
using System.Collections.Generic;

namespace StudentERP.Models;

public partial class StudentBatch
{
    public Guid StudentId { get; set; }

    public string Did { get; set; } = null!;

    public string Fid { get; set; } = null!;

    public string CurrentSem { get; set; } = null!;

    public virtual Semester CurrentSemNavigation { get; set; } = null!;

    public virtual DegreeName DidNavigation { get; set; } = null!;

    public virtual FieldName FidNavigation { get; set; } = null!;

    public virtual StudentLogin Student { get; set; } = null!;
}
