using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using StudentERP.Models;
using StudentERP.Repository.IRepository;

namespace StudentERP.Controllers
{
    public class UserController : Controller
    {
        private readonly IStudentLoginRepository _studentLoginRepository;
        private readonly IConfiguration _configuration;
        private readonly SmtpSettings _smtpSettings;

        public UserController(IStudentLoginRepository studentLoginRepository, IConfiguration configuration, IOptions<SmtpSettings> smtpSettings)
        {
            _studentLoginRepository = studentLoginRepository;
            _configuration = configuration;
            _smtpSettings = smtpSettings.Value;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(string semester = null)
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

            // Determine the semester to display
            string displaySemester = semester;
            if (string.IsNullOrEmpty(semester) || !await _studentLoginRepository.IsValidSemester(studentBatch.Did, studentBatch.Fid, semester))
            {
                displaySemester = studentBatch?.CurrentSem ?? "N/A";
            }

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
                DisplaySem = displaySemester,
                AvailableSemesters = studentBatch != null ? await _studentLoginRepository.GetAvailableSemesters(studentBatch.Did, studentBatch.Fid) : new List<string>(),
                Subjects = await _studentLoginRepository.GetSubjectsForSemester(displaySemester)
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
            var (success, message) = await _studentLoginRepository.RegisterStudentAsync(model, fullname);
            return Json(new { success, message });
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
        public async Task<IActionResult> Login(StudentLogin model, string action = null, string otpCode = null)
        {
            if (!ModelState.IsValid)
                return Json(new { success = false, message = "Validation failed" });

            var result = await _studentLoginRepository.ValidateLoginAsync(model, action, otpCode);
            if (result.Success && result.Token != null)
            {
                HttpContext.Session.SetString("StudentEmail", model.StudentMail);
                HttpContext.Session.SetString("StudentId", result.StudentId.ToString());
                return Json(new { success = true, token = result.Token, redirectUrl = Url.Action("Dashboard") });
            }
            return Json(new { success = result.Success, requiresOtp = result.RequiresOtp, message = result.Message });
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
                StudentId = student.StudentId
            };
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string CurrentPassword, string CurrentPasswordConfirm, string NewPassword)
        {
            var email = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "You must be logged in to change your password." });

            var (success, message) = await _studentLoginRepository.ChangePasswordAsync(email, CurrentPassword, CurrentPasswordConfirm, NewPassword);
            if (success)
                TempData["Success"] = "Password updated successfully!";
            return Json(new { success, message });
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
                return Json(new { exists = false, message = "Not logged in" });

            var (exists, fileName, message) = await _studentLoginRepository.CheckProfilePictureAsync(email);
            return Json(new { exists, fileName, message });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(IFormFile profilePicture, bool replaceConfirmed = false)
        {
            var email = HttpContext.Session.GetString("StudentEmail");
            if (string.IsNullOrEmpty(email))
                return Json(new { success = false, message = "You must be logged in." });

            var (success, confirmRequired, message) = await _studentLoginRepository.UpdateStudentProfileAsync(email, profilePicture, replaceConfirmed);
            return Json(new { success, confirmRequired, message });
        }
        public ActionResult About()
        {
            var email = HttpContext.Session.GetString("StudentEmail");
            Console.WriteLine($"Dashboard - Email from session: {email}");
            if (string.IsNullOrEmpty(email))
            {
                Console.WriteLine("Dashboard - No email in session, redirecting to Login");
                return RedirectToAction("Login");
            }

            return View();
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
        public string DisplaySem { get; set; } = null!;
        public List<string> AvailableSemesters { get; set; } = new List<string>();
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
        public Guid StudentId { get; set; }
    }

    public class EditProfileViewModel
    {
        public string StudentEmail { get; set; } = null!;
        public Guid StudentId { get; set; }
    }
}