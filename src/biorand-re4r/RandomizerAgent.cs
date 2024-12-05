using System.Text;
using System.Text.Json;
using IntelOrca.Biohazard.BioRand.Server.Models;

namespace IntelOrca.Biohazard.BioRand
{
    public class RandomizerAgent
    {
        private readonly HttpClient _httpClient = new();
        private readonly JsonSerializerOptions _options = new JsonSerializerOptions()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        private bool _generating;
        private TimeSpan _pollTime = TimeSpan.FromSeconds(10);
        private readonly IRandomizerAgentHandler _handler;

        public IRandomizer Randomizer => _handler.Randomizer;
        public string BaseUri { get; }
        public string ApiKey { get; }
        public int GameId { get; }
        public Guid Id { get; private set; }
        public string Status { get; private set; } = "Idle";

        public RandomizerAgent(string baseUri, string apiKey, int gameId, IRandomizerAgentHandler handler)
        {
            BaseUri = baseUri;
            ApiKey = apiKey;
            GameId = gameId;
            _handler = handler;

            _httpClient.DefaultRequestHeaders.Add("X-API-KEY", ApiKey);
        }

        public async Task RunAsync(CancellationToken ct = default)
        {
            await RegisterAsync();
            while (!ct.IsCancellationRequested)
            {
                await SendStatusAsync();
                if (!_generating)
                {
                    await ProcessNextRandomizer();
                }
                await Task.Delay(_pollTime);
            }
        }

        private async Task RegisterAsync()
        {
            var response = await PostAsync<RegisterResponse>("generator/register", new
            {
                GameId,
                Randomizer.ConfigurationDefinition,
                Randomizer.DefaultConfiguration,
            });
            Id = response.Id;
        }

        private async Task SendStatusAsync()
        {
            try
            {
                await PutAsync<object>("generator/heartbeat", new
                {
                    Id,
                    Status
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task ProcessNextRandomizer()
        {
            try
            {
                var queue = await GetAsync<QueueResponseItem[]>("generator/queue");
                foreach (var q in queue)
                {
                    if (q.GameId != GameId)
                        continue;

                    if (await _handler.CanGenerateAsync(q))
                    {
                        try
                        {
                            await PostAsync<object>("generator/begin", new
                            {
                                Id,
                                RandoId = q.Id,
                                Version = Randomizer.BuildVersion
                            });
                            await BeginGeneration(q);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex);
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task BeginGeneration(QueueResponseItem q)
        {
            var randomizerInput = new RandomizerInput()
            {
                Configuration = RandomizerConfiguration.FromJson(q.Config!),
                ProfileName = q.ProfileName,
                ProfileAuthor = q.ProfileUserName,
                ProfileDescription = q.ProfileDescription,
                Seed = q.Seed
            };

            _generating = true;
            try
            {
                await Task.Run(async () =>
                {
                    try
                    {
                        var output = await _handler.GenerateAsync(q, randomizerInput);
                        await PostAsync<object>("generator/end", new
                        {
                            Id,
                            RandoId = q.Id,
                            output.PakOutput,
                            output.FluffyOutput
                        });
                    }
                    finally
                    {
                        _generating = false;
                    }
                });
            }
            catch
            {
                _generating = false;
            }
        }

        private Task<T> GetAsync<T>(string path) where T : class => SendAsync<T>(HttpMethod.Get, path)!;
        private Task<T> PostAsync<T>(string path, object data) where T : class => SendAsync<T>(HttpMethod.Post, path, data);
        private Task<T> PutAsync<T>(string path, object data) where T : class => SendAsync<T>(HttpMethod.Put, path, data);

        private async Task<T> SendAsync<T>(HttpMethod method, string path, object? data = null) where T : class
        {
            var url = GetUri(path);
            var request = new HttpRequestMessage(method, url);
            if (data != null)
            {
                var json = JsonSerializer.Serialize(data, _options);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                throw new Exception($"{response.StatusCode} returned");

            var responseContent = await response.Content.ReadAsStringAsync();
            if (responseContent.Length == 0)
                return null!;

            return JsonSerializer.Deserialize<T>(responseContent, _options)!;
        }

        private string GetUri(string path)
        {
            return $"{BaseUri}/{path}";
        }

        private class RegisterResponse
        {
            public Guid Id { get; set; }
        }

        public class QueueResponseItem
        {
            public int Id { get; set; }
            public int GameId { get; set; }
            public DateTime Created { get; set; }
            public int UserId { get; set; }
            public int Seed { get; set; }
            public int ConfigId { get; set; }
            public RandoStatus Status { get; set; }
            public UserRoleKind UserRole { get; set; }
            public string? UserName { get; set; }
            public int ProfileId { get; set; }
            public string? ProfileName { get; set; }
            public string? ProfileDescription { get; set; }
            public int ProfileUserId { get; set; }
            public string? ProfileUserName { get; set; }
            public string? Config { get; set; }
        }
    }

    public interface IRandomizerAgentHandler
    {
        IRandomizer Randomizer { get; }
        Task<bool> CanGenerateAsync(RandomizerAgent.QueueResponseItem queueItem);
        Task<RandomizerOutput> GenerateAsync(RandomizerAgent.QueueResponseItem queueItem, RandomizerInput input);
    }
}
