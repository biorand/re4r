using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class WeaponModifier : Modifier
    {
        private const string WeaponCustomUserDataPath = "natives/stm/_chainsaw/appsystem/weaponcustom/weaponcustomuserdata.user.2";
        private const string WeaponDetailCustomUserDataPath = "natives/stm/_chainsaw/appsystem/weaponcustom/weapondetailcustomuserdata.user.2";

        private bool _randomStats;
        private bool _randomPrices;
        private Rng _priceRng = new();
        private Rng _valueRng = new();

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var stats = GetWeaponStats(randomizer);
            foreach (var weaponId in stats.GroupBy(x => x.WeaponId))
            {
                var weapon = itemRepo.FromWeaponId(weaponId.Key);
                var weaponName = weapon?.Name ?? weaponId.Key.ToString();

                logger.Push($"Weapon {weaponName}");
                foreach (var upgradeName in weaponId.GroupBy(x => x.Name))
                {
                    logger.Push(upgradeName.Key);
                    foreach (var stat in upgradeName.OrderBy(x => x.Level))
                    {
                        var value = stat.Value;
                        if (value is float f)
                            value = f.ToString("0.00");
                        logger.LogLine($"Level = {stat.Level} Cost = {stat.Cost} Info = {stat.Info} Value = {value}");
                    }
                    logger.Pop();
                }
                logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            _priceRng = randomizer.CreateRng();
            _valueRng = randomizer.CreateRng();

            _randomStats = randomizer.GetConfigOption<bool>("random-weapon-stats");
            _randomPrices = randomizer.GetConfigOption<bool>("random-weapon-upgrade-prices");
            if (!_randomStats && !_randomPrices)
                return;

            var itemRepo = ItemDefinitionRepository.Default;
            var mainFile = randomizer.FileRepository.GetUserFile(WeaponCustomUserDataPath);
            var detailFile = randomizer.FileRepository.GetUserFile(WeaponDetailCustomUserDataPath);
            var metaRoot = mainFile.RSZ!.ObjectList[0];
            var dataRoot = detailFile.RSZ!.ObjectList[0];

            var stats = GetWeaponStats(randomizer);
            var weapons = stats.GroupBy(x => x.WeaponId);
            foreach (var weaponStats in weapons)
            {
                var weaponId = weaponStats.Key;
                var weaponName = itemRepo.FromWeaponId(weaponId)?.Name ?? weaponId.ToString();
                logger.Push(weaponName);

                var statKinds = weaponStats.GroupBy(x => x.Name);
                foreach (var statKind in statKinds)
                {
                    var modifiedStats = statKind
                        .Select(x => new WeaponStatModifier(x))
                        .ToArray();

                    RandomizeStats(modifiedStats, logger);
                    foreach (var modifiedStat in modifiedStats)
                    {
                        if (modifiedStat.Stat.Level == 0)
                            continue;

                        var stat = modifiedStat.Stat;
                        var statDef = WeaponStatsDefinition.Upgrades.First(x => x.Name == statKind.Key);
                        if (_randomPrices)
                        {
                            metaRoot.Set(stat.CostPath, modifiedStat.Cost);
                        }
                        if (_randomStats)
                        {
                            metaRoot.Set(stat.InfoPath, GetInfo(stat.Name, stat.Info, stat.Value, modifiedStat.Value));
                            if (stat.Value is float)
                            {
                                dataRoot.Set(stat.ValuePath, modifiedStat.Value);
                            }
                            else if (stat.Value is int)
                            {
                                dataRoot.Set(stat.ValuePath, (int)MathF.Round(modifiedStat.Value));
                            }
                        }
                    }
                }

                logger.Pop();
            }

            randomizer.FileRepository.SetUserFile(WeaponCustomUserDataPath, mainFile);
            randomizer.FileRepository.SetUserFile(WeaponDetailCustomUserDataPath, detailFile);
        }

        private static string GetInfo(string name, string oldInfo, object oldValue, float newValue)
        {
            var infoValue = float.Parse(oldInfo);
            var infoMultiplier = infoValue / Convert.ToSingle(oldValue);
            var newInfoValue = newValue * infoMultiplier;
            var fmt = "0.00";
            if (name == "Ammo Capacity")
                fmt = "0";
            else if (name == "Durability")
                fmt = "0.0";
            else if (newInfoValue >= 10)
                fmt = "0.0";

            var result = newInfoValue.ToString(fmt);
            return result;
        }

        private void RandomizeStats(WeaponStatModifier[] stats, RandomizerLogger logger)
        {
            if (_randomStats)
            {
                var name = stats[0].Stat.Name;
                if (name == "Power")
                    RandomizePower(stats, logger);
                if (name == "Ammo Capacity")
                    RandomizeCapacity(stats, logger);
                if (name == "Reload Speed")
                    RandomizeReloadSpeed(stats, logger);
                if (name == "Rate of Fire")
                    RandomizeRateOfFire(stats, logger);
                if (name == "Durability")
                    RandomizeDurability(stats, logger);
                LogValueChange(stats, logger);
            }
            if (_randomPrices)
            {
                var priceRatio = _priceRng.NextDouble(0.5, 2);
                foreach (var stat in stats)
                {
                    stat.Cost = (stat.Cost * priceRatio).RoundPrice();
                }
            }
        }

        private static void LogValueChange(WeaponStatModifier[] stats, RandomizerLogger logger)
        {
            var name = stats[0].Stat.Name;
            var format = stats.First().Stat.Value is float ? "0.00" : "0";
            var originalMin = Convert.ToSingle(stats.First().Stat.Value).ToString(format);
            var originalMax = Convert.ToSingle(stats.Last().Stat.Value).ToString(format);
            var max = Convert.ToSingle(stats.Last().Value).ToString(format);
            logger.LogLine($"{name} {originalMin} - {originalMax} -> {max}");
        }

        private void RandomizePower(WeaponStatModifier[] stats, RandomizerLogger logger)
        {
            var originalMin = stats[0].Value;
            var originalMax = stats.Last().Value;

            var min = originalMin + 0.05f;
            var maxLower = min + 0.10f;
            var maxUpper = originalMax * 4;
            var max = Lerp(maxLower, maxUpper, MathF.Pow(_valueRng.NextFloat(), 5));

            for (var i = 1; i < stats.Length; i++)
            {
                stats[i].Value = Lerp(originalMin, max, i / (stats.Length - 1.0f));
            }
        }

        private void RandomizeCapacity(WeaponStatModifier[] stats, RandomizerLogger logger)
        {
            var originalMin = (int)stats[0].Value;
            var originalMax = (int)stats.Last().Value;

            var min = originalMin + 1;
            var maxLower = min + stats.Length;
            var maxUpper = originalMax * 2;
            var max = (int)Lerp(maxLower, maxUpper, MathF.Pow(_valueRng.NextFloat(), 5));

            for (var i = 1; i < stats.Length; i++)
            {
                stats[i].Value = Lerp(originalMin, max, i / (stats.Length - 1.0f));
            }
        }

        private void RandomizeReloadSpeed(WeaponStatModifier[] stats, RandomizerLogger logger)
        {
            var originalMin = stats[0].Value;
            var originalMax = stats.Last().Value;

            var min = originalMin + 0.05f;
            var maxLower = min + 0.10f;
            var maxUpper = originalMax * 2;
            var max = Lerp(maxLower, maxUpper, MathF.Pow(_valueRng.NextFloat(), 5));

            for (var i = 1; i < stats.Length; i++)
            {
                stats[i].Value = Lerp(originalMin, max, i / (stats.Length - 1.0f));
            }
        }

        private void RandomizeRateOfFire(WeaponStatModifier[] stats, RandomizerLogger logger)
        {
            var originalMin = stats[0].Value;
            var originalMax = stats.Last().Value;

            var min = originalMin - 0.05f;
            var maxLower = min - 0.10f;
            var maxUpper = originalMax / 2;
            var max = Lerp(maxLower, maxUpper, MathF.Pow(_valueRng.NextFloat(), 5));

            for (var i = 1; i < stats.Length; i++)
            {
                stats[i].Value = Lerp(originalMin, max, i / (stats.Length - 1.0f));
            }
        }

        private void RandomizeDurability(WeaponStatModifier[] stats, RandomizerLogger logger)
        {
            var originalMin = (int)stats[0].Value;
            var originalMax = (int)stats.Last().Value;

            var min = originalMin + 100;
            var maxLower = min + stats.Length;
            var maxUpper = originalMax * 2;
            var max = (int)Lerp(maxLower, maxUpper, MathF.Pow(_valueRng.NextFloat(), 5));

            for (var i = 1; i < stats.Length; i++)
            {
                var val = Lerp(originalMin, max, i / (stats.Length - 1.0f));
                stats[i].Value = (int)MathF.Ceiling(val / 100) * 100;
            }
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + ((b - a) * t);
        }

        private ImmutableArray<WeaponStat> GetWeaponStats(ChainsawRandomizer randomizer)
        {
            var mainFile = randomizer.FileRepository.GetUserFile(WeaponCustomUserDataPath);
            var detailFile = randomizer.FileRepository.GetUserFile(WeaponDetailCustomUserDataPath);
            var metaRoot = mainFile.RSZ!.ObjectList[0];
            var dataRoot = detailFile.RSZ!.ObjectList[0];

            var stats = new List<WeaponStat>();
            var def = WeaponStatsDefinition;
            foreach (var d in def.Upgrades)
            {
                var costs = new Dictionary<(int, int), (string, int)>();
                var infos = new Dictionary<(int, int), (string, string)>();
                var values = new Dictionary<(int, int), (string, object)>();

                for (var w = 0; w < 100; w++)
                {
                    for (var i = 0; i < 10; i++)
                    {
                        for (var l = 0; l < 10; l++)
                        {
                            var metaWeaponId = metaRoot.Get<int?>(ProcessPath(d.MetaWeaponId, w, i, l), relaxed: true);
                            var metaCategory = metaRoot.Get<int?>(ProcessPath(d.MetaCategory, w, i, l), relaxed: true);
                            var metaCostPath = ProcessPath(d.Meta, w, i, l) + "._Cost";
                            var metaCost = metaRoot.Get<int?>(metaCostPath, relaxed: true);
                            var metaInfoPath = ProcessPath(d.Meta, w, i, l) + "._Info";
                            var metaInfo = metaRoot.Get<string?>(metaInfoPath, relaxed: true);
                            if (metaWeaponId != null && metaCategory == d.Category && metaCost != null)
                            {
                                costs[(metaWeaponId.Value, l)] = (metaCostPath, metaCost.Value);
                                infos[(metaWeaponId.Value, l)] = (metaInfoPath, metaInfo ?? "");
                            }

                            var dataWeaponId = dataRoot.Get<int?>(ProcessPath(d.DataWeaponId, w, i, l), relaxed: true);
                            var dataCategory = dataRoot.Get<int?>(ProcessPath(d.DataCategory, w, i, l), relaxed: true);
                            var dataPath = ProcessPath(d.Data, w, i, l);
                            var data = dataRoot.Get(dataPath, relaxed: true);
                            if (dataWeaponId != null && dataCategory == d.Category && data != null)
                            {
                                values[(dataWeaponId.Value, l)] = (dataPath, data);
                            }
                        }
                    }
                }

                foreach (var kvp in costs)
                {
                    var metaWeaponId = kvp.Key.Item1;
                    var level = kvp.Key.Item2;
                    var metaCost = kvp.Value;
                    infos.TryGetValue((metaWeaponId, level), out var metaInfo);
                    if (values.TryGetValue((metaWeaponId, level), out var value))
                    {
                        if (metaCost.Item2 < 0)
                            metaCost.Item2 = 0;
                        if (d.Name == "Ammo Capacity" || d.Name == "Durability")
                        {
                            if ((int)value.Item2 < 0)
                                value.Item2 = int.Parse(metaInfo.Item2);
                        }
                        else
                        {
                            if ((float)value.Item2 < 0)
                                value.Item2 = 1.0f;
                        }
                        stats.Add(new WeaponStat(
                            d.Name,
                            metaWeaponId,
                            level,
                            metaCost.Item2,
                            metaInfo.Item2 ?? "",
                            value.Item2,
                            metaCost.Item1,
                            metaInfo.Item1,
                            value.Item1));
                    }
                }
            }

            return [.. stats];
        }

        private static string ProcessPath(string xpath, int w, int i, int l)
        {
            return xpath
                .Replace("{W}", w.ToString())
                .Replace("{I}", i.ToString())
                .Replace("{L}", l.ToString());
        }

        private static WeaponStatsDefinition WeaponStatsDefinition { get; } = Resources.stats.DeserializeJson<WeaponStatsDefinition>();
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

    public record WeaponStat(
        string Name,
        int WeaponId,
        int Level,
        int Cost,
        string Info,
        object Value,
        string CostPath,
        string InfoPath,
        string ValuePath)
    {
    }

    public class WeaponStatModifier(WeaponStat stat)
    {
        public WeaponStat Stat => stat;
        public int Cost { get; set; } = stat.Cost;
        public float Value { get; set; } = Convert.ToSingle(stat.Value);
    }
}
