using System;
using System.Text.Json;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.WebApi;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class MyJsonDataAttribute : Attribute, IRequestDataAttribute<WebApiController>
    {
        public async Task<object?> GetRequestDataAsync(WebApiController controller, Type type, string parameterName)
        {
            using var req = controller.HttpContext.OpenRequestText();
            var content = await req.ReadToEndAsync();
            return JsonSerializer.Deserialize(content, type, new JsonSerializerOptions()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }
    }
}
