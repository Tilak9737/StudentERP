using Microsoft.EntityFrameworkCore;
using StudentERP.Models;
using StudentERP.Repository.IRepository;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using StudentERP.Controllers;
using System.Net.Mail;
using System.Net;
using Microsoft.Extensions.Options;

namespace StudentERP.Repository
{
    public class StudentLoginRepository : IStudentLoginRepository
    {
        private readonly StudentErpContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly SmtpSettings _smtpSettings;


        public StudentLoginRepository(StudentErpContext context, IWebHostEnvironment environment, IOptions<SmtpSettings> smtpSettings)
        {
            _context = context;
            _environment = environment;
            _smtpSettings = smtpSettings.Value;

        }

        public async Task<bool> RegisterStudent(StudentLogin studentLogin)
        {
            try
            {
                studentLogin.HashPassword = HashPassword(studentLogin.HashPassword);
                //studentLogin.Token = string.Empty;
                studentLogin.IsActive = true;
                studentLogin.StudentId = Guid.NewGuid();

                _context.StudentLogins.Add(studentLogin);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RegisterStudent - Error: {ex.Message}");
                return false;
            }
        }

        public async Task<StudentLogin> GetStudentByEmail(string email)
        {
            return await _context.StudentLogins.FirstOrDefaultAsync(s => s.StudentMail == email);
        }

        public string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public async Task<bool> UpdateStudent(StudentLogin studentLogin)
        {
            try
            {
                _context.StudentLogins.Update(studentLogin);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateStudent - Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> SaveOrReplaceStudentProfile(StudentProfile profile)
        {
            try
            {
                var existing = await GetStudentProfile(profile.StudentId);
                if (existing != null)
                {
                    _context.StudentProfiles.Remove(existing);
                    await _context.SaveChangesAsync();
                }
                _context.StudentProfiles.Add(profile);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SaveOrReplaceStudentProfile - Error: {ex.Message}");
                return false;
            }
        }

        public async Task<StudentProfile?> GetStudentProfile(Guid studentId)
        {
            return await _context.StudentProfiles.FirstOrDefaultAsync(p => p.StudentId == studentId);
        }

        public async Task<PersonalDetail?> GetPersonalDetails(Guid studentId)
        {
            try
            {
                return await _context.PersonalDetails
                    .FirstOrDefaultAsync(pd => pd.StudentId == studentId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetPersonalDetails - Error: {ex.Message}");
                return null;
            }
        }

        public async Task<ContactDetail?> GetContactDetails(Guid studentId)
        {
            try
            {
                return await _context.ContactDetails
                    .FirstOrDefaultAsync(cd => cd.StudentId == studentId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetContactDetails - Error: {ex.Message}");
                return null;
            }
        }

        public async Task<ParentsDetail?> GetParentsDetails(Guid studentId)
        {
            try
            {
                return await _context.ParentsDetails
                    .FirstOrDefaultAsync(pd => pd.StudentId == studentId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetParentsDetails - Error: {ex.Message}");
                return null;
            }
        }
        public async Task<StudentBatch?> GetStudentBatch(Guid studentId)
        {
            return await _context.StudentBatches.FirstOrDefaultAsync(sb => sb.StudentId == studentId);
        }

        public async Task<DegreeName?> GetDegreeName(string did)
        {
            return await _context.DegreeNames.FirstOrDefaultAsync(d => d.Did == did);
        }

        public async Task<FieldName?> GetFieldName(string fid)
        {
            return await _context.FieldNames.FirstOrDefaultAsync(f => f.Fid == fid);
        }

        public async Task<List<SubjectInfo>> GetSubjectsForSemester(string semId)
        {
            return await _context.Subjects
                .Where(s => s.SemId == semId)
                .Select(s => new SubjectInfo
                {
                    SubjectName = s.SubjectName,
                    SubjectCode = s.SubjectCode,
                    SubjectCredit = s.SubjectCredit,
                    SyllabusFileName = s.SyllabusFileName
                })
                .ToListAsync();
        }
        public async Task SendLoginNotificationEmail(string email, bool isSuccess)

        {
            try
            {
                using (var smtpClient = new SmtpClient(_smtpSettings.Host, _smtpSettings.Port))
                {
                    smtpClient.Credentials = new NetworkCredential(_smtpSettings.Username, _smtpSettings.Password);
                    smtpClient.EnableSsl = _smtpSettings.EnableSSL;

                    var mailMessage = new MailMessage
                    {
                        From = new MailAddress(_smtpSettings.FromEmail),
                        Subject = isSuccess ? "Login Successful" : "Login Failed",
                        Body = isSuccess ? "You have successfully logged into your StudentERP account."
                                         : "An unsuccessful login attempt was detected on your StudentERP account.",
                        IsBodyHtml = true
                    };

                    mailMessage.To.Add(email);
                    await smtpClient.SendMailAsync(mailMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendLoginNotification - Error: {ex.Message}");
            }
        }

    }
}