namespace REE
{
    public sealed class PakList
    {
        private readonly Dictionary<ulong, string> _map = [];

        public PakList(string contents)
            : this(contents.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
        }

        public PakList(string[] files)
        {
            foreach (var line in files)
            {
                var path = line.Trim();
                if (!string.IsNullOrEmpty(line))
                {
                    _map[GetHash(path)] = path;
                }
            }
        }

        public string? GetPath(ulong hash)
        {
            _map.TryGetValue(hash, out var path);
            return path;
        }

        private static ulong GetHash(string path)
        {
            var dwHashLower = (ulong)PakHash.iGetHash(path.ToLower());
            var dwHashUpper = (ulong)PakHash.iGetHash(path.ToUpper());
            return dwHashLower | (dwHashUpper << 32);
        }
    }
}
