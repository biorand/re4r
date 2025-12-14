using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal readonly struct SceneHierachyPath
    {
        public ImmutableArray<string> Hierachy { get; }

        public IReadOnlyList<string> Folders => Hierachy.SkipLast(1).ToImmutableArray();
        public string Name => Hierachy.Last();

        public SceneHierachyPath(string path)
        {
            Hierachy = path.Split('/').ToImmutableArray();
        }

        public override string ToString() => string.Join('/', Hierachy);

        public static implicit operator SceneHierachyPath(string path) => new(path);
    }
}
