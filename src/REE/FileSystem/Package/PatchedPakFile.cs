using System.Text.RegularExpressions;

namespace REE
{
    public class PatchedPakFile
    {
        private readonly PakFile[] _files;

        public PatchedPakFile(string path)
        {
            var files = new List<PakFile>();

            var match = Regex.Match(path, @"(^.*)\.patch_([0-9]{3})\.pak$");
            if (match.Success)
            {
                var basePath = match.Groups[1].Value;
                var endNumber = int.Parse(match.Groups[2].Value);
                files.Add(new PakFile(basePath));
                for (var i = 1; i <= endNumber; i++)
                {
                    var patchFileName = $"{basePath}.patch_{i:000}.pak";
                    files.Add(new PakFile(patchFileName));
                }
            }
            else
            {
                files.Add(new PakFile(path));
                for (var i = 1; i < 10000; i++)
                {
                    var patchFileName = $"{path}.patch_{i:000}.pak";
                    if (File.Exists(patchFileName))
                    {
                        files.Add(new PakFile(patchFileName));
                    }
                    else
                    {
                        break;
                    }
                }
            }

            _files = files.ToArray();
        }

        public byte[]? GetFileData(string path)
        {
            for (var i = _files.Length - 1; i >= 0; i--)
            {
                var data = _files[i].GetFileData(path);
                if (data != null)
                {
                    return data;
                }
            }
            return null;
        }
    }
}
