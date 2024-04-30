using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class AuthController : BaseController
    {
        private readonly DatabaseService _db;
        private readonly EmailService _emailService;

        public AuthController(DatabaseService db, EmailService emailService) : base(db)
        {
            _db = db;
            _emailService = emailService;
        }

        [Route(HttpVerbs.Post, "/register")]
        public async Task<object> RegisterAsync([MyJsonData] RegisterRequest req)
        {
            var validationResult = new Dictionary<string, string>();

            // Validate email
            var email = req.Email?.Trim() ?? "";
            if (!IsValidEmailAddress(email))
            {
                validationResult["email"] = "Invalid e-mail address.";
            }
            else
            {
                var existingUserByEmail = await _db.GetUserByEmail(email);
                if (existingUserByEmail != null)
                {
                    validationResult["email"] = "Email already registered.";
                }
            }

            // Validate name
            var name = req.Name?.Trim() ?? "";
            if (name == null || name.Length < 4)
            {
                validationResult["name"] = "Name too short.";
            }
            else if (name.Length > 32)
            {
                validationResult["name"] = "Name too long.";
            }
            else if (!Regex.IsMatch(name, "^[A-Za-z0-9_]+$"))
            {
                validationResult["name"] = "Name contains invalid characters.";
            }
            else
            {
                var existingUserByName = await _db.GetUserByName(name);
                if (existingUserByName != null)
                {
                    validationResult["name"] = "Name already registered.";
                }
            }

            if (validationResult.Count == 0)
            {
                await _db.CreateUserAsync(email, name!);
                await _emailService.SendEmailAsync(email,
                    "BioRand 4 - Early Access",
$@"Dear {name},

Thank you for registering for early access for BioRand: Resident Evil 4 (2023).
You will be informed when you are granted early access.

Kind regards,
The BioRand Team");

                return new
                {
                    Success = true,
                    Email = email,
                    Name = name
                };
            }
            else
            {
                return new
                {
                    Success = false,
                    Email = email,
                    Name = name,
                    Validation = validationResult
                };
            }
        }

        [Route(HttpVerbs.Post, "/signin")]
        public async Task<object> SignInAsync([MyJsonData] SignInRequest req)
        {
            var email = req.Email?.Trim() ?? "";

            if (!IsValidEmailAddress(email))
                return Failure(HttpStatusCode.BadRequest, "Invalid e-mail address.");

            var user = await _db.GetUserByEmail(email);
            if (user == null)
                return Failure(HttpStatusCode.BadRequest, "E-mail address not registered.");

            if (string.IsNullOrEmpty(req.Code))
            {
                var token = await _db.CreateTokenAsync(user);

                await _emailService.SendEmailAsync(email,
                    "BioRand - Sign In",
                    $"Hello {user.Name},\n\nUse the following code to login to BioRand:\n{token.Code}\n\nIf you did not request this code, ignore this message.");
                // Send e-mail
                return new
                {
                    success = true,
                    email
                };
            }
            else
            {
                // Check code
                int.TryParse(req.Code, out var code);
                var token = await _db.GetTokenAsync(user, code);
                if (token == null)
                    return Failure(HttpStatusCode.BadRequest, "Code invalid.");
                if (token.LastUsed != null)
                    return Failure(HttpStatusCode.BadRequest, "Code invalid.");

                await _db.UseTokenAsync(token.Token);
                return new
                {
                    success = true,
                    id = user.Id,
                    email = user.Email,
                    name = user.Name,
                    token = token.Token
                };
            }
        }

        private static bool IsValidEmailAddress([NotNullWhen(true)] string? email)
        {
            if (email == null || email.Length < 3 || email.Length > 256)
                return false;
            var atIndex = email.IndexOf('@');
            if (atIndex <= 0 || atIndex >= 255)
                return false;
            return true;
        }

        internal class RegisterRequest
        {
            public string? Email { get; set; }
            public string? Name { get; set; }
        }

        internal class SignInRequest
        {
            public string? Email { get; set; }
            public string? Code { get; set; }
        }
    }
}
