using System;
using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Models
{
    internal class FlagCondition(RszInstance instance)
    {
        public RszInstance Instance => instance;

        public bool Or
        {
            get => instance.Get<int>("_Logic") == 1;
            set => instance.Set("_Logic", value ? 1 : 0);
        }

        public ImmutableArray<CheckFlagInfo> Flags
        {
            get
            {
                return instance.GetList("_CheckFlags")
                    .Select(x => new CheckFlagInfo((RszInstance)x!))
                    .ToImmutableArray();
            }
            set
            {
                instance.Set("_CheckFlags", value.Select(x => (object)x.Instance).ToList());
            }
        }

        public void Clear()
        {
            Flags = [];
        }

        public void Add(ScnFile scn, Guid guid)
        {
            Flags = Flags.Add(CheckFlagInfo.Create(scn, guid));
        }

        public override string ToString()
        {
            return string.Join(Or ? " || " : " && ", Flags);
        }
    }
}
