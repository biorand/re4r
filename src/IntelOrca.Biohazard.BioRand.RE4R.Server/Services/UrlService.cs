namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Services
{
    internal class UrlService
    {
        private readonly UrlConfig? _config;

        public UrlService(UrlConfig? config)
        {
            _config = config;
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
