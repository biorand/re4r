using System;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class WeaponChoice
    {
        public WeaponDefinition? Primary { get; }
        public WeaponDefinition? Secondary { get; }

        public WeaponChoice(WeaponDefinition? primary = null, WeaponDefinition? secondary = null)
        {
            if (primary == null && secondary != null)
                throw new ArgumentNullException(nameof(primary));

            Primary = primary;
            Secondary = secondary;
        }

        public bool IsNone => Primary == null;

        public override string ToString()
        {
            if (Primary == null)
                return "(none)";
            if (Secondary == null)
                return $"{Primary}";
            return $"{Primary}, {Secondary}";
        }
    }
}
