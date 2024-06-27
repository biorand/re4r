using System;
using System.Text.Json;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.RestModels;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    [ApiController]
    [Route("webhook")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class WebHookController(
        DatabaseService databaseService,
        Re4rConfiguration config,
        ILogger<WebHookController> logger) : ControllerBase
    {
        [HttpPost("kofi")]
        public async Task<object> KofiAsync([FromForm] string data)
        {
            var kofiData = JsonSerializer.Deserialize<KoFiWebHookData>(data, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });
            if (kofiData == null)
                return BadRequest();

            try
            {
                if (kofiData.VerificationToken != config.Kofi?.WebhookToken)
                {
                    logger.LogWarning("Ko-fi donation received, but verification failed {MessageId}", kofiData.MessageId);
                    return BadRequest();
                }

                var email = kofiData.Email?.ToLowerInvariant();
                var userId = null as int?;
                if (email != null)
                {
                    try
                    {
                        userId = await databaseService.FindKofiMatchAsync(email);
                        if (userId != null)
                        {
                            var user = await databaseService.GetUserAsync(userId.Value);
                            logger.LogInformation("Matched user {UserId}[{UserName}] for ko-fi donation {MessageId}", user.Id, user.Name, kofiData.MessageId);
                        }
                        else
                        {
                            logger.LogInformation("Unable to find matching email {Email} for ko-fi donation {MessageId}", kofiData.Email, kofiData.MessageId);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to find matching user to ko-fi donation.");
                    }
                }
                else
                {
                    logger.LogInformation("Ko-fi donation had no email {MessageId}", kofiData.MessageId);
                }

                var kofiModel = new KofiDbModel
                {
                    MessageId = kofiData.MessageId,
                    UserId = userId,
                    Email = email,
                    Timestamp = kofiData.Timestamp,
                    Price = decimal.Parse(kofiData.Amount),
                    TierName = kofiData.TierName,
                    Data = data
                };
                await databaseService.CreateKofiAsync(kofiModel);
                logger.LogInformation("Ko-fi donation received {MessageId}", kofiData.MessageId);
                return Empty;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ko-fi donation web hook failed {MessageId}", kofiData.MessageId);
                throw;
            }
        }
    }
}
