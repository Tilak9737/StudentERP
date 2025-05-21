using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MimeKit;
using StudentERP.Controllers;
using StudentERP.Models;
using StudentERP.Repository.IRepository;

namespace StudentERP.Repository
{
    public class StudentLoginRepository : IStudentLoginRepository
    {
        private readonly StudentErpContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly SmtpSettings _smtpSettings;
        private readonly IConfiguration _configuration;

        public StudentLoginRepository(StudentErpContext context, IWebHostEnvironment environment, IOptions<SmtpSettings> smtpSettings, IConfiguration configuration)
        {
            _context = context;
            _environment = environment;
            _smtpSettings = smtpSettings.Value;
            _configuration = configuration;
        }

        public async Task<(bool Success, string Message)> RegisterStudentAsync(StudentLogin studentLogin, string fullName)
        {
            try
            {
                var existingStudent = await GetStudentByEmail(studentLogin.StudentMail);
                if (existingStudent != null)
                    return (false, "Email already registered");

                studentLogin.HashPassword = HashPassword(studentLogin.HashPassword);
                studentLogin.IsActive = true;
                studentLogin.StudentId = Guid.NewGuid();

                _context.StudentLogins.Add(studentLogin);
                await _context.SaveChangesAsync();

                await SendEmailAsync(studentLogin.StudentMail, fullName, "welcome");
                return (true, "Registration successful");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"RegisterStudentAsync - Error: {ex.Message}");
                return (false, "Registration failed. Please try again.");
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
                return await _context.PersonalDetails.FirstOrDefaultAsync(pd => pd.StudentId == studentId);
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
                return await _context.ContactDetails.FirstOrDefaultAsync(cd => cd.StudentId == studentId);
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
                return await _context.ParentsDetails.FirstOrDefaultAsync(pd => pd.StudentId == studentId);
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

        public async Task<(bool Success, bool RequiresOtp, string Message, string Token, Guid StudentId)> ValidateLoginAsync(StudentLogin model, string action, string otpCode)
        {
            var student = await GetStudentByEmail(model.StudentMail);
            if (student == null || student.HashPassword != HashPassword(model.HashPassword) || !student.IsActive)
            {
                await SendEmailAsync(model.StudentMail, "", "login_notification", false);
                return (false, false, "Invalid email, password, or inactive account", null, Guid.Empty);
            }

            if (action == "sendOtp")
            {
                var otpSent = await GenerateAndStoreOtp(model.StudentMail);
                return (otpSent, true, otpSent ? "OTP sent to your email" : "Failed to send OTP", null, Guid.Empty);
            }

            if (action == "verifyOtp" && !string.IsNullOrEmpty(otpCode))
            {
                var isValidOtp = await ValidateOtp(model.StudentMail, otpCode);
                if (isValidOtp)
                {
                    await SendEmailAsync(model.StudentMail, "", "login_notification", true);
                    var token = GenerateJwtToken(student);
                    return (true, false, "Login successful", token, student.StudentId);
                }
                return (false, true, "Invalid or expired OTP", null, Guid.Empty);
            }

            return (true, true, "Please request an OTP", null, Guid.Empty);
        }

        public async Task<(bool Success, string Message)> ChangePasswordAsync(string email, string currentPassword, string currentPasswordConfirm, string newPassword)
        {
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(currentPasswordConfirm) || string.IsNullOrEmpty(newPassword))
                return (false, "All fields are required.");

            if (currentPassword != currentPasswordConfirm)
                return (false, "Current passwords do not match.");

            var student = await GetStudentByEmail(email);
            if (student == null || student.HashPassword != HashPassword(currentPassword))
                return (false, "Current password is incorrect.");

            if (HashPassword(newPassword) == student.HashPassword)
                return (false, "New password must be different from the current password.");

            student.HashPassword = HashPassword(newPassword);
            var result = await UpdateStudent(student);
            return (result, result ? "Password updated successfully!" : "Failed to update password. Please try again.");
        }

        public async Task<(bool Exists, string FileName, string Message)> CheckProfilePictureAsync(string email)
        {
            var student = await GetStudentByEmail(email);
            if (student == null)
                return (false, null, "Invalid session");

            var profile = await GetStudentProfile(student.StudentId);
            if (profile != null && !string.IsNullOrEmpty(profile.ProfilePictureName))
            {
                var filePath = Path.Combine(_environment.WebRootPath, "Images", profile.ProfilePictureName);
                if (System.IO.File.Exists(filePath))
                    return (true, profile.ProfilePictureName, "Profile picture exists");
            }
            return (false, null, "No profile picture found");
        }

        public async Task<(bool Success, bool ConfirmRequired, string Message)> UpdateStudentProfileAsync(string email, IFormFile profilePicture, bool replaceConfirmed)
        {
            try
            {
                var student = await GetStudentByEmail(email);
                if (student == null)
                    return (false, false, "Invalid session.");

                if (profilePicture == null || profilePicture.Length == 0)
                    return (false, false, "Please select a file.");

                var expectedFileName = $"{student.StudentId.ToString().ToUpper()}.jpg";
                if (profilePicture.FileName != expectedFileName)
                    return (false, false, $"Invalid filename. Please rename your file to '{expectedFileName}' and try again.");

                var fileName = expectedFileName;
                var filePath = Path.Combine(_environment.WebRootPath, "Images", fileName);

                var existingProfile = await GetStudentProfile(student.StudentId);
                if (existingProfile != null && !string.IsNullOrEmpty(existingProfile.ProfilePictureName) && !replaceConfirmed)
                {
                    if (System.IO.File.Exists(filePath))
                        return (false, true, $"A profile picture ({fileName}) already exists. Do you want to replace it?");
                }

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePicture.CopyToAsync(fileStream);
                }

                var profile = new StudentProfile
                {
                    StudentId = student.StudentId,
                    ProfilePictureName = fileName
                };
                var saveResult = await SaveOrReplaceStudentProfile(profile);
                return (saveResult, false, saveResult ? "Profile picture uploaded and saved successfully!" : "Failed to save profile data.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateStudentProfileAsync - Error: {ex.Message}");
                return (false, false, $"Upload error: {ex.Message}");
            }
        }

        public async Task<bool> GenerateAndStoreOtp(string email)
        {
            try
            {
                await DeleteExpiredOtps();

                var otpCode = new Random().Next(000000, 999999).ToString("D6");
                var otp = new Otp
                {
                    StudentMail = email,
                    OtpCode = otpCode,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(150),
                    IsUsed = false
                };

                _context.Otps.Add(otp);
                await _context.SaveChangesAsync();

                await SendEmailAsync(email, "", "otp", false, otpCode);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GenerateAndStoreOtp - Error: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ValidateOtp(string email, string otpCode)
        {
            try
            {
                var otp = await _context.Otps
                    .FirstOrDefaultAsync(o => o.StudentMail == email && o.OtpCode == otpCode && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow);

                if (otp == null)
                    return false;

                otp.IsUsed = true;
                _context.Otps.Update(otp);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ValidateOtp - Error: {ex.Message}");
                return false;
            }
        }

        public async Task DeleteExpiredOtps()
        {
            try
            {
                var expiredOtps = await _context.Otps
                    .Where(o => o.ExpiresAt <= DateTime.UtcNow || o.IsUsed)
                    .ToListAsync();

                if (expiredOtps.Any())
                {
                    _context.Otps.RemoveRange(expiredOtps);
                    await _context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DeleteExpiredOtps - Error: {ex.Message}");
            }
        }

        private async Task SendEmailAsync(string toEmail, string fullName, string emailType, bool isSuccess = false, string otpCode = null)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
                message.To.Add(new MailboxAddress(fullName, toEmail));

                string subject, body;
                switch (emailType)
                {
                    case "welcome":
                        subject = "Welcome to StudentERP! Let's Get Started!";
                        body = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Arial', sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; color: #374151; }}
        .container {{ max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .header {{ background: #374151; padding: 20px; text-align: center; color: #ffffff; }}
        .header h1 {{ margin: 0; font-size: 24px; font-family: 'Permanent Marker', 'Arial', sans-serif; }}
        .content {{ padding: 30px; line-height: 1.6; }}
        .content h2 {{ color: #374151; font-size: 22px; margin-top: 0; }}
        .content p {{ font-size: 16px; margin: 10px 0; color: #374151; }}
        .footer {{ background: #f9fafb; padding: 15px; text-align: center; font-size: 14px; color: #4b5563; border-top: 1px solid #d1d5db; }}
        @media (max-width: 600px) {{
            .container {{ margin: 10px; }}
            .header h1 {{ font-size: 20px; }}
            .content {{ padding: 20px; }}
            .content h2 {{ font-size: 18px; }}
            .content p {{ font-size: 14px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to StudentERP!</h1>
        </div>
        <div class='content'>
            <h2>Hello, {toEmail},</h2>
            <p>Your journey with StudentERP starts now, and we’re thrilled to have you on board!</p>
            <p>Your account is all set, opening the door to a seamless way to manage your academic world. Dive into your personalized dashboard to explore your courses, track progress, and more.</p>
            <p>Have questions? Our team is here to help you every step of the way.</p>
            <p>Warm regards,<br>The StudentERP Team</p>
        </div>
        <div class='footer'>
            <p>© 2025 StudentERP | Contact Support</p>
        </div>
    </div>
</body>
</html>";
                        break;

                    case "login_notification":
                        var istTimeZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                        var loginTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istTimeZone).ToString("dd MMM yyyy, hh:mm tt IST");
                        subject = isSuccess ? "StudentERP: Login Successful" : "StudentERP: Login Attempt Detected";
                        body = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Arial', sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; color: #374151; }}
        .container {{ max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .header {{ background: #374151; padding: 20px; text-align: center; color: #ffffff; }}
        .header h1 {{ margin: 0; font-size: 24px; font-family: 'Permanent Marker', 'Arial', sans-serif; }}
        .content {{ padding: 30px; line-height: 1.6; }}
        .content h2 {{ color: #374151; font-size: 22px; margin-top: 0; }}
        .content p {{ font-size: 16px; margin: 10px 0; color: #374151; }}
        .details {{ margin: 20px 0; padding: 15px; background: #f9fafb; border-radius: 5px; }}
        .details p {{ margin: 5px 0; font-size: 15px; }}
        .cta-button {{ display: inline-block; padding: 12px 24px; margin: 20px 0; background: #ef4444; color: #ffffff; text-decoration: none; border-radius: 5px; font-weight: 600; font-size: 16px; }}
        .cta-button:hover {{ background: #dc2626; }}
        .footer {{ background: #f9fafb; padding: 15px; text-align: center; font-size: 14px; color: #4b5563; border-top: 1px solid #d1d5db; }}
        .footer a {{ color: #ef4444; text-decoration: none; }}
        @media (max-width: 600px) {{
            .container {{ margin: 10px; }}
            .header h1 {{ font-size: 20px; }}
            .content {{ padding: 20px; }}
            .content h2 {{ font-size: 18px; }}
            .content p {{ font-size: 14px; }}
            .details p {{ font-size: 14px; }}
            .cta-button {{ padding: 10px 20px; font-size: 14px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Login Notification</h1>
        </div>
        <div class='content'>
            <h2>{(isSuccess ? "Successful Login" : "Login Attempt Detected")}</h2>
            <p>Hello,</p>
            <p>We {(isSuccess ? "detected a successful login to" : "detected an attempt to access")} your StudentERP account. Below are the details:</p>
            <div class='details'>
                <p><strong>Time:</strong> {loginTime}</p>
                <p><strong>Status:</strong> {(isSuccess ? "Successful" : "Failed")}</p>
            </div>
            {(isSuccess ?
                "<p>If this was you, no further action is needed. If you don’t recognize this login, please contact our support team immediately.</p>" :
                "<p>This attempt was unsuccessful, but for your security, please verify this activity. If this wasn’t you, contact our support team immediately.</p>")}
            <a href='mailto:{_smtpSettings.FromEmail}?subject=Unauthorized%20Login%20Attempt' class='cta-button'>Contact Support</a>
            <p>Best regards,<br>The StudentERP Team</p>
        </div>
        <div class='footer'>
            <p>© 2025 StudentERP | <a href='mailto:{_smtpSettings.FromEmail}'>Contact Support</a></p>
        </div>
    </div>
</body>
</html>";
                        break;

                    case "otp":
                        subject = "Your StudentERP OTP";
                        body = $@"<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{ font-family: 'Arial', sans-serif; margin: 0; padding: 0; background-color: #f4f4f4; color: #374151; }}
        .container {{ max-width: 600px; margin: 20px auto; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.1); }}
        .header {{ background: #374151; padding: 20px; text-align: center; color: #ffffff; }}
        .header h1 {{ margin: 0; font-size: 24px; font-family: 'Permanent Marker', 'Arial', sans-serif; }}
        .content {{ padding: 30px; line-height: 1.6; }}
        .content h2 {{ color: #374151; font-size: 22px; margin-top: 0; }}
        .content p {{ font-size: 16px; margin: 10px 0; color: #374151; }}
        .otp {{ font-size: 24px; font-weight: bold; color: #ef4444; margin: 20px 0; text-align: center; }}
        .footer {{ background: #f9fafb; padding: 15px; text-align: center; font-size: 14px; color: #4b5563; border-top: 1px solid #d1d5db; }}
        @media (max-width: 600px) {{
            .container {{ margin: 10px; }}
            .header h1 {{ font-size: 20px; }}
            .content {{ padding: 20px; }}
            .content h2 {{ font-size: 18px; }}
            .content p {{ font-size: 14px; }}
            .otp {{ font-size: 20px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Your OTP Code</h1>
        </div>
        <div class='content'>
            <h2>Hello,</h2>
            <p>Your one-time password (OTP) for StudentERP login is:</p>
            <p class='otp'>{otpCode}</p>
            <p>This OTP is valid for 5 minutes. Do not share it with anyone.</p>
            <p>If you did not request this OTP, please ignore this email.</p>
            <p>Best regards,<br>The StudentERP Team</p>
        </div>
        <div class='footer'>
            <p>© 2025 StudentERP | Contact Support</p>
        </div>
    </div>
</body>
</html>";
                        break;

                    default:
                        throw new ArgumentException("Invalid email type");
                }

                message.Subject = subject;
                message.Body = new TextPart("html") { Text = body };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, _smtpSettings.EnableSSL ? MailKit.Security.SecureSocketOptions.StartTls : MailKit.Security.SecureSocketOptions.None);
                    await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SendEmailAsync - Error: {ex.Message}");
            }
        }

        private string GenerateJwtToken(StudentLogin student)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, student.StudentMail),
                new Claim("StudentId", student.StudentId.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        public async Task<List<string>> GetAvailableSemesters(string did, string fid)
        {
            try
            {
                var semesters = await _context.Semesters
                    .Where(s => s.Did == did && s.Fid == fid)
                    .Select(s => s.SemId)
                    .OrderBy(s => s)
                    .ToListAsync();
                return semesters;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAvailableSemesters - Error: {ex.Message}");
                return new List<string>();
            }
        }

        public async Task<bool> IsValidSemester(string did, string fid, string semId)
        {
            try
            {
                return await _context.Semesters
                    .AnyAsync(s => s.Did == did && s.Fid == fid && s.SemId == semId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"IsValidSemester - Error: {ex.Message}");
                return false;
            }
        }
    }
}