using System;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Models
{
    internal class FlagCondition(RszStructNode node)
    {
        public RszStructNode Node => node;

        public bool Or
        {
            get => node.Get<int>("_Logic") == 1;
            set => node.Set("_Logic", value ? 1 : 0);
        }

        public ImmutableArray<CheckFlagInfo> Flags
        {
            get
            {
                return node.Get<RszArrayNode>("_CheckFlags")
                    .Select(x => new CheckFlagInfo((RszStructNode)x))
                    .ToImmutableArray();
            }
            set
            {
                node.Set("_CheckFlags", value.Select(x => (object)x.Node).ToList());
            }
        }

        public void Clear()
        {
            Flags = [];
        }

        public void Add(Guid guid)
        {
            Flags = Flags.Add(CheckFlagInfo.Create(guid));
        }

        public override string ToString()
        {
            return string.Join(Or ? " || " : " && ", Flags);
        }
    }
}
