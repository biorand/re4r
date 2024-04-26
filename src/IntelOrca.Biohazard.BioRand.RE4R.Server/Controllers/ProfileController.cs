using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Swan.Formatters;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class ProfileController : WebApiController
    {
        private readonly DatabaseService _db;

        public ProfileController(DatabaseService db)
        {
            _db = db;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task<object> GetProfilesAsync()
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return new object { };
            }

            var profiles = await _db.GetProfilesAsync(authorizedUser.Id);
            return profiles.Select(x => new
            {
                x.Id,
                x.UserId,
                x.UserName,
                x.Name,
                x.Description,
                CreatedAt = x.Created,
                x.StarCount,
                x.SeedCount,
                Data = Json.Deserialize(x.Data)
            }).ToArray();
        }

        [Route(HttpVerbs.Get, "/search")]
        public async Task<object> SearchProfilesAsync([QueryField] string q, [QueryField] string user, [QueryField] int page)
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
            {
                Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                return new object { };
            }

            var profiles = await _db.GetProfilesAsync(q, user, page);
            return new
            {
                Page = 1,
                PageCount = 1,
                PageResults = profiles.Select(x => new
                {
                    x.Id,
                    x.Name,
                    x.Description,
                    x.UserName,
                    x.StarCount,
                    x.SeedCount,
                    IsStarred = false
                }).ToArray()
            };
        }

        private async Task<UserDbModel?> GetAuthorizedUserAsync()
        {
            var authorization = HttpContext.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authorization))
            {
                var parts = authorization.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var type = parts[0];
                    var token = parts[1];
                    if (type == "Bearer")
                    {
                        return await _db.GetUserByToken(token);
                    }
                }
            }
            return null;
        }
    }
}
