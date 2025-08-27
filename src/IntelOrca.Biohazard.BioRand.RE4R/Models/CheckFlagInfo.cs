using System;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R.Models
{
    internal sealed class CheckFlagInfo(RszObjectNode node)
    {
        public RszObjectNode Node => node;

        public static CheckFlagInfo Create(Guid flag)
        {
            var instance = FileRepository.RszRepository.Create("chainsaw.CheckFlagInfo");
            var result = new CheckFlagInfo(instance)
            {
                CompareValue = true,
                Flag = flag
            };
            return result;
        }

        public Guid Flag
        {
            get => node.Get<Guid>("_CheckFlag");
            set => node.Set("_CheckFlag", value);
        }

        public bool CompareValue
        {
            get => node.Get<bool>("_CompareValue");
            set => node.Set("_CompareValue", value);
        }

        public override string ToString()
        {
            return $"{Flag}";
        }
    }
}
