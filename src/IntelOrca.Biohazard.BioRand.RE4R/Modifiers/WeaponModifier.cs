using System.Collections.Generic;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class WeaponModifier : Modifier
    {
        private const string WeaponCustomUserDataPath = "natives/stm/_chainsaw/appsystem/weaponcustom/weaponcustomuserdata.user.2";
        private const string WeaponDetailCustomUserDataPath = "natives/stm/_chainsaw/appsystem/weaponcustom/weapondetailcustomuserdata.user.2";

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var mainFile = randomizer.FileRepository.GetUserFile(WeaponCustomUserDataPath);
            var detailFile = randomizer.FileRepository.GetUserFile(WeaponDetailCustomUserDataPath);

            var metaRoot = mainFile.RSZ!.ObjectList[0];
            var dataRoot = detailFile.RSZ!.ObjectList[0];

            var stats = new List<WeaponStat>();
            var def = Resources.stats.DeserializeJson<WeaponStatsDefinition>();
            foreach (var d in def.Upgrades)
            {
                var costs = new Dictionary<(int, int), int>();
                var values = new Dictionary<(int, int), object>();

                for (var w = 0; w < 100; w++)
                {
                    for (var i = 0; i < 10; i++)
                    {
                        for (var l = 1; l < 10; l++)
                        {
                            var metaWeaponId = metaRoot.Get<int?>(ProcessPath(d.MetaWeaponId, w, i, l), relaxed: true);
                            var metaCategory = metaRoot.Get<int?>(ProcessPath(d.MetaCategory, w, i, l), relaxed: true);
                            var metaCost = metaRoot.Get<int?>(ProcessPath(d.Meta, w, i, l) + "._Cost", relaxed: true);
                            if (metaWeaponId != null && metaCategory == d.Category && metaCost != null)
                            {
                                costs[(metaWeaponId.Value, l)] = metaCost.Value;
                            }

                            var dataWeaponId = dataRoot.Get<int?>(ProcessPath(d.DataWeaponId, w, i, l), relaxed: true);
                            var dataCategory = dataRoot.Get<int?>(ProcessPath(d.DataCategory, w, i, l), relaxed: true);
                            var data = dataRoot.Get(ProcessPath(d.Data, w, i, l), relaxed: true);
                            if (dataWeaponId != null && dataCategory == d.Category && data != null)
                            {
                                values[(dataWeaponId.Value, l)] = data;
                            }
                        }
                    }
                }

                foreach (var kvp in costs)
                {
                    var metaWeaponId = kvp.Key.Item1;
                    var level = kvp.Key.Item2;
                    var metaCost = kvp.Value;
                    if (values.TryGetValue((metaWeaponId, level), out var metaValue))
                    {
                        stats.Add(new WeaponStat(d.Name, metaWeaponId, level, metaCost, metaValue));
                    }
                }
            }

            foreach (var weaponId in stats.GroupBy(x => x.WeaponId))
            {
                logger.Push($"Weapon {weaponId.Key}");
                foreach (var upgradeName in weaponId.GroupBy(x => x.Name))
                {
                    logger.Push(upgradeName.Key);
                    foreach (var stat in upgradeName.OrderBy(x => x.Level))
                    {
                        logger.LogLine($"Level = {stat.Level} Cost = {stat.Cost} Value = {stat.Value}");
                    }
                    logger.Pop();
                }
                logger.Pop();
            }
        }

        private static string ProcessPath(string xpath, int w, int i, int l)
        {
            return xpath
                .Replace("{W}", w.ToString())
                .Replace("{I}", i.ToString())
                .Replace("{L}", l.ToString());
        }
    }

    public class WeaponStatsDefinition
    {
        public WeaponUpgradeDefinition[] Upgrades { get; set; } = [];
    }

    public class WeaponUpgradeDefinition
    {
        public string Name { get; set; } = "";
        public int Category { get; set; }
        public string MetaWeaponId { get; set; } = "";
        public string DataWeaponId { get; set; } = "";
        public string MetaCategory { get; set; } = "";
        public string DataCategory { get; set; } = "";
        public string Meta { get; set; } = "";
        public string Data { get; set; } = "";
    }

    public record WeaponStat(string Name, int WeaponId, int Level, int Cost, object Value)
    {
    }
}
