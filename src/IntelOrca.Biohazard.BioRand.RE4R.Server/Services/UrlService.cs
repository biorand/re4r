namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Services
{
    public class UrlService
    {
        private readonly UrlConfig? _config;

        public UrlService(Re4rConfiguration config)
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
