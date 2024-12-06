using System.Linq;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntelOrca.Biohazard.BioRand.Server.Controllers
{
    [ApiController]
    [Route("game")]
    public class GameController(DatabaseService db) : ControllerBase
    {
        [HttpGet]
        public async Task<object> GetAsync()
        {
            var games = await db.GetGamesAsync();
            return games.Select(x => new
            {
                x.Id,
                x.Name,
                x.Moniker
            }).ToArray();
        }
    }
}
