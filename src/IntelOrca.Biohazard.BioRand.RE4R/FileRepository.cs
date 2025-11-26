using System;
using System.Collections.Concurrent;
using System.IO;
using IntelOrca.Biohazard.REE.Package;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class FileRepository : IPatchContext, IDisposable
    {
        private static RszTypeRepository? _rszRepository;

        public static RszTypeRepository RszRepository
        {
            get
            {
                if (_rszRepository == null)
                {
                    var rszJson = EmbeddedData.GetFile("rszre4.json.gz");
                    _rszRepository = RszRepositorySerializer.Default.FromJsonGz(rszJson);
                }
                return _rszRepository;
            }
        }

        public RszTypeRepository TypeRepository => RszRepository;

        private readonly ChainsawRandomizer? _randomizer;
        private readonly PatchedPakFile? _inputPakFile;
        private readonly string? _inputGamePath;
        private ConcurrentDictionary<string, byte[]> _outputFiles = new(StringComparer.OrdinalIgnoreCase);

        public DynamicData DynamicData { get; } = new DynamicData(download: false);

        public FileRepository()
        {
        }

        public FileRepository(PatchedPakFile inputPakFile, DynamicData dynamicData)
        {
            _inputPakFile = inputPakFile;
            DynamicData = dynamicData;
        }

        public FileRepository(ChainsawRandomizer randomizer, string inputGamePath, DynamicData dynamicData)
        {
            _randomizer = randomizer;
            if (inputGamePath.EndsWith(".pak", System.StringComparison.OrdinalIgnoreCase))
            {
                _inputPakFile = new PatchedPakFile(inputGamePath);
            }
            else
            {
                _inputGamePath = inputGamePath;
            }
            DynamicData = dynamicData;
        }

        public void Dispose()
        {
            _inputPakFile?.Dispose();
        }

        public byte[] GetSupplementFile(string path)
        {
            return EmbeddedData.GetFile(path);
        }

        public byte[]? GetFile(string path)
        {
            if (_outputFiles.TryGetValue(path, out var data))
                return data;

            if (_inputGamePath == null)
            {
                return _inputPakFile?.GetEntryData(path);
            }
            else
            {
                var fullPath = Path.Combine(_inputGamePath, path);
                if (File.Exists(fullPath))
                {
                    return File.ReadAllBytes(fullPath);
                }
                return null;
            }
        }

        public void SetFile(string path, byte[] data)
        {
            _outputFiles[path] = data;
        }

        public void WriteOutputPakFile(string path)
        {
            var builder = new PakFileBuilder();
            foreach (var outputFile in _outputFiles)
            {
                builder.AddEntry(outputFile.Key, outputFile.Value);
            }
            builder.Save(path, CompressionKind.Zstd);
        }

        public PakFileBuilder GetOutputPakFile()
        {
            var builder = new PakFileBuilder();
            foreach (var outputFile in _outputFiles)
            {
                builder.AddEntry(outputFile.Key, outputFile.Value);
            }
            return builder;
        }

        public void WriteOutputFolder(string path)
        {
            foreach (var outputFile in _outputFiles)
            {
                var fullPath = Path.Combine(path, outputFile.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
                File.WriteAllBytes(fullPath, outputFile.Value);
            }
        }

        public T? GetConfigOption<T>(string key, T? defaultValue = default)
        {
            var randomizer = _randomizer;
            return randomizer == null ? defaultValue : randomizer.GetConfigOption(key, defaultValue);
        }
    }
}
