using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Swan.Logging;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class MainController : WebApiController
    {
        private readonly RandomizerService _randomizer;

        public MainController(RandomizerService randomizer)
        {
            _randomizer = randomizer;
        }

        [Route(HttpVerbs.Get, "/config")]
        public Task<RandomizerConfigurationDefinition> GetConfigAsync()
        {
            return _randomizer.GetConfigAsync();
        }

        [Route(HttpVerbs.Post, "/generate")]
        public async Task<object> GenerateAsync([MyJsonData] GenerateRequest request)
        {
            var isValidPassword = IsMatchingPassword(request.Password);
            var logLevel = isValidPassword ? LogLevel.Info : LogLevel.Warning;

            var ipAddress = Request.Headers["X-Forwarded-For"];
            if (string.IsNullOrEmpty(ipAddress))
                ipAddress = Request.RemoteEndPoint.ToString();

            var configJson = JsonSerializer.Serialize(request.Config);
            $"Generate [{ipAddress}] [{request.Password}] Seed = {request.Seed} Config = {configJson}".Log(
                typeof(MainController),
                logLevel);

            if (!isValidPassword)
            {
                return new
                {
                    result = "failure",
                    message = "Incorrect password"
                };
            }

            var result = await _randomizer.GenerateAsync(
                request.Seed,
                RandomizerConfigurationDefinition.ProcessConfig(request.Config));
            return new
            {
                result = "success",
                seed = result.Seed,
                downloadUrl = CreateUrl($"/download?id={result.Id}"),
                downloadUrlMod = CreateUrl($"/download?id={result.Id}&mod=true")
            };
        }

        private bool IsMatchingPassword(string? password)
        {
            try
            {
                var webConfig = Re4rConfiguration.GetDefault();
                return webConfig.Passwords != null && webConfig.Passwords.Contains(password);
            }
            catch (Exception ex)
            {
                ex.Log(typeof(MainController));
                return false;
            }
        }

        private string CreateUrl(string path)
        {
            return path;
        }

        public class GenerateRequest
        {
            public int Seed { get; set; }
            public string? Password { get; set; }
            public Dictionary<string, object>? Config { get; set; }
        }
    }
}
