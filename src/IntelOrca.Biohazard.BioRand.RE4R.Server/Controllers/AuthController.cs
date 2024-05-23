using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Serilog;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class AuthController : BaseController
    {
        private readonly DatabaseService _db;
        private readonly EmailService _emailService;
        private readonly ILogger _logger;

        public AuthController(DatabaseService db, EmailService emailService, TwitchService twitchService) : base(db, twitchService)
        {
            _db = db;
            _emailService = emailService;
            _logger = Log.ForContext<AuthController>();
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
                _logger.Information("Creating user {Name} <{Email}>", email, name);
                await _db.CreateUserAsync(email, name!);
                await _emailService.SendEmailAsync(name!, email,
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
                _logger.Information("Register failed for {Name} <{Email}>", name, email);
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
            var validationResult = new Dictionary<string, string>();

            var email = req.Email?.Trim() ?? "";
            if (!IsValidEmailAddress(email))
            {
                validationResult["email"] = "Invalid e-mail address.";
            }
            else
            {
                var user = await _db.GetUserByEmail(email);
                if (user == null)
                {
                    _logger.Information("Sign in failed for {Email}, account not found", email);
                    validationResult["email"] = "E-mail address not registered.";
                }
                else if (string.IsNullOrEmpty(req.Code))
                {
                    _logger.Information("User {UserId}[{UserName}] sign in attempted", user.Id, user.Name);

                    var token = await _db.CreateTokenAsync(user);
                    _logger.Information("Auth token created for {UserId}[{UserName}]", user.Id, user.Name);

                    var code = token.Code.ToString("000000");
                    await _emailService.SendEmailAsync(user.Name, user.Email,
                        "BioRand 4 - Sign In",
$@"Dear {user.Name},

Your code for signing into BioRand 4 is: {code}

Enter this code to complete the sign in process. If you did not request this code, please ignore this message.

Kind regards,
The BioRand Team");

                    return new
                    {
                        Success = true,
                        user.Email
                    };
                }
                else
                {
                    // Check code
                    var codeInvalid = !int.TryParse(req.Code, out var code);
                    if (!codeInvalid)
                    {
                        var token = await _db.GetTokenAsync(user, code);
                        if (token != null && token.LastUsed == null)
                        {
                            _logger.Information("User {UserId}[{UserName}] signed in successfully", user.Id, user.Name);

                            if (!await _db.AdminUserExistsAsync())
                            {
                                user.Role = UserRoleKind.Administrator;
                                await _db.UpdateUserAsync(user);
                                _logger.Information("User {UserId}[{UserName}] role set to {Role}", user.Id, user.Name, user.Role);
                            }

                            if (user.Role == UserRoleKind.Pending)
                            {
                                user.Role = UserRoleKind.PendingEarlyAccess;
                                await _db.UpdateUserAsync(user);
                                _logger.Information("User {UserId}[{UserName}] role set to {Role}", user.Id, user.Name, user.Role);
                            }

                            await _db.UseTokenAsync(token.Token);
                            _logger.Information("Auth token verified for {UserId}[{UserName}]", user.Id, user.Name);
                            return new
                            {
                                Success = true,
                                token.Token,
                                User = GetUser(user)
                            };
                        }
                    }
                    _logger.Information("User {UserId}[{UserName}] sign in failed, code invalid", user.Id, user.Name);
                    validationResult["code"] = "Code is invalid.";
                }
            }

            return new
            {
                Success = false,
                Email = email,
                Validation = validationResult
            };
        }

        [Route(HttpVerbs.Post, "/signout")]
        public async Task<object> SignOutAsync()
        {
            var tokenString = GetAuthToken();
            if (tokenString == null)
                return UnauthorizedResult();

            var token = await _db.GetTokenAsync(tokenString);
            if (token == null)
                return UnauthorizedResult();

            var user = await _db.GetUserAsync(token.UserId);

            await _db.DeleteTokenAsync(token.Id);

            _logger.Information("User {UserId}[{UserName}] deleted token {TokenId}",
                user?.Id, user?.Name, token.Id);
            return EmptyResult();
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
