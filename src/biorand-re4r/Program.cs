using System.Reflection;
using IntelOrca.Biohazard.BioRand.RE4R.Commands;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class Program
    {
        public static int Main(string[] args)
        {
            // return DebugCode();
            var app = new CommandApp();
            app.Configure(config =>
            {
                config.PropagateExceptions();
                config.Settings.ApplicationName = "biorand-re4r";
                config.Settings.ApplicationVersion = GetVersion();
                config.AddCommand<AgentCommand>("agent")
                    .WithDescription("Runs a remote generator agent for generating randos")
                    .WithExample("agent", "localhost:8080", "-k", "nCF6UaetQJJ053QLwhXqUGR68U85Rcia", "-i", "input.pak");
                config.AddCommand<GenerateCommand>("generate")
                    .WithDescription("Generates a new rando")
                    .WithExample("generate", "-o", "re_chunk_000.pak.patch_006.pak", "--seed", "35825", "--config", "tough.json");
                config.AddCommand<SetupCommand>("setup")
                    .WithDescription("Create a mini pak containing all the required vanilla assets.")
                    .WithExample("setup", "-o", "custom.pak", "-i", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RESIDENT EVIL 4  BIOHAZARD RE4");
                config.AddCommand<UpdateCommand>("update")
                    .WithDescription("Updates csv file(s).")
                    .WithExample("update");
                config.AddCommand<ModCommand>("mod")
                    .WithDescription("Export one or more standalone mods or combine them into a super mod. Run with no arguments to display available mods.")
                    .WithExample("mod", "-m", "flamethrower", "-o", "mods", "-i", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RESIDENT EVIL 4  BIOHAZARD RE4")
                    .WithExample("mod", "-m", "flamethrower", "-o", "supermod.zip", "-i", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\RESIDENT EVIL 4  BIOHAZARD RE4");
            });
            return app.Run(args);
        }

        private static string GetVersion()
        {
            return GetGitHash();
        }

        private static string GetGitHash()
        {
            var assembly = Assembly.GetExecutingAssembly();
            if (assembly == null)
                return string.Empty;

            var attribute = assembly
                .GetCustomAttributes<AssemblyInformationalVersionAttribute>()
                .FirstOrDefault();
            if (attribute == null)
                return string.Empty;

            var rev = attribute.InformationalVersion;
            var plusIndex = rev.IndexOf('+');
            if (plusIndex != -1)
            {
                return rev.Substring(plusIndex + 1);
            }
            return rev;
        }
    }
}
