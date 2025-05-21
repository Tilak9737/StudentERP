using StudentERP.Controllers;
using StudentERP.Models;

namespace StudentERP.Repository.IRepository
{
    public interface IStudentLoginRepository
    {
        Task<(bool Success, string Message)> RegisterStudentAsync(StudentLogin studentLogin, string fullName);
        Task<StudentLogin> GetStudentByEmail(string email);
        string HashPassword(string password);
        Task<bool> UpdateStudent(StudentLogin studentLogin);
        Task<StudentProfile?> GetStudentProfile(Guid studentId);
        Task<bool> SaveOrReplaceStudentProfile(StudentProfile profile);
        Task<PersonalDetail?> GetPersonalDetails(Guid studentId);
        Task<ContactDetail?> GetContactDetails(Guid studentId);
        Task<ParentsDetail?> GetParentsDetails(Guid studentId);
        Task<StudentBatch?> GetStudentBatch(Guid studentId);
        Task<DegreeName?> GetDegreeName(string did);
        Task<FieldName?> GetFieldName(string fid);
        Task<List<SubjectInfo>> GetSubjectsForSemester(string semId);
        Task<(bool Success, bool RequiresOtp, string Message, string Token, Guid StudentId)> ValidateLoginAsync(StudentLogin model, string action, string otpCode);
        Task<(bool Success, string Message)> ChangePasswordAsync(string email, string currentPassword, string currentPasswordConfirm, string newPassword);
        Task<(bool Exists, string FileName, string Message)> CheckProfilePictureAsync(string email);
        Task<(bool Success, bool ConfirmRequired, string Message)> UpdateStudentProfileAsync(string email, IFormFile profilePicture, bool replaceConfirmed);
        Task<bool> GenerateAndStoreOtp(string email);
        Task<bool> ValidateOtp(string email, string otpCode);
        Task DeleteExpiredOtps();
        Task<List<string>> GetAvailableSemesters(string did, string fid);
        Task<bool> IsValidSemester(string did, string fid, string semId);
    }
}