using System;
using System.Collections.Concurrent;
using System.IO;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.REE.Messages;
using IntelOrca.Biohazard.REE.Package;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class FileRepository : IDisposable
    {
        private static IntelOrca.Biohazard.REE.Rsz.RszTypeRepository? _rszRepository;

        public static IntelOrca.Biohazard.REE.Rsz.RszTypeRepository RszRepository
        {
            get
            {
                if (_rszRepository == null)
                {
                    var rszJson = EmbeddedData.GetFile("rszre4.json.gz");
                    _rszRepository = IntelOrca.Biohazard.REE.Rsz.RszRepositorySerializer.Default.FromJsonGz(rszJson);
                }
                return _rszRepository;
            }
        }

        private readonly PatchedPakFile? _inputPakFile;
        private readonly string? _inputGamePath;
        private ConcurrentDictionary<string, byte[]> _outputFiles = new(StringComparer.OrdinalIgnoreCase);

        public FileRepository()
        {
        }

        public FileRepository(PatchedPakFile inputPakFile)
        {
            _inputPakFile = inputPakFile;
        }

        public FileRepository(string inputGamePath)
        {
            if (inputGamePath.EndsWith(".pak", System.StringComparison.OrdinalIgnoreCase))
            {
                _inputPakFile = new PatchedPakFile(inputGamePath);
            }
            else
            {
                _inputGamePath = inputGamePath;
            }
        }

        public void Dispose()
        {
            _inputPakFile?.Dispose();
        }

        public bool Exists(string path) => GetGameFileData(path) != null;

        public byte[]? GetGameFileData(string path)
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

        public void SetGameFileData(string path, ReadOnlyMemory<byte> data) => SetGameFileData(path, data.ToArray());

        public void SetGameFileData(string path, byte[] data)
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

        public ScnFile GetScnFile(string path)
        {
            var data = GetGameFileData(path);
            return data == null
                ? throw new Exception("Unable to read data file.")
                : ChainsawRandomizerFactory.Default.ReadScnFile(data);
        }

        public IntelOrca.Biohazard.REE.Rsz.ScnFile GetScnFile2(string path)
        {
            var data = GetGameFileData(path);
            return data == null
                ? throw new Exception("Unable to read data file.")
                : new IntelOrca.Biohazard.REE.Rsz.ScnFile(20, data);
        }

        public void ModifyScnFile(string path, Action<ScnFile> callback)
        {
            var scnFile = GetScnFile(path);
            callback(scnFile);
            SetScnFile(path, scnFile);
        }

        public void SetScnFile(string path, ScnFile value)
        {
            SetGameFileData(path, value.ToByteArray());
        }

        public void SetScnFile2(string path, IntelOrca.Biohazard.REE.Rsz.ScnFile value)
        {
            SetGameFileData(path, value.Data);
        }

        public IntelOrca.Biohazard.REE.Rsz.UserFile GetUserFile(string path)
        {
            var data = GetGameFileData(path);
            return data == null
                ? throw new Exception("Unable to read data file.")
                : new IntelOrca.Biohazard.REE.Rsz.UserFile(data);
        }

        public T DeserializeUserFile<T>(string path)
        {
            var userFile = GetUserFile(path);
            return IntelOrca.Biohazard.REE.Rsz.RszSerializer.Deserialize<T>(userFile.GetObjects(RszRepository)[0])!;
        }

        public void SerializeUserFile<T>(string path, T value)
        {
            var userFile = GetUserFile(path);
            var builder = userFile.ToBuilder(RszRepository);
            var targetType = ((IntelOrca.Biohazard.REE.Rsz.RszStructNode)builder.Objects[0]).Type;
            builder.Objects = [IntelOrca.Biohazard.REE.Rsz.RszSerializer.Serialize(targetType, value!)];
            SetUserFile(path, builder.Build());
        }

        public void SetUserFile(string path, IntelOrca.Biohazard.REE.Rsz.UserFile value)
        {
            SetGameFileData(path, value.Data);
        }

        public void ModifyUserFile(string path, Func<IntelOrca.Biohazard.REE.Rsz.RszStructNode, IntelOrca.Biohazard.REE.Rsz.RszStructNode> callback)
        {
            var userFile = GetUserFile(path);
            var builder = userFile.ToBuilder(RszRepository);
            builder.Objects = [callback((IntelOrca.Biohazard.REE.Rsz.RszStructNode)builder.Objects[0])];
            SetUserFile(path, builder.Build());
        }

        public MsgFile GetMsgFile(string path)
        {
            return new MsgFile(GetGameFileData(path));
        }

        public void SetMsgFile(string path, MsgFile msg)
        {
            SetGameFileData(path, msg.Data.ToArray());
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
    }
}
