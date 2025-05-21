namespace StudentERP.Models;

public partial class StudentProfile
{
    public Guid StudentId { get; set; }

    public string ProfilePictureName { get; set; } = null!;

    public virtual StudentLogin Student { get; set; } = null!;
}
