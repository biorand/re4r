using System.Linq;
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
    [Route("home")]
    public class HomeController(
        AuthService auth,
        DatabaseService db,
        ILogger<HomeController> logger) : ControllerBase
    {
        [HttpGet("news")]
        public async Task<object> GetNewsAsync()
        {
            var newsItems = await db.GetNewsItems();
            return newsItems
                .Select(ConvertNewsItem)
                .ToArray();
        }

        [HttpPost("news")]
        public async Task<object> InsertNewsAsync([FromBody] NewsItemRequest request)
        {
            var user = await auth.GetAuthorizedUserAsync(UserRoleKind.Administrator);
            if (user == null)
                return Unauthorized();

            var model = new NewsDbModel()
            {
                Timestamp = request.Timestamp.ToDateTime(),
                Title = request.Title,
                Body = request.Body
            };
            await db.CreateNewsItem(model);
            logger.LogInformation("User {UserId}[{UserName}] created news item {NewsId}", user.Id, user.Name, model.Id);

            return ConvertNewsItem(model);
        }

        [HttpGet("stats")]
        public async Task<object> GetStatsAsync()
        {
            var seeds = await db.GetSeedsDaily();
            var totalUsers = await db.GetTotalUsersDaily();
            return new
            {
                Seeds = seeds,
                TotalUsers = totalUsers
            };
        }

        private object ConvertNewsItem(NewsDbModel model)
        {
            return new
            {
                Date = model.Timestamp.ToString("d MMMM"),
                Timestamp = model.Timestamp.ToUnixTimeSeconds(),
                Title = model.Title,
                Body = model.Body,
            };
        }
    }
}
