using System;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Models;
using Microsoft.Extensions.Logging;

namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public class BioRandService(
        DatabaseService db,
        RandomizerService randomizerService,
        ILogger<BioRandService> logger)
    {
        public async Task Initialize()
        {
            await CreateDefaultProfiles();
        }

        private async Task CreateDefaultProfiles()
        {
            // Default profile
            var randomizer = randomizerService.GetRandomizer();
            var defaultConfig = randomizer.DefaultConfiguration;

            var profile = await db.GetDefaultProfile();
            if (profile == null)
            {
                var newProfile = new ProfileDbModel()
                {
                    UserId = db.SystemUserId,
                    Created = DateTime.UtcNow,
                    Name = "Default",
                    Description = "The default profile.",
                    Public = true
                };

                logger.LogInformation("Creating profile {Name} for default config", newProfile.Name);
                await db.CreateProfileAsync(newProfile, defaultConfig);
            }
            else
            {
                profile.Description = "The default profile.";
                profile.Public = true;

                logger.LogInformation("Updating profile {Id} {Name} to default config", profile.Id, profile.Name);
                await db.UpdateProfileAsync(profile);
                await db.SetProfileConfigAsync(profile.Id, defaultConfig);
            }
        }
    }
}
