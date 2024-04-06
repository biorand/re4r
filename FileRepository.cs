using System.Collections.Generic;
using System.IO;
using IntelOrca.Biohazard.BioRand.RE4R.FileSystem.Package;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class FileRepository
    {
        private readonly PatchedPakFile? _inputPakFile;
        private readonly string? _inputGamePath;
        private Dictionary<string, byte[]> _outputFiles = new Dictionary<string, byte[]>();

        public FileRepository(PatchedPakFile inputPakFile)
        {
            _inputPakFile = inputPakFile;
        }

        public FileRepository(string inputGamePath)
        {
            _inputGamePath = inputGamePath;
        }

        public byte[]? GetGameFileData(string path)
        {
            if (_inputGamePath == null)
            {
                return _inputPakFile!.GetFileData(path);
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
