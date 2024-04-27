using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class AuthController : WebApiController
    {
        private readonly DatabaseService _db;
        private readonly EmailService _emailService;

        public AuthController(DatabaseService db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }

        [Route(HttpVerbs.Post, "/register")]
        public async Task<object> RegisterAsync([MyJsonData] RegisterRequest req)
        {
            var email = req.Email?.Trim() ?? "";
            var name = req.Name?.Trim() ?? "";

            if (!IsValidEmailAddress(email))
                return Failure(HttpStatusCode.BadRequest, "Invalid e-mail address.");
            if (name == null || name.Length < 4)
                return Failure(HttpStatusCode.BadRequest, "Name too short.");
            if (name.Length > 32)
                return Failure(HttpStatusCode.BadRequest, "Name too long.");
            if (!Regex.IsMatch(name, "^[A-Za-z0-9_]+$"))
                return Failure(HttpStatusCode.BadRequest, "Name contains invalid characters.");

            var existingUserByEmail = await _db.GetUserByEmail(email);
            if (existingUserByEmail != null)
                return Failure(HttpStatusCode.BadRequest, "Email already registered.");

            var existingUserByName = await _db.GetUserByName(name);
            if (existingUserByName != null)
                return Failure(HttpStatusCode.BadRequest, "Name already registered.");

            var user = await _db.CreateUserAsync(email, name);
            var token = await _db.CreateTokenAsync(user);

            await _emailService.SendEmailAsync(email,
                $"Welcome {name},\n\nYou are now registered for BioRand.\n\nUse the following code to login:\n{token.Code}");

            return new
            {
                email,
                name
            };
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
                    email = user.Email,
                    name = user.Name,
                    token = token.Token
                };
            }
        }

        private object Failure(HttpStatusCode statusCode, string message)
        {
            Response.StatusCode = (int)statusCode;
            return new
            {
                success = false,
                message
            };
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
