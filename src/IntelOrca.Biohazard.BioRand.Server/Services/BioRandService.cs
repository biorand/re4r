using System;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Models;
using Microsoft.Extensions.Logging;

namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public class BioRandService(
        DatabaseService db,
        GeneratorService generatorService,
        ILogger<BioRandService> logger)
    {
        public async Task Initialize()
        {
            await CreateDefaultProfiles();
            await db.SetAllRandoStatusToExpiredAsync();
        }

        private async Task CreateDefaultProfiles()
        {
            // Default profile
            var defaultConfig = await generatorService.GetDefaultConfigAsync();

            var profile = await db.GetDefaultProfile();
            if (profile == null)
            {
                var newProfile = new ProfileDbModel()
                {
                    UserId = db.SystemUserId,
                    Created = DateTime.UtcNow,
                    Name = "Default",
                    Description = "The default profile.",
                    Public = true,
                    Official = true
                };

                logger.LogInformation("Creating profile {Name} for default config", newProfile.Name);
                await db.CreateProfileAsync(newProfile, defaultConfig);
            }
            else
            {
                profile.Description = "The default profile.";
                profile.Public = true;
                profile.Official = true;

                logger.LogInformation("Updating profile {Id} {Name} to default config", profile.Id, profile.Name);
                await db.UpdateProfileAsync(profile);
                await db.SetProfileConfigAsync(profile.Id, defaultConfig);
            }
        }
    }
}
