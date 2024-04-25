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

        public AuthController(DatabaseService db)
        {
            _db = db;
        }

        [Route(HttpVerbs.Post, "/register")]
        public async Task<object> RegisterAsync([MyJsonData] RegisterRequest req)
        {
            if (!IsValidEmailAddress(req.Email))
                return Failure(HttpStatusCode.BadRequest, "Invalid e-mail address.");
            if (req.Name == null || req.Name.Length < 1)
                return Failure(HttpStatusCode.BadRequest, "Name too short.");
            if (req.Name.Length > 32)
                return Failure(HttpStatusCode.BadRequest, "Name too long.");
            if (Regex.IsMatch(req.Name, ""))
                return Failure(HttpStatusCode.BadRequest, "Name contains invalid characters.");

            var existingUserByEmail = await _db.GetUserByEmail(req.Email);
            if (existingUserByEmail != null)
                return Failure(HttpStatusCode.BadRequest, "Email already registered.");

            var existingUserByName = await _db.GetUserByName(req.Name);
            if (existingUserByName != null)
                return Failure(HttpStatusCode.BadRequest, "Name already registered.");

            await _db.CreateUserAsync(req.Email, req.Name);
            return new
            {
                email = req.Email,
                name = req.Name
            };
        }

        [Route(HttpVerbs.Post, "/signin")]
        public async Task<object> RegisterAsync([MyJsonData] SignInRequest req)
        {
            if (!IsValidEmailAddress(req.Email))
                return Failure(HttpStatusCode.BadRequest, "Invalid e-mail address.");

            var user = await _db.GetUserByEmail(req.Email);
            if (user == null)
                return Failure(HttpStatusCode.BadRequest, "E-mail address not registered.");

            if (req.Code == null)
            {
                await _db.CreateTokenAsync(user);
                // Send e-mail
                return new { };
            }
            else
            {
                // Check code
                var token = await _db.GetTokenAsync(user, req.Code.Value);
                if (token == null)
                    return Failure(HttpStatusCode.BadRequest, "Code invalid.");
                if (token.LastUsed != null)
                    return Failure(HttpStatusCode.BadRequest, "Code invalid.");

                await _db.UseTokenAsync(token.Token);
                return new
                {
                    token = token.Token
                };
            }
        }

        private object Failure(HttpStatusCode statusCode, string reason)
        {
            Response.StatusCode = (int)statusCode;
            return new { reason };
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
            public int? Code { get; set; }
        }
    }
}
