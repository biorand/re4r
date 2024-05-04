using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Swan.Formatters;
using static IntelOrca.Biohazard.BioRand.RE4R.Server.Services.DatabaseService;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class ProfileController : BaseController
    {
        private readonly DatabaseService _db;

        public ProfileController(DatabaseService db) : base(db)
        {
            _db = db;
        }

        [Route(HttpVerbs.Get, "/")]
        public async Task<object> GetProfilesAsync()
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return UnauthorizedResult();

            var profiles = await _db.GetProfilesAsync(authorizedUser.Id);
            return profiles.Select(GetProfile).ToArray();
        }

        [Route(HttpVerbs.Get, "/definition")]
        public Task<RandomizerConfigurationDefinition> GetConfigAsync()
        {
            var chainsawRandomizerFactory = ChainsawRandomizerFactory.Default;
            var randomizer = chainsawRandomizerFactory.Create();
            var enemyClassFactory = randomizer.EnemyClassFactory;
            var configDefinition = RandomizerConfigurationDefinition.Create(enemyClassFactory);
            return Task.FromResult(configDefinition);
        }

        [Route(HttpVerbs.Get, "/search")]
        public async Task<object> SearchProfilesAsync([QueryField] string q, [QueryField] string user, [QueryField] int page)
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return UnauthorizedResult();

            var profiles = await _db.GetProfilesAsync(q, user, page);
            return new
            {
                Page = 1,
                PageCount = 1,
                PageResults = profiles.Select(GetProfile).ToArray()
            };
        }

        [Route(HttpVerbs.Post, "/")]
        public async Task<object> InsertProfileAsync([MyJsonData] UpdateProfileRequest body)
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return UnauthorizedResult();

            var config = RandomizerConfigurationDefinition.ProcessConfig(body.Config);

            var profile = await _db.CreateProfileAsync(authorizedUser.Id, body.Name, body.Description, config);
            return await GetProfileAsync(profile.Id);
        }

        [Route(HttpVerbs.Get, "/{id}")]
        public async Task<object> GetProfileAsync(int id)
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return UnauthorizedResult();

            var profile = await _db.GetProfileAsync(id, authorizedUser.Id);
            if (profile == null)
                return NotFoundResult();

            if (profile.UserId != authorizedUser.Id && !profile.Public && authorizedUser.Role != UserRoleKind.Administrator)
                return ForbiddenResult();

            return GetProfile(profile);
        }

        [Route(HttpVerbs.Put, "/{id}")]
        public async Task<object> UpdateProfileAsync(int id, [MyJsonData] UpdateProfileRequest body)
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return UnauthorizedResult();

            var profile = await _db.GetProfileAsync(id, authorizedUser.Id);
            if (profile == null)
                return NotFoundResult();

            if (authorizedUser.Role < UserRoleKind.Administrator && profile.UserId != authorizedUser.Id)
                return UnauthorizedResult();

            profile.Name = body.Name;
            profile.Description = body.Description;

            var config = RandomizerConfigurationDefinition.ProcessConfig(body.Config);
            await _db.UpdateProfileAsync(profile);
            await _db.SetProfileConfigAsync(id, config);

            return await GetProfileAsync(id);
        }

        [Route(HttpVerbs.Delete, "/{id}")]
        public async Task<object> DeleteProfileAsync(int id)
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return UnauthorizedResult();

            var profile = await _db.GetProfileAsync(id, authorizedUser.Id);
            if (profile == null)
                return NotFoundResult();

            if (authorizedUser.Role < UserRoleKind.Administrator && profile.UserId != authorizedUser.Id)
                return UnauthorizedResult();

            await _db.DeleteProfileAsync(id);
            return EmptyResult();
        }

        [Route(HttpVerbs.Any, "/{profileId}/star")]
        public async Task<object> StarProfileAsync(int profileId)
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return UnauthorizedResult();

            var star = Request.HttpVerb switch
            {
                HttpVerbs.Post => true,
                HttpVerbs.Delete => false,
                _ => (bool?)null
            };
            if (!star.HasValue)
                return ErrorResult(HttpStatusCode.BadRequest);

            await _db.StarProfileAsync(profileId, authorizedUser.Id, star.Value);
            return EmptyResult();
        }

        [Route(HttpVerbs.Put, "/temp")]
        public async Task<object> UpdateTempProfileAsync([MyJsonData] UpdateTempConfigRequest body)
        {
            var authorizedUser = await GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return UnauthorizedResult();

            await _db.SetUserConfigAsync(authorizedUser.Id, body.ProfileId, body.Config);
            return EmptyResult();
        }

        private object GetProfile(ExtendedProfileDbModel profile)
        {
            return new
            {
                profile.Id,
                profile.Name,
                profile.Description,
                profile.UserId,
                profile.UserName,
                profile.StarCount,
                profile.SeedCount,
                profile.IsStarred,
                profile.ConfigId,
                Config = string.IsNullOrEmpty(profile.Data) ? null : Json.Deserialize(profile.Data)
            };
        }

        public class UpdateProfileRequest
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public Dictionary<string, object> Config { get; set; } = [];
        }

        public class UpdateTempConfigRequest
        {
            public int ProfileId { get; set; }
            public Dictionary<string, object> Config { get; set; } = [];
        }
    }
}
