using System;
using System.Collections.Generic;

namespace StudentERP.Models;

public partial class FieldName
{
    public string Fid { get; set; } = null!;

    public string Did { get; set; } = null!;

    public string Fname { get; set; } = null!;

    public virtual DegreeName DidNavigation { get; set; } = null!;

    public virtual ICollection<Semester> Semesters { get; set; } = new List<Semester>();

    public virtual ICollection<StudentBatch> StudentBatches { get; set; } = new List<StudentBatch>();

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
