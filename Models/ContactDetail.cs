namespace StudentERP.Models;

public partial class ContactDetail
{
    public Guid StudentId { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? City { get; set; }

    public string? State { get; set; }

    public string? ZipCode { get; set; }

    public virtual StudentLogin Student { get; set; } = null!;
}
