namespace StudentERP.Models;

public partial class PersonalDetail
{
    public Guid StudentId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public virtual StudentLogin Student { get; set; } = null!;
}
