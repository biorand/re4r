using System.Linq;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Extensions;
using IntelOrca.Biohazard.BioRand.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntelOrca.Biohazard.BioRand.Server.Controllers
{
    [ApiController]
    [Route("info")]
    public class InfoController(
        AuthService auth,
        GeneratorService generatorService) : ControllerBase
    {
        [HttpGet]
        public async Task<object> GetAsync()
        {
            if (!await auth.IsAuthorized(Models.UserRoleKind.Administrator))
                return Unauthorized();

            var generators = await generatorService.GetAllAsync();
            var generatedRandos = await generatorService.GetGeneratedResultsAsync();
            return new
            {
                generators = generators.Select(x => new
                {
                    x.Id,
                    x.GameId,
                    x.Status,
                    RegisterTime = x.RegisterTime.ToUnixTimeSeconds(),
                    LastHeartbeatTime = x.LastHeartbeatTime.ToUnixTimeSeconds()
                }),
                totalRandoMemory = generatedRandos.Sum(x => x.ModFile.Length + x.ZipFile.Length),
                generatedRandos = generatedRandos.Select(x => new
                {
                    x.RandoId,
                    x.GameMoniker,
                    CreatedAt = x.CreatedAt.ToUnixTimeSeconds(),
                    x.Seed,
                    Assets = new[]
                    {
                        new
                        {
                            Name = "fluffy",
                            x.ModFile.Length
                        },
                        new
                        {
                            Name = "pak",
                            x.ZipFile.Length
                        }
                    }
                })
            };
        }
    }
}
