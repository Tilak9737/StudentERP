namespace StudentERP.Models;

public partial class Student
{
    public int StudentId { get; set; }

    public string FirstName { get; set; } = null!;

    public string LastName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public DateOnly? EnrollmentDate { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
