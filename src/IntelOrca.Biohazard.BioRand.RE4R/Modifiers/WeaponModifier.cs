using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using MsgTool;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class WeaponModifier : Modifier
    {
        private const string WeaponCustomUserDataPath = "natives/stm/_chainsaw/appsystem/weaponcustom/weaponcustomuserdata.user.2";
        private const string WeaponDetailCustomUserDataPath = "natives/stm/_chainsaw/appsystem/weaponcustom/weapondetailcustomuserdata.user.2";
        private const string ItemDefinitionUserDataPath = "natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2";
        private const string WeaponCustomMsgPath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_wpcustom.msg.22";

        private bool _randomStats;
        private bool _randomPrices;
        private bool _randomExclusives;
        private Rng _priceRng = new();
        private Rng _valueRng = new();
        private Rng _exclusiveRng = new();
        private Msg.Builder _msg = new();

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var stats = GetWeaponStats(randomizer);
            foreach (var weaponId in stats.GroupBy(x => x.WeaponId))
            {
                var weapon = itemRepo.FromWeaponId(weaponId.Key);
                var weaponName = weapon?.Name ?? weaponId.Key.ToString();

                logger.Push($"Weapon {weaponName}");
                foreach (var upgradeName in weaponId.OfType<WeaponUpgradeStat>().GroupBy(x => x.Name))
                {
                    logger.Push(upgradeName.Key);
                    foreach (var stat in upgradeName.OrderBy(x => x.Level))
                    {
                        var value = stat.Value;
                        if (value.Value is float f)
                            value = value.WithValue(f.ToString("0.00"));
                        logger.LogLine($"Level = {stat.Level} Cost = {stat.Cost} Info = {stat.Info} Value = {value}");
                    }
                    logger.Pop();
                }

                // Exclusive
                var exclusives = weaponId.OfType<WeaponExclusiveStat>().ToArray();
                if (exclusives.Length > 0)
                {
                    logger.Push($"Exclusive: {exclusives[0].Name} Cost = {exclusives[0].Cost}");
                    foreach (var exclusive in weaponId.OfType<WeaponExclusiveStat>())
                    {
                        logger.LogLine($"Field = {GetFieldName(exclusive.Value.Path)} Value = {exclusive.Value.Value}");
                    }
                    logger.Pop();
                }

                logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var rng = randomizer.CreateRng();
            _priceRng = rng.NextFork();
            _valueRng = rng.NextFork();
            _exclusiveRng = rng.NextFork();

            _randomStats = randomizer.GetConfigOption<bool>("random-weapon-stats");
            _randomPrices = randomizer.GetConfigOption<bool>("random-weapon-upgrade-prices");
            _randomExclusives = randomizer.GetConfigOption<bool>("random-weapon-exclusives");
            if (!_randomStats && !_randomPrices && !_randomExclusives)
                return;

            _msg = randomizer.FileRepository.GetMsgFile(WeaponCustomMsgPath).ToBuilder();

            var itemRepo = ItemDefinitionRepository.Default;
            var mainFile = randomizer.FileRepository.GetUserFile(WeaponCustomUserDataPath);
            var detailFile = randomizer.FileRepository.GetUserFile(WeaponDetailCustomUserDataPath);
            var metaRoot = mainFile.RSZ!.ObjectList[0];
            var dataRoot = detailFile.RSZ!.ObjectList[0];

            var stats = GetWeaponStats(randomizer);

            var weapons = stats.GroupBy(x => x.WeaponId);
            var applicableWeapons = WeaponStatsDefinition.Exclusives.SelectMany(x => x.Include).Distinct().ToHashSet();
            foreach (var weaponStats in weapons)
            {
                var weaponId = weaponStats.Key;
                if (!applicableWeapons.Contains(weaponId))
                    continue;

                var weaponName = itemRepo.FromWeaponId(weaponId)?.Name ?? weaponId.ToString();
                logger.Push(weaponName);

                stats = RandomizeStats(metaRoot, dataRoot, stats, weaponId, logger);
                RandomizeExclusives(metaRoot, dataRoot, stats, weaponId, logger);

                logger.Pop();
            }

            SetWeaponStats(metaRoot, dataRoot, stats);

            randomizer.FileRepository.SetUserFile(WeaponCustomUserDataPath, mainFile);
            randomizer.FileRepository.SetUserFile(WeaponDetailCustomUserDataPath, detailFile);

            UpdateItemDefinitions(randomizer);

            randomizer.FileRepository.SetMsgFile(WeaponCustomMsgPath, _msg.ToMsg());
        }

        private static string GetLastArrayOfPath(string path)
        {
            var i = path.LastIndexOf('[');
            if (i == -1)
                return path;
            return path.Substring(0, i);
        }

        private void UpdateItemDefinitions(ChainsawRandomizer randomizer)
        {
            var userFile = randomizer.FileRepository.GetUserFile(ItemDefinitionUserDataPath);
            if (userFile == null)
                return;

            var stats = GetWeaponStats(randomizer);
            var baseStats = stats
                .OfType<WeaponUpgradeStat>()
                .Where(x => x.Level == 0 && x.Name == "Ammo Capacity")
                .ToArray();

            var itemRepo = ItemDefinitionRepository.Default;
            var root = userFile.RSZ!.ObjectList[0];
            var items = root.GetArray<RszInstance>("_Datas");
            foreach (var item in items)
            {
                var itemId = item.Get<int>("_ItemId");
                var itemDef = itemRepo.Find(itemId);
                if (itemDef == null)
                    continue;

                var stat = baseStats.FirstOrDefault(x => x.WeaponId == itemDef.WeaponId);
                if (stat == null)
                    continue;

                item.Set("_WeaponDefineData._AmmoMax", (int)stat.Value.Value);
            }

            randomizer.FileRepository.SetUserFile(ItemDefinitionUserDataPath, userFile);
        }

        private static string GetInfo(string name, string oldInfo, object oldValue, float newValue)
        {
            if (newValue == Convert.ToSingle(oldValue))
                return oldInfo;

            var infoValue = float.Parse(oldInfo);
            var infoMultiplier = infoValue / Convert.ToSingle(oldValue);
            var newInfoValue = (newValue * infoMultiplier) + 0.005;
            if (name == "Rate of Fire")
                newInfoValue = 1.8f - newValue * 0.36f;

            var fmt = "0.00";
            if (name == "Ammo Capacity" || name == "Reload Speed (Rounds)")
                fmt = "0";
            else if (name == "Durability")
                fmt = "0.0";
            else if (newInfoValue >= 10)
                fmt = "0.0";

            var result = newInfoValue.ToString(fmt);
            return result;
        }

        private ImmutableArray<WeaponStat> RandomizeStats(RszInstance metaRoot, RszInstance dataRoot, ImmutableArray<WeaponStat> stats, int weaponId, RandomizerLogger logger)
        {
            var result = stats;
            var statKinds = stats.GroupBy(x => x.Name);
            foreach (var statKind in statKinds)
            {
                var modifiedStats = statKind
                    .Where(x => x.WeaponId == weaponId)
                    .OfType<WeaponUpgradeStat>()
                    .Select(x => new WeaponStatModifier(x))
                    .ToList();

                if (modifiedStats.Count == 0)
                    continue;

                // Remove the old stats
                result = result.RemoveRange(modifiedStats.Select(x => x.Stat));

                RandomizeStats(modifiedStats, logger);

                foreach (var modifiedStat in modifiedStats)
                {
                    var stat = modifiedStat.Stat;
                    var newStat = stat with
                    {
                        Cost = stat.Cost.WithValue(modifiedStat.Cost),
                        Info = stat.Info.WithValue(GetInfo(stat.Name, stat.Info.Value, stat.Value.Value, modifiedStat.Value)),
                        Value = stat.Value.WithValue(stat.Value.Value is float
                            ? (object)(float)modifiedStat.Value
                            : (object)(int)MathF.Round(modifiedStat.Value))
                    };
                    result = result.Add(newStat);
                }
            }
            return result;
        }

        private void RandomizeStats(List<WeaponStatModifier> stats, RandomizerLogger logger)
        {
            if (_randomStats)
            {
                var firstStat = stats[0].Stat;
                var lastStat = stats[^1].Stat;

                var name = firstStat.Name;
                var upgradeDef = WeaponStatsDefinition.Upgrades.First(x => x.Name == name);
                var group = upgradeDef.Groups.First(x => x.Include.Contains(firstStat.WeaponId));
                var startValue = group.StartMin == 0
                    ? Convert.ToSingle(firstStat.Value.Value)
                    : _valueRng.NextFloat(group.StartMin, group.Min);

                var groupMin = group.Min;
                var groupMax = group.Max;
                if (_valueRng.NextProbability(75))
                {
                    groupMax = groupMin + ((groupMax - groupMin) / 2);
                }

                var max = groupMin < groupMax
                    ? Math.Max(startValue, _valueRng.NextFloat(groupMin, groupMax + 1))
                    : Math.Min(startValue, _valueRng.NextFloat(groupMax, groupMin + 1));
                RandomizeStats(stats, startValue, max, group.MinIncrement, group.StartMin != 0);
                LogValueChange(stats, firstStat, lastStat, logger);
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

        private static void RandomizeStats(
            List<WeaponStatModifier> stats,
            float startValue,
            float max,
            float minIncrement,
            bool fromStart)
        {
            var startI = fromStart ? 0 : 1;
            var increment = minIncrement >= 0
                ? MathF.Max(minIncrement, (max - startValue) / 4.0f)
                : MathF.Min(minIncrement, (max - startValue) / 4.0f);
            var value = startValue;
            for (var i = startI; i < 5; i++)
            {
                if (minIncrement >= 0 ? value < max : value > max)
                {
                    var newValue = value + increment;
                    value = newValue;
                    stats[i].Value = newValue;
                }
                else
                {
                    stats.RemoveRange(i, stats.Count - i);
                    break;
                }
            }
        }

        private static void LogValueChange(
            List<WeaponStatModifier> stats,
            WeaponUpgradeStat originalFirst,
            WeaponUpgradeStat originalLast,
            RandomizerLogger logger)
        {
            var name = originalFirst.Name;
            var format = originalFirst.Value.Value is float ? "0.00" : "0";
            var originalMin = Convert.ToSingle(originalFirst.Value.Value).ToString(format);
            var originalMax = Convert.ToSingle(originalLast.Value.Value).ToString(format);
            var min = Convert.ToSingle(stats.First().Value).ToString(format);
            var max = Convert.ToSingle(stats.Last().Value).ToString(format);
            var ratio = stats.Last().Value / Convert.ToSingle(originalLast.Value.Value) * 100;
            logger.LogLine($"{name} {originalMin} - {originalMax} -> {min} - {max} ({ratio:0}%)");
        }

        private void RandomizeExclusives(RszInstance metaRoot, RszInstance dataRoot, ImmutableArray<WeaponStat> stats, int weaponId, RandomizerLogger logger)
        {
            if (_randomPrices)
                RandomizeExclusivePrices(metaRoot, stats, weaponId, logger);
            if (_randomExclusives)
                RandomizeExclusive(metaRoot, dataRoot, stats, weaponId, logger);
        }

        private void RandomizeExclusivePrices(RszInstance metaRoot, ImmutableArray<WeaponStat> stats, int weaponId, RandomizerLogger logger)
        {
            var firstExclusiveStat = stats
                .OfType<WeaponExclusiveStat>()
                .FirstOrDefault(x => x.WeaponId == weaponId);

            if (firstExclusiveStat == null)
                return;

            var cost = _priceRng.Next(50, 151) * 1000;
            metaRoot.Set(firstExclusiveStat.Cost.Path, cost);
            logger.LogLine($"Exclusive Cost = {cost}");
        }

        private void RandomizeExclusive(
            RszInstance metaRoot,
            RszInstance dataRoot,
            ImmutableArray<WeaponStat> stats,
            int weaponId,
            RandomizerLogger logger)
        {
            var randomExclusive = WeaponStatsDefinition.Exclusives
                .Where(x => x.Include.Contains(weaponId))
                .Shuffle(_exclusiveRng)
                .First();

            var metaWeaponIndex = FindWeaponIndex(metaRoot, randomExclusive.MetaWeaponId, weaponId);
            var dataWeaponIndex = FindWeaponIndex(dataRoot, randomExclusive.DataWeaponId, weaponId);
            if (metaWeaponIndex == -1 || dataWeaponIndex == -1)
                return;

            var metaCategoryField = GetProcessedField<int>(metaRoot, randomExclusive.MetaCategory, metaWeaponIndex, 0);
            metaRoot.Set(metaCategoryField.Path, randomExclusive.Category);

            var dataCategoryField = GetProcessedField<int>(dataRoot, randomExclusive.DataCategory, dataWeaponIndex, 0);
            dataRoot.Set(dataCategoryField.Path, randomExclusive.Category);

            var messageIdField = GetProcessedField<Guid>(metaRoot, randomExclusive.MetaMessageId, metaWeaponIndex, 0);
            var perksMessageIdField = GetProcessedField<Guid>(metaRoot, randomExclusive.MetaPerksMessageId, metaWeaponIndex, 0);
            var rateValueField = GetProcessedField<float>(metaRoot, randomExclusive.MetaRateValue, metaWeaponIndex, 0);

            float rateValue = 1.0f;
            object value = 1.0f;
            switch (randomExclusive.Category)
            {
                case 3:
                    rateValue = _exclusiveRng.NextFloat(randomExclusive.MinValue, randomExclusive.MaxValue);
                    rateValue = (int)Math.Round(rateValue);
                    value = (int)rateValue;
                    break;
                case 7:
                case 9:
                    value = true;
                    break;
                default:
                    rateValue = _exclusiveRng.NextFloat(randomExclusive.MinValue, randomExclusive.MaxValue);
                    rateValue = (int)(rateValue / 0.5) * 0.5f;
                    value = rateValue;
                    break;
            }

            var rateString = rateValue.ToString();
            metaRoot.Set(messageIdField.Path, CreateMsgEntry(randomExclusive.MessageId, randomExclusive.Message, rateString));
            metaRoot.Set(perksMessageIdField.Path, CreateMsgEntry(randomExclusive.PerkMessageId, randomExclusive.PerkMessage, rateString));

            metaRoot.Set(rateValueField.Path, rateValue);
            logger.LogLine($"Setting exclusive to {randomExclusive.Name} Rate = {value}");
            foreach (var field in randomExclusive.DataFields)
            {
                var fieldField = GetProcessedField<object>(dataRoot, field, dataWeaponIndex, 0);
                dataRoot.Set(fieldField.Path, value);
            }
        }

        private Guid CreateMsgEntry(Guid original, string ourString, string newValue)
        {
            var entry = original == default ?
                _msg.Create(ourString) :
                _msg.Duplicate(original);

            for (var i = 0; i < entry.Langs.Count; i++)
            {
                var oldString = entry.Langs[i];
                var newString = Regex.Replace(oldString, @"\d+([.,]\d+)?", newValue);
                entry.Langs[i] = newString;
            }
            return entry.Guid;
        }

        private static int FindWeaponIndex(RszInstance root, string xpath, int weaponId)
        {
            for (var w = 0; w < 100; w++)
            {
                var weaponPath = ProcessPath(xpath, w, 0);
                var weaponIdField = GetProcessedField<int>(root, weaponPath, w, 0);
                if (weaponIdField.Value == weaponId)
                {
                    return w;
                }
            }
            return -1;
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
                var costs = new Dictionary<(int, int), WeaponRszField<int>>();
                var infos = new Dictionary<(int, int), WeaponRszField<string>>();
                var values = new Dictionary<(int, int), WeaponRszField<object>>();

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
                                costs[(metaWeaponId.Value, l)] = new WeaponRszField<int>(metaCostPath, metaCost.Value);
                                infos[(metaWeaponId.Value, l)] = new WeaponRszField<string>(metaInfoPath, metaInfo ?? "");
                            }

                            var dataWeaponId = dataRoot.Get<int?>(ProcessPath(d.DataWeaponId, w, i, l), relaxed: true);
                            var dataCategory = dataRoot.Get<int?>(ProcessPath(d.DataCategory, w, i, l), relaxed: true);
                            var dataPath = ProcessPath(d.Data, w, i, l);
                            var data = dataRoot.Get(dataPath, relaxed: true);
                            if (dataWeaponId != null && dataCategory == d.Category && data != null)
                            {
                                values[(dataWeaponId.Value, l)] = new WeaponRszField<object>(dataPath, data);
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
                        if (metaCost.Value < 0)
                            metaCost = metaCost.WithValue(0);
                        if (d.Name == "Ammo Capacity" || d.Name == "Reload Speed (Rounds)" || d.Name == "Durability")
                        {
                            if ((int)value.Value < 0)
                                value = value.WithValue(int.Parse(metaInfo.Value));
                        }
                        else
                        {
                            if ((float)value.Value < 0)
                                value = value.WithValue(1.0f);
                        }
                        stats.Add(new WeaponUpgradeStat(
                            d.Name,
                            metaWeaponId,
                            level,
                            metaCost,
                            metaInfo,
                            value));
                    }
                }
            }

            foreach (var e in def.Exclusives)
            {
                for (var w = 0; w < 100; w++)
                {
                    for (var i = 0; i < 10; i++)
                    {
                        var dataCategory = dataRoot.Get<int?>(ProcessPath(e.DataCategory, w, i), relaxed: true);
                        if ((dataCategory ?? -1) != e.Category)
                            continue;

                        var dataWeaponId = dataRoot.Get<int?>(ProcessPath(e.DataWeaponId, w, i), relaxed: true);
                        var metaCost = GetProcessedField<int>(metaRoot, e.MetaCost, w, i);
                        foreach (var field in e.DataFields)
                        {
                            var dataPath = ProcessPath(field, w, i);
                            var value = dataRoot.Get(dataPath, relaxed: true);
                            stats.Add(new WeaponExclusiveStat(
                                e.Name,
                                dataWeaponId!.Value,
                                metaCost,
                                new WeaponRszField<string>(),
                                new WeaponRszField<object>(dataPath, value!)));
                        }
                    }
                }
            }

            return [.. stats];
        }

        private void SetWeaponStats(RszInstance metaRoot, RszInstance dataRoot, ImmutableArray<WeaponStat> stats)
        {
            // Limit arrays to correct number
            var aa = stats
                .OfType<WeaponUpgradeStat>()
                .GroupBy(x => (x.WeaponId, x.Name));
            foreach (var a in aa)
            {
                var levels = a.Count();
                var firstLevel = a.First();
                var metaArrayPath = GetLastArrayOfPath(firstLevel.Cost.Path);
                var dataArrayPath = GetLastArrayOfPath(firstLevel.Value.Path);
                var metaArray = metaRoot.GetList(metaArrayPath);
                var dataArray = dataRoot.GetList(dataArrayPath);
                while (metaArray.Count > levels)
                    metaArray.RemoveAt(metaArray.Count - 1);
                while (dataArray.Count > levels)
                    dataArray.RemoveAt(dataArray.Count - 1);
            }

            foreach (var stat in stats)
            {
                metaRoot.Set(stat.Cost.Path, stat.Cost.Value);
                dataRoot.Set(stat.Value.Path, stat.Value.Value);

                if (stat is WeaponExclusiveStat exclusive)
                {
                }
                else if (stat is WeaponUpgradeStat upgrade)
                {
                    metaRoot.Set(stat.Info.Path, stat.Info.Value);
                }
            }
        }

        private static WeaponRszField<T> GetProcessedField<T>(RszInstance instance, string xpath, int w, int i, int l = 0)
        {
            var processedPath = ProcessPath(xpath, w, i, l);
            var value = instance.Get<T?>(processedPath, relaxed: true);
            return new WeaponRszField<T>(processedPath, value!);
        }

        private static string ProcessPath(string xpath, int w, int i, int l = 0)
        {
            return xpath
                .Replace("{W}", w.ToString())
                .Replace("{I}", i.ToString())
                .Replace("{L}", l.ToString());
        }

        private static string GetFieldName(string xpath)
        {
            var i = xpath.LastIndexOf('.');
            if (i == -1)
                return xpath;
            return xpath[(i + 1)..];
        }

        private static WeaponStatsDefinition WeaponStatsDefinition { get; } = Resources.stats.DeserializeJson<WeaponStatsDefinition>();
    }

    public class WeaponStatsDefinition
    {
        public WeaponUpgradeDefinition[] Upgrades { get; set; } = [];
        public WeaponExclusiveDefinition[] Exclusives { get; set; } = [];
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
        public WeaponUpgradeDefinitionGroup[] Groups { get; set; } = [];
    }

    public class WeaponUpgradeDefinitionGroup
    {
        public float StartMin { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        public float MinIncrement { get; set; }
        public int[] Include { get; set; } = [];
    }

    public class WeaponExclusiveDefinition
    {
        public string Name { get; set; } = "";
        public int Category { get; set; }
        public string MetaWeaponId { get; set; } = "";
        public string MetaCategory { get; set; } = "";
        public string DataWeaponId { get; set; } = "";
        public string DataCategory { get; set; } = "";
        public string MetaMessageId { get; set; } = "";
        public string MetaPerksMessageId { get; set; } = "";
        public string MetaRateValue { get; set; } = "";
        public string MetaCost { get; set; } = "";
        public string[] DataFields { get; set; } = [];
        public int[] Include { get; set; } = [];
        public Guid MessageId { get; set; }
        public Guid PerkMessageId { get; set; }
        public string Message { get; set; } = "";
        public string PerkMessage { get; set; } = "";
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
    }

    public abstract record WeaponStat(
        string Name,
        int WeaponId,
        WeaponRszField<int> Cost,
        WeaponRszField<string> Info,
        WeaponRszField<object> Value)
    {
    }

    public record WeaponUpgradeStat(
        string Name,
        int WeaponId,
        int Level,
        WeaponRszField<int> Cost,
        WeaponRszField<string> Info,
        WeaponRszField<object> Value) : WeaponStat(Name, WeaponId, Cost, Info, Value)
    {
    }

    public record WeaponExclusiveStat(
        string Name,
        int WeaponId,
        WeaponRszField<int> Cost,
        WeaponRszField<string> Info,
        WeaponRszField<object> Value) : WeaponStat(Name, WeaponId, Cost, Info, Value)
    {
    }

    public class WeaponStatModifier(WeaponUpgradeStat stat)
    {
        public WeaponUpgradeStat Stat => stat;
        public int Cost { get; set; } = stat.Cost.Value;
        public float Value { get; set; } = Convert.ToSingle(stat.Value.Value);

        public override string ToString() => $"Cost = {Cost} Value = {Value}";
    }

    public readonly struct WeaponRszField<T>(string path, T value)
    {
        public string Path => path;
        public T Value => value;

        public WeaponRszField<T> WithValue(T value) => new WeaponRszField<T>(path, value);
        public override string? ToString() => Value?.ToString() ?? base.ToString();
    }
}
