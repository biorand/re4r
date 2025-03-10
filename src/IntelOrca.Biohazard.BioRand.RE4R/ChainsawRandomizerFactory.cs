﻿using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using RszTool;

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
        public RszFileOption RszFileOption { get; } = CreateRszFileOption();

        private ChainsawRandomizerFactory()
        {
        }

        public ChainsawRandomizer Create()
        {
            var enemyClassFactory = EnemyClassFactory.Create();
            var randomizer = new ChainsawRandomizer(enemyClassFactory);
            return randomizer;
        }

        public ScnFile ReadScnFile(byte[] data)
        {
            var scnFile = new ScnFile(RszFileOption, new FileHandler(new MemoryStream(data)));
            scnFile.Read();
            scnFile.SetupGameObjects();
            return scnFile;
        }

        public UserFile ReadUserFile(byte[] data)
        {
            var userFile = new UserFile(RszFileOption, new FileHandler(new MemoryStream(data)));
            userFile.Read();
            return userFile;
        }

        private static RszFileOption CreateRszFileOption()
        {
            var gameName = GameName.re4;

            var dataFile = Path.Combine($"rsz{gameName}.json");
            var enumFile = Path.Combine($"Enums/{gameName}_enum.json");
            var pathFile = Path.Combine($"RszPatch/rsz{gameName}_patch.json");

            var tempPath = Path.Combine(Path.GetTempPath(), "re4rr");
            Directory.CreateDirectory(tempPath);

            Ungzip(EmbeddedData.GetFile("rszre4.json.gz")!, Path.Combine(tempPath, dataFile));
            CopyFile(EmbeddedData.GetFile("re4_enum.json"), Path.Combine(tempPath, "Data", enumFile));
            CopyFile(EmbeddedData.GetFile("rszre4_patch.json"), Path.Combine(tempPath, "Data", pathFile));

            var cwd = Directory.GetCurrentDirectory();
            try
            {
                Directory.SetCurrentDirectory(tempPath);
                return new RszFileOption(gameName);
            }
            finally
            {
                Directory.SetCurrentDirectory(cwd);
            }
        }

        private static void Ungzip(byte[] input, string targetPath)
        {
            using var inputStream = new MemoryStream(input);
            using var outputStream = File.OpenWrite(targetPath);
            using var deflateStream = new GZipStream(inputStream, CompressionMode.Decompress);
            deflateStream.CopyTo(outputStream);
        }

        private static void CopyFile(byte[] input, string targetPath)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
            File.WriteAllBytes(targetPath, input);
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
