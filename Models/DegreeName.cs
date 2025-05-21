namespace StudentERP.Models;

public partial class DegreeName
{
    public string Did { get; set; } = null!;

    public string Dname { get; set; } = null!;

    public int ComplePeriod { get; set; }

    public virtual ICollection<FieldName> FieldNames { get; set; } = new List<FieldName>();

    public virtual ICollection<Semester> Semesters { get; set; } = new List<Semester>();

    public virtual ICollection<StudentBatch> StudentBatches { get; set; } = new List<StudentBatch>();

    public virtual ICollection<Subject> Subjects { get; set; } = new List<Subject>();
}
