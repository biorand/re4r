using System.Collections.Immutable;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class EnemyFieldDefinition(string name, ImmutableArray<object> values)
    {
        public string Name { get; } = name;
        public ImmutableArray<object> Values { get; } = values;

        public override string ToString()
        {
            if (Values.Length == 1)
                return $"{Name} = {Values[0]}";
            else
                return $"{Name} = {{{Values.Length} possible values}}";
        }
    }
}
