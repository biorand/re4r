using System;
using System.Threading;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Controllers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static IntelOrca.Biohazard.BioRand.Server.Services.DatabaseService;

namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public class GenerationService : IHostedService
    {
        private readonly DatabaseService _db;
        private readonly RandomizerService _randomizerService;
        private readonly ILogger _logger;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        private Task? _mainTask;

        public GenerationService(
            DatabaseService db,
            RandomizerService randomizerService,
            ILogger<RandoController> logger)
        {
            _db = db;
            _randomizerService = randomizerService;
            _logger = logger;
        }

        public Task StartAsync(CancellationToken ct)
        {
            _cts = new CancellationTokenSource();
            _mainTask = Task.Run(ProcessLoop, ct);
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken ct)
        {
            if (_mainTask != null)
            {
                await _cts.CancelAsync();
                await _mainTask;
            }
        }

        private async Task ProcessLoop()
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    await CleanupAsync();
                    await GenerateNextRandoAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process generation task.");
                }
                await Task.Delay(500);
            }
        }

        private async Task CleanupAsync()
        {
            var ids = await _randomizerService.ExpireOldRandos();
            foreach (var id in ids)
            {
                await _db.SetRandoStatusAsync((int)id, Models.RandoStatus.Expired);
            }
        }

        private async Task GenerateNextRandoAsync()
        {
            var unassignedRandos = await _db.GetUnassignedRandosAsync();
            if (unassignedRandos.Results.Length == 0)
                return;

            var rando = unassignedRandos.Results[0];
            try
            {
                await _db.SetRandoStatusAsync(rando.Id, Models.RandoStatus.Processing);
                await GenerateAsync(rando);
                await _db.SetRandoStatusAsync(rando.Id, Models.RandoStatus.Completed);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate rando {RandoId}", rando.Id);
                await _db.SetRandoStatusAsync(rando.Id, Models.RandoStatus.Failed);
            }
        }

        public async Task GenerateAsync(ExtendedRandoDbModel rando)
        {
            var config = RandomizerConfiguration.FromJson(rando.Config!);
            _logger.LogInformation("User [{UserId}]{UserName} generatating rando ProfileId = {ProfileId} ProfileName = {ProfileName} Seed = {Seed}",
                rando.UserId, rando.UserName, rando.ProfileId, rando.ProfileName, rando.Seed);
            var result = await _randomizerService.GenerateAsync(
                (ulong)rando.Id,
                rando.ProfileName ?? "",
                rando.ProfileDescription ?? "",
                rando.UserName ?? "",
                rando.Seed,
                config);
            _logger.LogInformation("User [{UserId}]{UserName} generated rando {RandoId} ProfileId = {ProfileId} Seed = {Seed}",
                rando.UserId, rando.UserName, result.Id, rando.ProfileId, result.Seed);
        }
    }
}
