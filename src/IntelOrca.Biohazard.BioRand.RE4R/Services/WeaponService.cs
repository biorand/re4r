using System.Collections.Generic;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class WeaponService
    {
        private readonly List<int> _restrictedUpgrades = [];

        public void RestrictUpgrades(int wp)
        {
            _restrictedUpgrades.Add(wp);
        }

        public bool IsRestricted(int wp) => _restrictedUpgrades.Contains(wp);
    }
}
