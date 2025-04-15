using System;
using System.Collections.Generic;

namespace StudentERP.Models;

public partial class Subject
{
    public int SubjectId { get; set; }

    public string Did { get; set; } = null!;

    public string Fid { get; set; } = null!;

    public string SemId { get; set; } = null!;

    public string SubjectName { get; set; } = null!;

    public string SubjectCode { get; set; } = null!;

    public string? SyllabusFileName { get; set; }

    public int SubjectCredit { get; set; }

    public virtual DegreeName DidNavigation { get; set; } = null!;

    public virtual FieldName FidNavigation { get; set; } = null!;

    public virtual Semester Sem { get; set; } = null!;
}
