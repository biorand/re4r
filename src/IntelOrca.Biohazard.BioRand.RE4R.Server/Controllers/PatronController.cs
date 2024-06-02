using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.RestModels;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    [ApiController]
    [Route("patron")]
    public class PatronController(AuthService auth, DatabaseService db) : ControllerBase
    {
        [HttpGet("donations")]
        public async Task<object> GetKofiDonations(string? sort, string? order, int page = 1)
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
                SortOptions.FromQuery(sort, order, ["Timestamp", "UserId", "Email", "Price"]),
                LimitOptions.FromPage(page, itemsPerPage));
            return ResultListResult.Map(page, itemsPerPage, result, x => new
            {
                x.Id,
                MessageId = x.MessageId.ToString(),
                Timestamp = x.Timestamp.ToUnixTimeSeconds(),
                x.Email,
                Amount = x.Price,
                x.TierName,
                Payload = x.Data,
                User = new
                {
                    Id = x.UserId,
                    Name = x.UserName,
                    Role = x.UserRole,
                    AvatarUrl = x.UserAvatarUrl
                }
            });
        }
    }
}
