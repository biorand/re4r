using System;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Models
{
    internal sealed class CheckFlagInfo(RszInstance instance)
    {
        public RszInstance Instance => instance;

        public static CheckFlagInfo Create(ScnFile scn, Guid flag)
        {
            var instance = scn.RSZ!.CreateInstance("chainsaw.CheckFlagInfo");
            var result = new CheckFlagInfo(instance)
            {
                CompareValue = true,
                Flag = flag
            };
            return result;
        }

        public Guid Flag
        {
            get => instance.Get<Guid>("_CheckFlag");
            set => instance.Set("_CheckFlag", value);
        }

        public bool CompareValue
        {
            get => instance.Get<bool>("_CompareValue");
            set => instance.Set("_CompareValue", value);
        }

        public override string ToString()
        {
            return $"{Flag}";
        }
    }
}
