using StudentERP.Controllers;
using StudentERP.Models;
using System;
using System.Threading.Tasks;

namespace StudentERP.Repository.IRepository
{
    public interface IStudentLoginRepository
    {
        Task<bool> RegisterStudent(StudentLogin studentLogin);
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
        Task SendLoginNotificationEmail(string email, bool isSuccess);

    }
}