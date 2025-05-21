namespace StudentERP.Models;

public partial class StudentLogin
{
    public Guid StudentId { get; set; }

    public string StudentMail { get; set; } = null!;

    public string HashPassword { get; set; } = null!;

    public Guid Token { get; set; }

    public bool IsActive { get; set; }

    public virtual AdmissionDetail? AdmissionDetail { get; set; }

    public virtual ContactDetail? ContactDetail { get; set; }

    public virtual ICollection<Otp> Otps { get; set; } = new List<Otp>();

    public virtual ParentsDetail? ParentsDetail { get; set; }

    public virtual PersonalDetail? PersonalDetail { get; set; }

    public virtual StudentBatch? StudentBatch { get; set; }

    public virtual StudentProfile? StudentProfile { get; set; }
}
