using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentERP.Models;
using StudentERP.Repository.IRepository;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using MimeKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;

namespace StudentERP.Controllers
{
    public class UserController : Controller
    {
        private readonly IStudentLoginRepository _studentLoginRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly SmtpSettings _smtpSettings;

        public UserController(IStudentLoginRepository studentLoginRepository, IWebHostEnvironment environment, IConfiguration configuration, IOptions<SmtpSettings> smtpSettings)
        {
            _studentLoginRepository = studentLoginRepository;
            _environment = environment;
            _configuration = configuration;
            _smtpSettings = smtpSettings.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var email = HttpContext.Session.GetString("StudentEmail");
            Console.WriteLine($"Dashboard - Email from session: {email}");
            if (string.IsNullOrEmpty(email))
            {
                Console.WriteLine("Dashboard - No email in session, redirecting to Login");
                return RedirectToAction("Login");
            }

            var student = await _studentLoginRepository.GetStudentByEmail(email);
            if (student == null)
            {
                Console.WriteLine("Dashboard - Student not found, redirecting to Login");
                return RedirectToAction("Login");
            }

            var profile = await _studentLoginRepository.GetStudentProfile(student.StudentId);
            var personalDetails = await _studentLoginRepository.GetPersonalDetails(student.StudentId);
            var contactDetails = await _studentLoginRepository.GetContactDetails(student.StudentId);
            var parentsDetails = await _studentLoginRepository.GetParentsDetails(student.StudentId);
            var studentBatch = await _studentLoginRepository.GetStudentBatch(student.StudentId);

            var viewModel = new DashboardViewModel
            {
                StudentEmail = email,
                ProfilePictureName = profile?.ProfilePictureName,
                FullName = personalDetails != null ? $"{personalDetails.FirstName} {personalDetails.LastName}".Trim() : "No Data Available",
                PhoneNumber = contactDetails?.PhoneNumber ?? "No Data Available",
                FatherName = parentsDetails?.FatherName ?? "No Data Available",
                ParentPhoneNumber = parentsDetails?.ParentPhoneNumber ?? "No Data Available",
                DegreeName = studentBatch != null ? (await _studentLoginRepository.GetDegreeName(studentBatch.Did))?.Dname : "N/A",
                FieldName = studentBatch != null ? (await _studentLoginRepository.GetFieldName(studentBatch.Fid))?.Fname : "N/A",
                CurrentSem = studentBatch?.CurrentSem ?? "N/A",
                Subjects = studentBatch != null ? await _studentLoginRepository.GetSubjectsForSemester(studentBatch.CurrentSem) : new List<SubjectInfo>()
            };

            Console.WriteLine("Dashboard - Returning view");
            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(StudentLogin model, string fullname)
        {
            var existingStudent = await _studentLoginRepository.GetStudentByEmail(model.StudentMail);

            if (existingStudent != null)
                return Json(new { success = false, message = "Email already registered" });

            var studentLogin = new StudentLogin
            {
                StudentMail = model.StudentMail,
                HashPassword = model.HashPassword
            };

            var result = await _studentLoginRepository.RegisterStudent(studentLogin);
            await SendWelcomeEmail(studentLogin.StudentMail, fullname);
            if (result)
                return Json(new { success = true });
            return Json(new { success = false, message = "Registration failed. Please try again." });
        }

        private async Task SendWelcomeEmail(string toEmail, string fullName)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_smtpSettings.FromName, _smtpSettings.FromEmail));
            message.To.Add(new MailboxAddress(fullName, toEmail));
            message.Subject = "Welcome to StudentERP! Let's Get Started!";

            message.Body = new TextPart("html")
            {
                Text = $@"<!DOCTYPE html>
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
            .cta-button {{ padding: 10px 20px; font-size: 14px; }}
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Welcome to StudentERP!</h1>
        </div>
        <div class='content'>
            <h2>Hello {toEmail},</h2>
            <p>Your journey with StudentERP starts now, and we’re thrilled to have you on board!</p>
            <p>Your account is all set, opening the door to a seamless way to manage your academic world. Dive into your personalized dashboard to explore your courses, track progress, and more.</p>
            
            <p>Have questions? Our team is here to help you every step of the way.</p>
            <p>Warm regards,<br>The StudentERP Team</p>
        </div>
        <div class='footer'>
            <p>&copy; 2025 StudentERP | Contact Support</a></p>
        </div>
    </div>
</body>
</html>"
            };

            using (var client = new SmtpClient())
            {
                try
                {
                    await client.ConnectAsync(_smtpSettings.Host, _smtpSettings.Port, MailKit.Security.SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_smtpSettings.Username, _smtpSettings.Password);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to send email: {ex.Message}");
                }
            }
        }
        [HttpGet]
        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(HttpContext.Session.GetString("StudentEmail")))
                return RedirectToAction("Dashboard");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(StudentLogin model)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Validation failed" });

            var student = await _studentLoginRepository.GetStudentByEmail(model.StudentMail);
            if (student == null || student.HashPassword != _studentLoginRepository.HashPassword(model.HashPassword) || !student.IsActive)
                return Json(new { success = false, message = "Invalid email, password, or inactive account" });

            HttpContext.Session.SetString("StudentEmail", student.StudentMail);
            HttpContext.Session.SetString("StudentId", student.StudentId.ToString());
            var token = GenerateJwtToken(student);

            return Json(new { success = true, token = token });
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

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("StudentEmail")))
                return RedirectToAction("Login");

            var email = HttpContext.Session.GetString("StudentEmail");
            var student = await _studentLoginRepository.GetStudentByEmail(email);
            var profile = await _studentLoginRepository.GetStudentProfile(student.StudentId);

            var viewModel = new ProfileViewModel
            {
                StudentEmail = email,
                HasProfilePicture = profile != null && !string.IsNullOrEmpty(profile.ProfilePictureName),
                StudentId = student.StudentId // Added
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string CurrentPasswordConfirm, string NewPassword)
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("StudentEmail")))
                return Json(new { success = false, message = "You must be logged in to change your password." });

            if (string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(CurrentPasswordConfirm) || string.IsNullOrEmpty(NewPassword))
                return Json(new { success = false, message = "All fields are required." });

            if (CurrentPassword != CurrentPasswordConfirm)
                return Json(new { success = false, message = "Current passwords do not match." });

            var email = HttpContext.Session.GetString("StudentEmail");
            var student = await _studentLoginRepository.GetStudentByEmail(email);
            if (student == null || student.HashPassword != _studentLoginRepository.HashPassword(CurrentPassword))
                return Json(new { success = false, message = "Current password is incorrect." });

            if (_studentLoginRepository.HashPassword(NewPassword) == student.HashPassword)
                return Json(new { success = false, message = "New password must be different from the current password." });

            student.HashPassword = _studentLoginRepository.HashPassword(NewPassword);
            var result = await _studentLoginRepository.UpdateStudent(student);
            if (result)
            {
                TempData["Success"] = "Password updated successfully!";
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Failed to update password. Please try again." });
        }

        [HttpGet]
        public async Task<IActionResult> EditProfile()
        {
            if (string.IsNullOrEmpty(HttpContext.Session.GetString("StudentEmail")))
                return RedirectToAction("Login");

            var email = HttpContext.Session.GetString("StudentEmail");
            var student = await _studentLoginRepository.GetStudentByEmail(email);
            if (student == null)
                return RedirectToAction("Login");

            var viewModel = new EditProfileViewModel
            {
                StudentEmail = email,
                StudentId = student.StudentId
            };
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> CheckProfilePicture()
        {
            var email = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(email))
            {
                return Json(new { exists = false, message = "Not logged in" });
            }

            var student = await _studentLoginRepository.GetStudentByEmail(email);
            if (student == null)
            {
                return Json(new { exists = false, message = "Invalid session" });
            }

            var profile = await _studentLoginRepository.GetStudentProfile(student.StudentId);
            if (profile != null && !string.IsNullOrEmpty(profile.ProfilePictureName))
            {
                var filePath = Path.Combine(_environment.WebRootPath, "Images", profile.ProfilePictureName);
                if (System.IO.File.Exists(filePath))
                {
                    return Json(new { exists = true, fileName = profile.ProfilePictureName });
                }
            }

            return Json(new { exists = false });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(IFormFile profilePicture, bool replaceConfirmed = false)
        {
            Console.WriteLine("Entering EditProfile POST - Before anything else");
            try
            {
                Console.WriteLine("EditProfile POST started - Inside try block");

                var email = HttpContext.Session.GetString("StudentEmail");
                Console.WriteLine($"Session email retrieved: {email ?? "null"}");
                if (string.IsNullOrEmpty(email))
                {
                    Console.WriteLine("No email in session");
                    return Json(new { success = false, message = "You must be logged in." });
                }

                Console.WriteLine($"Fetching student for email: {email}");
                var student = await _studentLoginRepository.GetStudentByEmail(email);
                if (student == null)
                {
                    Console.WriteLine("Student not found");
                    return Json(new { success = false, message = "Invalid session." });
                }

                Console.WriteLine($"Profile picture: {profilePicture?.FileName ?? "null"}");
                if (profilePicture == null || profilePicture.Length == 0)
                {
                    Console.WriteLine("No file selected");
                    return Json(new { success = false, message = "Please select a file." });
                }

                var expectedFileName = $"{student.StudentId.ToString().ToUpper()}.jpg";
                if (profilePicture.FileName != expectedFileName)
                {
                    Console.WriteLine($"Invalid filename. Expected: {expectedFileName}, Got: {profilePicture.FileName}");
                    return Json(new { success = false, message = $"Invalid filename. Please rename your file to '{expectedFileName}' and try again." });
                }

                var fileName = expectedFileName;
                var filePath = Path.Combine(_environment.WebRootPath, "Images", fileName);

                var existingProfile = await _studentLoginRepository.GetStudentProfile(student.StudentId);
                if (existingProfile != null && !string.IsNullOrEmpty(existingProfile.ProfilePictureName) && !replaceConfirmed)
                {
                    if (System.IO.File.Exists(filePath))
                    {
                        Console.WriteLine("Profile picture already exists, awaiting confirmation");
                        return Json(new { success = false, confirmRequired = true, message = $"A profile picture ({fileName}) already exists. Do you want to replace it?" });
                    }
                }

                Console.WriteLine($"WebRootPath: {_environment.WebRootPath}");
                Console.WriteLine($"Saving file to: {filePath}");
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    Console.WriteLine("Writing file stream");
                    await profilePicture.CopyToAsync(fileStream);
                }

                var profile = new StudentProfile
                {
                    StudentId = student.StudentId,
                    ProfilePictureName = fileName
                };
                Console.WriteLine($"Saving profile to database: StudentId={profile.StudentId}, FileName={profile.ProfilePictureName}");
                var saveResult = await _studentLoginRepository.SaveOrReplaceStudentProfile(profile);
                if (!saveResult)
                {
                    Console.WriteLine("Failed to save profile to database");
                    return Json(new { success = false, message = "Failed to save profile data." });
                }

                Console.WriteLine("File and profile saved successfully");
                return Json(new { success = true, message = "Profile picture uploaded and saved successfully!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Upload error: {ex.Message}" });
            }
        }
    }

    public class DashboardViewModel
    {
        public string StudentEmail { get; set; } = null!;
        public string? ProfilePictureName { get; set; }
        public string FullName { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string FatherName { get; set; } = null!;
        public string ParentPhoneNumber { get; set; } = null!;
        public string? DegreeName { get; set; }
        public string? FieldName { get; set; }
        public string? CurrentSem { get; set; }
        public List<SubjectInfo> Subjects { get; set; } = new List<SubjectInfo>();
    }

    public class SubjectInfo
    {
        public string SubjectName { get; set; } = null!;
        public string SubjectCode { get; set; } = null!;
        public int SubjectCredit { get; set; }
        public string? SyllabusFileName { get; set; }
    }

    public class ProfileViewModel
    {
        public string StudentEmail { get; set; } = null!;
        public bool HasProfilePicture { get; set; }
        public Guid StudentId { get; set; } // Added
    }

    public class EditProfileViewModel
    {
        public string StudentEmail { get; set; } = null!;
        public Guid StudentId { get; set; }
    }
}