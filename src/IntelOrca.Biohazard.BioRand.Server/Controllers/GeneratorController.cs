using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.RestModels;
using IntelOrca.Biohazard.BioRand.Server.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace IntelOrca.Biohazard.BioRand.Server.Controllers
{
    [ApiController]
    [Route("generator")]
    public class GeneratorController(
        AuthService authService,
        GeneratorService generatorService,
        BioRandServerConfiguration config) : ControllerBase
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
                request.GameId,
                request.ConfigurationDefinition,
                RandomizerConfiguration.FromDictionary(request.DefaultConfiguration));
            return new
            {
                generator.Id
            };
        }

        [HttpPost("unregister")]
        public async Task<object> Unregister([FromBody] GeneratorUnregisterRequest request)
        {
            if (!TestApiKey())
                return Unauthorized();

            var success = await generatorService.UnregisterAsync(request.Id);
            if (!success)
                return NotFound();

            return Ok();
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

        [HttpPost("asset")]
        [DisableRequestSizeLimit]
        public async Task<object> Asset(
            [FromForm] string id,
            [FromForm] int randoId,
            [FromForm] string key,
            [FromForm] string title,
            [FromForm] string description,
            IFormFile data)
        {
            if (!TestApiKey())
                return Unauthorized();

            if (!Guid.TryParse(id, out var generatorId))
                return NotFound();

            if (!await generatorService.IsGeneratorValid(generatorId))
                return NotFound();

            var dataBytes = await GetBytes(data);
            var asset = new GenerateResultAsset(key, title, description, data.FileName, dataBytes);
            if (!await generatorService.AddAssetForRandoAsync(generatorId, randoId, asset))
                return StatusCode(StatusCodes.Status410Gone);

            return Ok();
        }


        [HttpPost("end")]
        public async Task<object> End([FromBody] GeneratorEndRequest request)
        {
            if (!TestApiKey())
                return Unauthorized();

            if (!Guid.TryParse(request.Id, out var id))
                return NotFound();

            if (!await generatorService.IsGeneratorValid(id))
                return NotFound();

            var success = await generatorService.FinishRando(id, request.RandoId, request.Instructions);
            if (!success)
                return StatusCode(StatusCodes.Status410Gone);

            return Ok();
        }

        private static async Task<byte[]> GetBytes(IFormFile formFile)
        {
            var ms = new MemoryStream();
            await formFile.CopyToAsync(ms);
            return ms.ToArray();
        }

        [HttpPost("fail")]
        public async Task<object> Fail([FromBody] GeneratorFailRequest request)
        {
            if (!TestApiKey())
                return Unauthorized();

            if (!Guid.TryParse(request.Id, out var id))
                return NotFound();

            if (!await generatorService.IsGeneratorValid(id))
                return NotFound();

            var success = await generatorService.FailRando(id, request.RandoId, request.Reason);
            if (!success)
                return StatusCode(StatusCodes.Status410Gone);

            return Ok();
        }

        private bool TestApiKey()
        {
            var apiKey = GetApiKey();
            if (string.IsNullOrEmpty(apiKey))
                return false;

            var validApiKeys = config.Generator?.ApiKeys;
            if (validApiKeys == null)
                return false;

            return validApiKeys.Contains(apiKey);
        }

        private string? GetApiKey()
        {
            var apiKey = HttpContext.Request.Headers["X-API-KEY"];
            return apiKey.Count >= 1 ? apiKey[0] : null;
        }
    }
}
