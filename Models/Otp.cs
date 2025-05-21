using System.ComponentModel.DataAnnotations;

namespace StudentERP.Models
{
    public partial class Otp
    {
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string StudentMail { get; set; } = null!;

        [Required]
        [StringLength(6)]
        public string OtpCode { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public bool IsUsed { get; set; }

        public virtual StudentLogin StudentMailNavigation { get; set; } = null!;
    }
}