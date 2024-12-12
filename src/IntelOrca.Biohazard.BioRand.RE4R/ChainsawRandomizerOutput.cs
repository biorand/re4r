using System.Collections.Generic;
using System.Text;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using REE;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public sealed class ChainsawRandomizerOutput
    {
        private byte[]? _zipFile;
        private byte[]? _modFile;

        public RandomizerInput Input { get; }
        public PakFileBuilder PakFile { get; }
        public Dictionary<string, string> LogFiles { get; }

        internal ChainsawRandomizerOutput(RandomizerInput input, PakFileBuilder pakFile, Dictionary<string, string> logFiles)
        {
            Input = input;
            PakFile = pakFile;
            LogFiles = logFiles;
        }

        public byte[] GetOutputZip()
        {
            if (_zipFile != null)
                return _zipFile;

            _zipFile = BuildZipFile()
                .AddEntry($"re_chunk_000.pak.patch_004.pak", PakFile.ToByteArray())
                .Build();
            return _zipFile;
        }

        public byte[] GetOutputMod()
        {
            if (_modFile != null)
                return _modFile;

            var zipFile = BuildZipFile();
            foreach (var entry in PakFile.Entries)
            {
                zipFile.AddEntry(entry.Key, entry.Value);
            }
            _modFile = zipFile
                .AddEntry("pic.jpg", EmbeddedData.GetFile("modimage.jpg"))
                .AddEntry("modinfo.ini", GetModInfo())
                .Build();
            return _modFile;
        }

        private ZipFileBuilder BuildZipFile(string logPrefix = "")
        {
            var builder = new ZipFileBuilder();
            builder.AddEntry($"{logPrefix}config.json", Encoding.UTF8.GetBytes(Input.Configuration.ToJson()));
            foreach (var logFile in LogFiles)
            {
                builder.AddEntry($"{logPrefix}{logFile.Key}", Encoding.UTF8.GetBytes(logFile.Value));
            }
            return builder;
        }

        private byte[] GetModInfo()
        {
            var rf = ChainsawRandomizerFactory.Default;

            var name = $"BioRand - {Sanitize(Input.ProfileName)} [{Input.Seed}]";
            var description = SanitizeParagraph(
                $"{Sanitize(Input.ProfileName)} by {Sanitize(Input.ProfileAuthor)} [{Input.Seed}]\n" +
                Input.ProfileDescription);
            var author = "BioRand by IntelOrca & BioRand Team";
            var version = $"{rf.CurrentVersionNumber} ({rf.GitHash})";

            var lines = new[] {
                $"name={name}",
                $"version={version}",
                $"description={description}",
                "screenshot=pic.jpg",
                $"author={author}",
                "category=!Other > Misc",
                ""
            };
            var content = string.Join('\n', lines);
            return Encoding.UTF8.GetBytes(content);
        }

        private static string SanitizeParagraph(string? s)
        {
            return (s ?? "").Trim().ReplaceLineEndings("\\n");
        }

        private static string Sanitize(string? s)
        {
            return (s ?? "").Trim().ReplaceLineEndings(" ");
        }
    }
}
