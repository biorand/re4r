using System.Linq;
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

        [HttpPut("news/{id}")]
        public async Task<object> UpdateNewsAsync(int id, [FromBody] NewsItemRequest request)
        {
            var user = await auth.GetAuthorizedUserAsync(UserRoleKind.Administrator);
            if (user == null)
                return Unauthorized();

            var model = await db.GetNewsItem(id);
            if (model == null)
                return NotFound();

            model.Timestamp = request.Timestamp.ToDateTime();
            model.Title = request.Title;
            model.Body = request.Body;

            await db.UpdateNewsItem(model);
            logger.LogInformation("User {UserId}[{UserName}] edited news item {NewsId}", user.Id, user.Name, id);
            return Ok();
        }

        [HttpDelete("news/{id}")]
        public async Task<object> DeleteNewsAsync(int id)
        {
            var user = await auth.GetAuthorizedUserAsync(UserRoleKind.Administrator);
            if (user == null)
                return Unauthorized();

            await db.DeleteNewsItem(id);
            logger.LogInformation("User {UserId}[{UserName}] deleted news item {NewsId}", user.Id, user.Name, id);
            return Ok();
        }

        [HttpGet("stats")]
        public async Task<object> GetStatsAsync()
        {
            var seeds = await db.GetSeedsDaily();
            var totalUsers = await db.GetTotalUsersMonthly();
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
                Id = model.Id,
                Date = model.Timestamp.ToString("d MMMM"),
                Timestamp = model.Timestamp.ToUnixTimeSeconds(),
                Title = model.Title,
                Body = model.Body,
            };
        }
    }
}
