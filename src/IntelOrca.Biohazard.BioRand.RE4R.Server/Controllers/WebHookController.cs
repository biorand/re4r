using System;
using System.Collections.Specialized;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Serilog;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class WebHookController(DatabaseService databaseService, TwitchService twitchService, KofiConfig? config)
        : BaseController(databaseService, twitchService)
    {
        private readonly DatabaseService _databaseService = databaseService;
        private readonly ILogger _logger = Log.ForContext<WebHookController>();

        [Route(HttpVerbs.Post, "/kofi")]
        public async Task<object> KofiAsync([FormData] NameValueCollection data)
        {
            var jsonData = data["data"];
            if (jsonData == null)
                return ErrorResult(HttpStatusCode.BadRequest);

            var kofiData = JsonSerializer.Deserialize<KoFiWebHookData>(jsonData, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            if (kofiData == null)
                return ErrorResult(HttpStatusCode.BadRequest);

            try
            {
                if (kofiData.VerificationToken != config?.WebhookToken)
                {
                    _logger.Warning("Ko-fi donation received, but verification failed {MessageId}", kofiData.MessageId);
                    return ErrorResult(HttpStatusCode.BadRequest);
                }

                var email = kofiData.Email?.ToLowerInvariant();
                var userId = null as int?;
                if (email != null)
                {
                    try
                    {
                        userId = await _databaseService.FindKofiMatchAsync(email);
                        if (userId != null)
                        {
                            var user = await _databaseService.GetUserAsync(userId.Value);
                            _logger.Information("Matched user {UserId}[{UserName}] for ko-fi donation {MessageId}", user.Id, user.Name, kofiData.MessageId);
                        }
                        else
                        {
                            _logger.Information("Unable to find matching email {Email} for ko-fi donation {MessageId}", kofiData.Email, kofiData.MessageId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Failed to find matching user to ko-fi donation.");
                    }
                }
                else
                {
                    _logger.Information("Ko-fi donation had no email {MessageId}", kofiData.MessageId);
                }

                var kofiModel = new KofiDbModel
                {
                    MessageId = kofiData.MessageId,
                    UserId = userId,
                    Email = email,
                    Timestamp = kofiData.Timestamp,
                    Price = decimal.Parse(kofiData.Amount),
                    TierName = kofiData.TierName,
                    Data = jsonData
                };
                await _databaseService.InsertKofiAsync(kofiModel);
                _logger.Information("Ko-fi donation received {MessageId}", kofiData.MessageId);
                return EmptyResult();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Ko-fi donation web hook failed {MessageId}", kofiData.MessageId);
                throw;
            }
        }
    }

    public class KoFiWebHookData
    {
        public Guid VerificationToken { get; set; }
        public Guid MessageId { get; set; }
        public DateTime Timestamp { get; set; }
        public string Type { get; set; } = "";
        public bool IsPublic { get; set; }
        public string? FromName { get; set; }
        public string? Message { get; set; }
        public string Amount { get; set; } = "";
        public string Url { get; set; } = "";
        public string? Email { get; set; }
        public string Currency { get; set; } = "";
        public bool IsSubscriptionPayment { get; set; }
        public bool IsFirstSubscriptionPayment { get; set; }
        public string KofiTransactionId { get; set; } = "";
        public object? ShopItems { get; set; }
        public string? TierName { get; set; }
        public object? Shipping { get; set; }
    }
}
