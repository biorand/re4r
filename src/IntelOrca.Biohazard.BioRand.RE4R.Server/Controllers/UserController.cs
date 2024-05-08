using System;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Serilog;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class UserController : BaseController
    {
        private readonly DatabaseService _db;
        private readonly EmailService _emailService;
        private readonly ILogger _logger;

        public UserController(DatabaseService db, EmailService emailService) : base(db)
        {
            _db = db;
            _emailService = emailService;
            _logger = Log.ForContext<UserController>();
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task<object> GetUsersAsync([QueryField] string sort, [QueryField] string order, [QueryField] int page)
        {
            var authorizedUser = await GetAuthorizedUserAsync(UserRoleKind.Administrator);
            if (authorizedUser == null)
                return UnauthorizedResult();

            if (page <= 0)
                page = 1;

            var itemsPerPage = 25;
            var descending = "desc".Equals(order, System.StringComparison.InvariantCultureIgnoreCase);
            var users = await _db.GetUsersAsync(sort, descending, LimitOptions.FromPage(page, itemsPerPage));
            return ResultListResult(page, itemsPerPage, users, GetUser);
        }

        [Route(HttpVerbs.Get, "/{id}")]
        public async Task<object> GetUserAsync(string id)
        {
            var processedId = (object)id;
            if (int.TryParse(id, out var numericId))
            {
                processedId = numericId;
            }

            var authorizedUser = await GetAuthorizedUserAsync(UserRoleKind.Pending);
            if (authorizedUser == null)
                return UnauthorizedResult();

            if (processedId is int userId)
            {
                if (authorizedUser.Role < UserRoleKind.Administrator && authorizedUser.Id != userId)
                    return UnauthorizedResult();

                var user = await _db.GetUserAsync(userId);
                if (user == null)
                    return NotFoundResult();

                return GetUser(user);
            }
            else
            {
                if (authorizedUser.Role < UserRoleKind.Administrator && !string.Equals(authorizedUser.NameLowerCase, (string)processedId, StringComparison.OrdinalIgnoreCase))
                    return UnauthorizedResult();

                var user = await _db.GetUserAsync((string)processedId);
                if (user == null)
                    return NotFoundResult();

                return GetUser(user);
            }
        }

        [Route(HttpVerbs.Put, "/{id}")]
        public async Task<object> UpdateUserAsync(int id, [MyJsonData] UserUpdateRequest request)
        {
            var authorizedUser = await GetAuthorizedUserAsync(UserRoleKind.Pending);
            if (authorizedUser == null)
                return UnauthorizedResult();

            var user = await _db.GetUserAsync(id);
            if (user == null)
                return NotFoundResult();

            if (authorizedUser.Role < UserRoleKind.Administrator && user.Id != authorizedUser.Id)
                return UnauthorizedResult();

            var oldRole = user.Role;
            if (authorizedUser.Role >= UserRoleKind.Administrator)
            {
                user.Name = request.Name ?? user.Name;
                user.NameLowerCase = request.Name?.ToLowerInvariant() ?? user.NameLowerCase;
                user.Role = request.Role ?? user.Role;
            }

            user.ShareHistory = request.ShareHistory ?? user.ShareHistory;

            await _db.UpdateUserAsync(user);
            _logger.Information("User [{UserId}]{UserName} updated user {UserId}[{UserName}]",
                authorizedUser.Id, authorizedUser.Name, user.Id, user.Name);

            if (oldRole == UserRoleKind.PendingEarlyAccess &&
                user.Role == UserRoleKind.EarlyAccess)
            {
                await _emailService.SendEmailAsync(user.Name, user.Email,
                    "BioRand 4 - Early Access",
$@"Dear {user.Name},

We are pleased to inform you that your request for early access has been approved.

You should now be able to sign in and generate randomizers for Resident Evil 4 (2023).

Kind regards,
The BioRand Team");
            }

            return new
            {
                Success = true
            };
        }

        public class UserUpdateRequest
        {
            public string? Email { get; set; }
            public string? Name { get; set; }
            public UserRoleKind? Role { get; set; }
            public bool? ShareHistory { get; set; }
        }
    }
}
