using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Extensions;
using IntelOrca.Biohazard.BioRand.Server.Models;
using IntelOrca.Biohazard.BioRand.Server.RestModels;
using IntelOrca.Biohazard.BioRand.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IntelOrca.Biohazard.BioRand.Server.Controllers
{
    [ApiController]
    [Route("patron")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class PatronController(AuthService auth, DatabaseService db, ILogger<PatronController> logger) : ControllerBase
    {
        [HttpGet("donations")]
        public async Task<object> GetKofiDonations(int? gameId, string? user, string? sort, string? order, int page = 1)
        {
            if (!(await auth.IsAuthorized(UserRoleKind.Administrator)))
                return Unauthorized();

            if (sort == null)
            {
                sort = "timestamp";
                order ??= "desc";
            }

            var itemsPerPage = 25;
            var result = await db.GetKofiAsync(
                gameId,
                user,
                SortOptions.FromQuery(sort, order, ["Timestamp", "UserName", "Email", "Price", "TierName"]),
                LimitOptions.FromPage(page, itemsPerPage));
            return ResultListResult.Map(page, itemsPerPage, result, x => new
            {
                x.Id,
                x.GameId,
                MessageId = x.MessageId.ToString(),
                Timestamp = x.Timestamp.ToUnixTimeSeconds(),
                x.Email,
                Amount = x.Price,
                x.TierName,
                Payload = x.Data,
                User = x.UserId == null ? null : new
                {
                    Id = x.UserId,
                    Name = x.UserName,
                    AvatarUrl = x.UserAvatarUrl
                }
            });
        }

        [HttpPut("match")]
        public async Task<object> UpdateDonationUser([FromBody] PatronUserMatchRequest req)
        {
            var authUser = await auth.GetAuthorizedUserAsync(UserRoleKind.Administrator);
            if (authUser == null)
                return Unauthorized();

            var kofi = await db.GetKofiAsync(req.Id);
            if (kofi == null)
                return NotFound();

            var user = await db.GetUserByName(req.UserName);
            if (user == null)
                return BadRequest();

            kofi.UserId = user.Id;
            await db.UpdateKofiAsync(kofi);
            logger.LogInformation("User {UserId}[{UserName}] matched kofi {KofiId} with user {MatchedUserId}[{MatchedUserName}]",
                authUser.Id, authUser.Name, kofi.Id, user.Id, user.Name);

            return Ok();
        }

        [HttpGet("daily")]
        public async Task<object> GetKofiDaily(int gameId)
        {
            if (!(await auth.IsAuthorized(UserRoleKind.Administrator)))
                return Unauthorized();

            return await db.GetKofiDaily(gameId);
        }
    }
}
