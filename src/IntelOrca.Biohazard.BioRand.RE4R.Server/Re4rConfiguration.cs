using System;
using System.IO;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class Re4rConfiguration
    {
        public string? GamePath { get; set; }
        public DatabaseConfig? Database { get; set; }
        public EmailConfig? Email { get; set; }
        public string[]? Passwords { get; set; }

        public static Re4rConfiguration GetDefault()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var biorandHomePath = Path.Combine(homePath, ".biorand");
            var configPath = Path.Combine(biorandHomePath, "biorand-re4r.json");
            if (File.Exists(configPath))
                return FromFile(configPath);
            return new Re4rConfiguration();
        }

        public static Re4rConfiguration FromFile(string path)
        {
            return File.ReadAllText(path).DeserializeJson<Re4rConfiguration>();
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
    }
}
