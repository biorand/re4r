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
        GeneratorService generatorService,
        DatabaseService db) : ControllerBase
    {
        [HttpGet]
        public async Task<object> GetAsync()
        {
            if (!await auth.IsAuthorized(Models.UserRoleKind.Administrator))
                return Unauthorized();

            var generators = await generatorService.GetAllAsync();
            var generatedRandos = await generatorService.GetGeneratedResultsAsync();
            var games = await db.GetGamesAsync();
            return new
            {
                generators = generators.Select(x => new
                {
                    x.Id,
                    x.GameId,
                    GameMoniker = games.FirstOrDefault(y => y.Id == x.GameId)?.Moniker ?? null,
                    x.Status,
                    RegisterTime = x.RegisterTime.ToUnixTimeSeconds(),
                    LastHeartbeatTime = x.LastHeartbeatTime.ToUnixTimeSeconds()
                }),
                totalRandoMemory = generatedRandos.Sum(x => x.Assets.Sum(y => (long)y.Data.Length)),
                generatedRandos = generatedRandos.Select(x => new
                {
                    Id = x.RandoId,
                    x.Seed,
                    x.Status,
                    StartTime = x.StartTime.ToUnixTimeSeconds(),
                    FinishTime = x.FinishTime.ToUnixTimeSeconds(),
                    x.FailReason,
                    Assets = x.Assets.Select(y => new
                    {
                        y.Key,
                        y.FileName,
                        y.Data.Length
                    })
                })
            };
        }
    }
}
