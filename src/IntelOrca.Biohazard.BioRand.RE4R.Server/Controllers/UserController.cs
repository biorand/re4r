using System.Linq;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class UserController : BaseController
    {
        private readonly DatabaseService _db;

        public UserController(DatabaseService db) : base(db)
        {
            _db = db;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task<object> GetUsersAsync([QueryField] string sort, [QueryField] string order, [QueryField] int page)
        {
            var authorizedUser = await GetAuthorizedUserAsync(UserRoleKind.Administrator);
            if (authorizedUser == null)
                return UnauthorizedResult();

            if (page <= 0)
                page = 1;

            var pageCount = 25;
            var descending = "desc".Equals(order, System.StringComparison.InvariantCultureIgnoreCase);
            var users = await _db.GetUsersAsync(sort, descending, (page - 1) * pageCount, pageCount);
            return new
            {
                Page = page,
                PageCount = 1,
                PageResults = users.Select(GetUser).ToArray()
            };
        }

        [Route(HttpVerbs.Get, "/{id}")]
        public async Task<object> GetUsersAsync(int id)
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return UnauthorizedResult();

            if (authorizedUser.Id != id && authorizedUser.Role < UserRoleKind.Administrator)
                return UnauthorizedResult();

            var user = await _db.GetUserAsync(id);
            return GetUser(user);
        }
    }
}
