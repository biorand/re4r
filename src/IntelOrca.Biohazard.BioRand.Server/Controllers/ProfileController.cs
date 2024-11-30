using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Models;
using IntelOrca.Biohazard.BioRand.Server.RestModels;
using IntelOrca.Biohazard.BioRand.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using static IntelOrca.Biohazard.BioRand.Server.Services.DatabaseService;

namespace IntelOrca.Biohazard.BioRand.Server.Controllers
{
    [Route("profile")]
    public class ProfileController(
        AuthService auth,
        DatabaseService _db,
        RandomizerService randomizerService,
        ILogger<ProfileController> _logger) : ControllerBase
    {
        [HttpGet]
        public async Task<object> GetProfilesAsync()
        {
            var authorizedUser = await auth.GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return Unauthorized();

            var profiles = await _db.GetProfilesForUserAsync(authorizedUser.Id);
            return profiles.Select(GetProfile).ToArray();
        }

        [HttpGet("definition")]
        public object GetConfigAsync()
        {
            var randomizer = randomizerService.GetRandomizer();
            return randomizer.ConfigurationDefinition;
        }

        [HttpGet("search")]
        public async Task<object> SearchProfilesAsync([FromQuery] string? q, [FromQuery] string? user, [FromQuery] int page = 1)
        {
            var authorizedUser = await auth.GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return Unauthorized();

            var itemsPerPage = 25;
            var profiles = await _db.GetProfilesAsync(authorizedUser.Id, q, user,
                new SortOptions("StarCount", true),
                LimitOptions.FromPage(page, itemsPerPage));
            return ResultListResult.Map(page, itemsPerPage, profiles, GetProfile);
        }

        [HttpPost("")]
        public async Task<object> InsertProfileAsync([FromBody] UpdateProfileRequest body)
        {
            var authorizedUser = await auth.GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return Unauthorized();

            var config = RandomizerConfiguration.FromDictionary(body.Config);

            var profile = new ProfileDbModel()
            {
                UserId = authorizedUser.Id,
                Name = body.Name,
                Description = body.Description
            };

            profile = await _db.CreateProfileAsync(profile, config);
            _logger.LogInformation("User [{UserId}]{UserName} created profile {Id}[{Name}]",
                authorizedUser.Id, authorizedUser.Name, profile.Id, profile.Name);
            return await GetProfileAsync(profile.Id);
        }

        [HttpGet("{id}")]
        public async Task<object> GetProfileAsync(int id)
        {
            var authorizedUser = await auth.GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return Unauthorized();

            var profile = await _db.GetProfileAsync(id, authorizedUser.Id);
            if (profile == null)
                return NotFound();

            if (profile.UserId != authorizedUser.Id && !profile.Public && authorizedUser.Role != UserRoleKind.Administrator)
                return Forbid();

            return GetProfile(profile);
        }

        [HttpPut("{id}")]
        public async Task<object> UpdateProfileAsync(int id, [FromBody] UpdateProfileRequest body)
        {
            var authorizedUser = await auth.GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return Unauthorized();

            var profile = await _db.GetProfileAsync(id, authorizedUser.Id);
            if (profile == null)
                return NotFound();

            if (authorizedUser.Role != UserRoleKind.Administrator && profile.UserId != authorizedUser.Id)
                return Unauthorized();

            profile.Name = body.Name;
            profile.Description = body.Description;
            profile.Public = body.Public;
            if (authorizedUser.Role == UserRoleKind.Tester ||
                authorizedUser.Role == UserRoleKind.Administrator)
            {
                profile.Official = body.Official;
            }

            var config = RandomizerConfiguration.FromDictionary(body.Config);

            await _db.UpdateProfileAsync(profile);
            await _db.SetProfileConfigAsync(id, config);
            _logger.LogInformation("User [{UserId}]{UserName} updated profile {Id}[{Name}]",
                authorizedUser.Id, authorizedUser.Name, profile.Id, profile.Name);

            return await GetProfileAsync(id);
        }

        [HttpDelete("{id}")]
        public async Task<object> DeleteProfileAsync(int id)
        {
            var authorizedUser = await auth.GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return Unauthorized();

            var profile = await _db.GetProfileAsync(id, authorizedUser.Id);
            if (profile == null)
                return NotFound();

            if (authorizedUser.Role != UserRoleKind.Administrator && profile.UserId != authorizedUser.Id)
                return Unauthorized();

            await _db.DeleteProfileAsync(id);
            _logger.LogInformation("User [{UserId}]{UserName} deleted profile {Id}[{Name}]",
                authorizedUser.Id, authorizedUser.Name, profile.Id, profile.Name);

            return Empty;
        }

        [HttpPost("{profileId}/star")]
        [HttpDelete("{profileId}/star")]
        public async Task<object> StarProfileAsync(int profileId)
        {
            var authorizedUser = await auth.GetAuthorizedUserAsync();
            if (authorizedUser == null)
                return Unauthorized();

            var star = Request.Method switch
            {
                string s when s == HttpMethods.Post => true,
                string s when s == HttpMethods.Delete => false,
                _ => (bool?)null
            };
            if (!star.HasValue)
                return BadRequest();

            var profile = await _db.GetProfileAsync(profileId, authorizedUser.Id);
            if (profile == null)
                return NotFound();

            await _db.StarProfileAsync(profileId, authorizedUser.Id, star.Value);
            if (star == true)
            {
                _logger.LogInformation("User [{UserId}]{UserName} bookmarked profile {Id}[{Name}]",
                    authorizedUser.Id, authorizedUser.Name, profile.Id, profile.Name);
            }
            else
            {
                _logger.LogInformation("User [{UserId}]{UserName} unbookmarked profile {Id}[{Name}]",
                    authorizedUser.Id, authorizedUser.Name, profile.Id, profile.Name);
            }

            return Empty;
        }

        private object GetProfile(ExtendedProfileDbModel profile)
        {
            var randomizer = randomizerService.GetRandomizer();
            RandomizerConfiguration? config = null;
            if (!string.IsNullOrEmpty(profile.Data))
            {
                config = randomizer.DefaultConfiguration + RandomizerConfiguration.FromJson(profile.Data);
            }

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
                profile.Public,
                profile.Official,
                profile.ConfigId,
                Config = config
            };
        }

        public class UpdateProfileRequest
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public bool Public { get; set; }
            public bool Official { get; set; }
            public Dictionary<string, object> Config { get; set; } = [];
        }

        public class UpdateTempConfigRequest
        {
            public int ProfileId { get; set; }
            public Dictionary<string, object> Config { get; set; } = [];
        }
    }
}
