using System;
using System.Linq;
using System.Reflection;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ChainsawRandomizerFactory
    {
        public static ChainsawRandomizerFactory Default { get; } = new ChainsawRandomizerFactory();

        private static Assembly CurrentAssembly => Assembly.GetExecutingAssembly();
        public Version CurrentVersion { get; } = GetCurrentVersion();
        public string CurrentVersionNumber => $"{CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build}";
        public string CurrentVersionInfo => $"BioRand for RE4R {CurrentVersion.Major}.{CurrentVersion.Minor}.{CurrentVersion.Build} ({GitHash})";
        public string GitHash { get; } = GetGitHash();

        private ChainsawRandomizerFactory()
        {
        }

        private static Version GetCurrentVersion()
        {
            var version = CurrentAssembly?.GetName().Version ?? new Version();
            if (version.Revision == -1)
                return version;
            return new Version(version.Major, version.Minor, version.Build);
        }

        private static string GetGitHash()
        {
            var assembly = CurrentAssembly;
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

        public static byte[] GetDefaultProfile() => EmbeddedData.GetFile("default-profile.json");
    }
}
