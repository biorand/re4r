using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.RestModels;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController(
        AuthService auth,
        DatabaseService db,
        EmailService emailService,
        UserService userService,
        ILogger<AuthController> logger) : ControllerBase
    {
        [HttpPost("register")]
        public async Task<object> RegisterAsync([FromBody] RegisterRequest req)
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
                var existingUserByEmail = await db.GetUserByEmail(email);
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
                var existingUserByName = await db.GetUserByName(name);
                if (existingUserByName != null)
                {
                    validationResult["name"] = "Name already registered.";
                }
            }

            if (validationResult.Count == 0)
            {
                logger.LogInformation("Creating user {Name} <{Email}>", email, name);
                await db.CreateUserAsync(email, name!);
                await emailService.SendEmailAsync(name!, email,
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
                logger.LogInformation("Register failed for {Name} <{Email}>", name, email);
                return new
                {
                    Success = false,
                    Email = email,
                    Name = name,
                    Validation = validationResult
                };
            }
        }

        [HttpPost("signin")]
        public async Task<object> SignInAsync([FromBody] SignInRequest req)
        {
            var validationResult = new Dictionary<string, string>();

            var email = req.Email?.Trim() ?? "";
            if (!IsValidEmailAddress(email))
            {
                validationResult["email"] = "Invalid e-mail address.";
            }
            else
            {
                var user = await db.GetUserByEmail(email);
                if (user == null)
                {
                    logger.LogInformation("Sign in failed for {Email}, account not found", email);
                    validationResult["email"] = "E-mail address not registered.";
                }
                else if (string.IsNullOrEmpty(req.Code))
                {
                    logger.LogInformation("User {UserId}[{UserName}] sign in attempted", user.Id, user.Name);

                    var token = await db.CreateTokenAsync(user);
                    logger.LogInformation("Auth token created for {UserId}[{UserName}]", user.Id, user.Name);

                    var code = token.Code.ToString("000000");
                    await emailService.SendEmailAsync(user.Name, user.Email,
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
                        var token = await db.GetTokenAsync(user, code);
                        if (token != null && token.LastUsed == null)
                        {
                            logger.LogInformation("User {UserId}[{UserName}] signed in successfully", user.Id, user.Name);

                            if (!await db.AdminUserExistsAsync())
                            {
                                user.Role = UserRoleKind.Administrator;
                                await db.UpdateUserAsync(user);
                                logger.LogInformation("User {UserId}[{UserName}] role set to {Role}", user.Id, user.Name, user.Role);
                            }

                            if (user.Role == UserRoleKind.Pending)
                            {
                                user.Role = UserRoleKind.PendingEarlyAccess;
                                await db.UpdateUserAsync(user);
                                logger.LogInformation("User {UserId}[{UserName}] role set to {Role}", user.Id, user.Name, user.Role);
                            }

                            await db.UseTokenAsync(token.Token);
                            logger.LogInformation("Auth token verified for {UserId}[{UserName}]", user.Id, user.Name);
                            return new
                            {
                                Success = true,
                                token.Token,
                                User = userService.GetUser(user)
                            };
                        }
                    }
                    logger.LogInformation("User {UserId}[{UserName}] sign in failed, code invalid", user.Id, user.Name);
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

        [HttpPost("signout")]
        public async Task<object> SignOutAsync()
        {
            var tokenString = auth.GetAuthToken();
            if (tokenString == null)
                return Unauthorized();

            var token = await db.GetTokenAsync(tokenString);
            if (token == null)
                return Unauthorized();

            var user = await db.GetUserAsync(token.UserId);

            await db.DeleteTokenAsync(token.Id);

            logger.LogInformation("User {UserId}[{UserName}] deleted token {TokenId}",
                user?.Id, user?.Name, token.Id);
            return Empty;
        }

        [ApiExplorerSettings(IgnoreApi = true)]
        [HttpGet("tokens")]
        public async Task<object> GetTokens(string? sort, string? order, int page = 1)
        {
            var user = await auth.GetAuthorizedUserAsync(UserRoleKind.Administrator);
            if (user == null)
                return Unauthorized();

            if (sort == null)
            {
                sort = "Created";
                order = "desc";
            }

            var itemsPerPage = 25;
            var result = await db.GetTokensAsync(
                SortOptions.FromQuery(sort, order, ["Created", "LastUsed", "UserName", "UserEmail"]),
                LimitOptions.FromPage(page, itemsPerPage));
            return ResultListResult.Map(page, itemsPerPage, result, x => new
            {
                x.Id,
                Created = x.Created.ToUnixTimeSeconds(),
                LastUsed = x.LastUsed?.ToUnixTimeSeconds(),
                Code = x.Code.ToString(),
                x.Token,
                User = new
                {
                    Id = x.UserId,
                    Name = x.UserName,
                    Email = x.UserEmail,
                    AvatarUrl = x.UserAvatarUrl
                }
            });
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

        public class RegisterRequest
        {
            public string? Email { get; set; }
            public string? Name { get; set; }
        }

        public class SignInRequest
        {
            public string? Email { get; set; }
            public string? Code { get; set; }
        }
    }
}
