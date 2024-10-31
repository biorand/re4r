using System;
using System.Collections.Concurrent;
using System.IO;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using MsgTool;
using REE;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class FileRepository
    {
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

        public byte[]? GetGameFileData(string path)
        {
            if (_outputFiles.TryGetValue(path, out var data))
                return data;

            if (_inputGamePath == null)
            {
                return _inputPakFile?.GetFileData(path);
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
            builder.Save(path, REE.PakFlags.ZSTD);
        }

        public ScnFile GetScnFile(string path)
        {
            var data = GetGameFileData(path);
            return data == null
                ? throw new Exception("Unable to read data file.")
                : ChainsawRandomizerFactory.Default.ReadScnFile(data);
        }

        public void ModifyScnFile(string path, Action<ScnFile> callback)
        {
            var scnFile = GetScnFile(path);
            callback(scnFile);
            SetScnFile(path, scnFile);
        }

        public UserFile GetUserFile(string path)
        {
            var data = GetGameFileData(path);
            return data == null
                ? throw new Exception("Unable to read data file.")
                : ChainsawRandomizerFactory.Default.ReadUserFile(data);
        }

        public T DeserializeUserFile<T>(string path)
        {
            var userFile = GetUserFile(path);
            return userFile.RSZ!.RszParser.Deserialize<T>(userFile.RSZ.ObjectList[0]);
        }

        public void SetScnFile(string path, ScnFile value)
        {
            SetGameFileData(path, value.ToByteArray());
        }

        public void SetUserFile(string path, UserFile value)
        {
            SetGameFileData(path, value.ToByteArray());
        }

        public void ModifyUserFile(string path, Action<RSZFile, RszInstance> callback)
        {
            var userFile = GetUserFile(path);
            callback(userFile.RSZ!, userFile.RSZ!.ObjectList[0]);
            SetUserFile(path, userFile);
        }

        public Msg GetMsgFile(string path)
        {
            return new Msg(GetGameFileData(path));
        }

        public void SetMsgFile(string path, Msg msg)
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
