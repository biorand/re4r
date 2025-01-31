using System;
using System.IO;
using IntelOrca.Biohazard.BioRand.Server.Extensions;

namespace IntelOrca.Biohazard.BioRand.Server
{
    public class BioRandServerConfiguration
    {
        public string? GamePath { get; set; }
        public UrlConfig? Url { get; set; }
        public DatabaseConfig? Database { get; set; }
        public EmailConfig? Email { get; set; }
        public TwitchConfig? Twitch { get; set; }
        public KofiConfig? Kofi { get; set; }
        public GeneratorConfig? Generator { get; set; }

        public static string GetBioRandDirectory()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var biorandHomePath = Path.Combine(homePath, ".biorand");
            return biorandHomePath;
        }

        public static string GetLogDirectory()
        {
            var biorandHomePath = GetBioRandDirectory();
            return Path.Combine(biorandHomePath, "logs");
        }

        public static BioRandServerConfiguration GetDefault()
        {
            var biorandHomePath = GetBioRandDirectory();
            var configPath = Path.Combine(biorandHomePath, "biorand-re4r.json");
            if (File.Exists(configPath))
                return FromFile(configPath);
            return new BioRandServerConfiguration();
        }

        public static BioRandServerConfiguration FromFile(string path)
        {
            return File.ReadAllText(path).DeserializeJson<BioRandServerConfiguration>();
        }
    }

    public class DatabaseConfig
    {
        public string? Path { get; set; }
    }

    public class EmailConfig
    {
        public string? From { get; set; }
        public string? Host { get; set; }
        public int Port { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string? Test { get; set; }
    }

    public class UrlConfig
    {
        public string? Api { get; set; }
        public string? Web { get; set; }
    }

    public class TwitchConfig
    {
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
    }

    public class KofiConfig
    {
        public Guid[] WebhookToken { get; set; } = [];
    }

    public class GeneratorConfig
    {
        public string[]? ApiKeys { get; set; }
        public int RandoExpireTime { get; set; }
        public int HeartbeatTimeout { get; set; }
    }
}
