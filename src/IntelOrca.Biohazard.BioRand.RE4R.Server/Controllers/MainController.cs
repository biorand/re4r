using System.Reflection;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Routing;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class MainController : BaseController
    {
        private readonly RandomizerService _randomizer;

        public MainController(DatabaseService db, TwitchService twitchService, RandomizerService randomizer) : base(db, twitchService)
        {
            _randomizer = randomizer;
        }

        [Route(HttpVerbs.Get, "/")]
        public object GetApi()
        {
            var baseUrl = Request.Url.AbsoluteUri.TrimEnd('/');
            return new
            {
                Version = Assembly.GetExecutingAssembly().GetName().Version!.ToString(),
                ProfileUrl = $"{baseUrl}/profile",
                ProfileSearchUrl = $"{baseUrl}/profile/search",
                ProfileStarUrl = $"{baseUrl}/profile/{{profileId}}/star",
                ProfileDefinitionUrl = $"{baseUrl}/profile/definition",
                RandoGenerateUrl = $"{baseUrl}/rando/generate",
                RandoUrl = $"{baseUrl}/rando/{{randoId}}",
                RandoDownloadUrl = $"{baseUrl}/rando/{{randoId}}/download",
            };
        }

        [Route(HttpVerbs.Get, "/favicon.ico")]
        public async Task GetFavicon()
        {
            var content = Resources.favicon;
            Response.ContentType = "image/x-icon";
            Response.ContentEncoding = null;
            Response.ContentLength64 = content.LongLength;
            await Response.OutputStream.WriteAsync(content);
        }
    }
}
