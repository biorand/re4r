using System;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.RestModels;
using IntelOrca.Biohazard.BioRand.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IntelOrca.Biohazard.BioRand.Server.Controllers
{
    [ApiController]
    [Route("generator")]
    public class GeneratorController(
        AuthService authService,
        GeneratorService generatorService,
        ILogger<GeneratorController> logger) : ControllerBase
    {
        [HttpGet]
        public async Task<object> GetGenerators()
        {
            var user = await authService.GetAuthorizedUserAsync();
            if (user == null)
                return Unauthorized();

            return generatorService.GetAllAsync();
        }

        [HttpPost("register")]
        public async Task<object> Register([FromBody] GeneratorRegisterRequest request)
        {
            if (!TestApiKey())
                return Unauthorized();

            var generator = await generatorService.RegisterAsync(
                request.ConfigurationDefinition,
                RandomizerConfiguration.FromDictionary(request.DefaultConfiguration));
            return new
            {
                Id = generator.Id
            };
        }

        [HttpPut("heartbeat")]
        public async Task<object> Heartbeat([FromBody] GeneratorHeartbeatRequest request)
        {
            if (!TestApiKey())
                return Unauthorized();

            if (!Guid.TryParse(request.Id, out var id))
                return NotFound();

            if (!await generatorService.UpdateHeartbeatAsync(id, request.Status))
                return NotFound();

            return Ok();
        }

        [HttpGet("queue")]
        public async Task<object> GetQueue()
        {
            if (!TestApiKey())
                return Unauthorized();

            var queue = await generatorService.GetQueueAsync();
            return queue;
        }

        [HttpPost("begin")]
        public async Task<object> Begin([FromBody] GeneratorBeginRequest request)
        {
            if (!TestApiKey())
                return Unauthorized();

            if (!Guid.TryParse(request.Id, out var id))
                return NotFound();

            if (!await generatorService.IsGeneratorValid(id))
                return NotFound();

            var success = await generatorService.ProcessRando(id, request.RandoId, request.Version);
            if (!success)
                return StatusCode(StatusCodes.Status410Gone);

            return Ok();
        }

        [HttpPost("end")]
        [DisableRequestSizeLimit]
        public async Task<object> End([FromBody] GeneratorEndRequest request)
        {
            if (!TestApiKey())
                return Unauthorized();

            if (!Guid.TryParse(request.Id, out var id))
                return NotFound();

            if (!await generatorService.IsGeneratorValid(id))
                return NotFound();

            var success = await generatorService.FinishRando(id, request.RandoId, request.PakOutput, request.FluffyOutput);
            if (!success)
                return StatusCode(StatusCodes.Status410Gone);

            return Ok();
        }

        private bool TestApiKey()
        {
            var apiKey = GetApiKey();
            if (apiKey == "2wbhTK38nQxp2HTU5AAoaaho8YNobErH")
                return true;

            return false;
        }

        private string? GetApiKey()
        {
            var apiKey = HttpContext.Request.Headers["X-API-KEY"];
            return apiKey.Count >= 1 ? apiKey[0] : null;
        }
    }
}
