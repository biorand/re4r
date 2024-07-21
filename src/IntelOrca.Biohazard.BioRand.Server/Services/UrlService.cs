namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public class UrlService
    {
        private readonly UrlConfig? _config;

        public UrlService(BioRandServerConfiguration config)
        {
            _config = config.Url;
        }

        public string GetApiUrl(string path)
        {
            var baseUrl = _config?.Api ?? "";
            if (baseUrl.EndsWith("/"))
                return $"{baseUrl}{path}";
            return $"{baseUrl}/{path}";
        }

        public string GetWebUrl(string path)
        {
            var baseUrl = _config?.Web ?? "";
            if (baseUrl.EndsWith("/"))
                return $"{baseUrl}{path}";
            return $"{baseUrl}/{path}";
        }
    }
}
