using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using MsgTool;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal partial class WeaponModifier : Modifier
    {
        private const string WeaponCustomUserDataPath = "natives/stm/_chainsaw/appsystem/weaponcustom/weaponcustomuserdata.user.2";
        private const string WeaponDetailCustomUserDataPath = "natives/stm/_chainsaw/appsystem/weaponcustom/weapondetailcustomuserdata.user.2";
        private const string ItemDefinitionUserDataPath = "natives/stm/_chainsaw/appsystem/ui/userdata/itemdefinitionuserdata.user.2";
        private const string WeaponCustomMsgPath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_wpcustom.msg.22";
        private const string ShopMsgPath = "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_shop.msg.22";

        private bool _randomStats;
        private bool _randomPrices;
        private bool _randomExclusives;
        private Func<string, Guid> _addMessage = _ => default;
        private Dictionary<int, int> _startAmmoCapacity = new();

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var mainFile = randomizer.FileRepository.GetUserFile(WeaponCustomUserDataPath);
            var detailFile = randomizer.FileRepository.GetUserFile(WeaponDetailCustomUserDataPath);
            var wpCustomMsg = randomizer.FileRepository.GetMsgFile(WeaponCustomMsgPath);
            var weaponStatCollection = new WeaponStatCollection(mainFile, detailFile);
            foreach (var wp in weaponStatCollection.Weapons)
            {
                logger.Push($"Weapon {wp.Name}");
                foreach (var m in wp.Modifiers)
                {
                    if (m is IWeaponUpgrade u)
                    {
                        var message = wpCustomMsg.GetString(u.MessageId, LanguageId.English)?.Replace("\r\n", " ");

                        logger.Push($"Upgrade ({message})");
                        foreach (var l in u.Levels)
                        {
                            logger.LogLine($"{l}");
                        }
                        logger.Pop();
                    }
                    else if (m is IWeaponExclusive e)
                    {
                        var perkMessage = wpCustomMsg.GetString(e.PerkMessageId, LanguageId.English)?.Replace("\r\n", " ");
                        logger.LogLine($"Exclusive ({perkMessage}) Cost = {e.Cost}");
                    }
                }
                logger.Pop();
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            _startAmmoCapacity.Clear();

            var rng = randomizer.CreateRng();
            var priceRng = rng.NextFork();
            var valueRng = rng.NextFork();

            _randomStats = randomizer.GetConfigOption<bool>("random-weapon-stats");
            _randomPrices = randomizer.GetConfigOption<bool>("random-weapon-upgrade-prices");
            _randomExclusives = randomizer.GetConfigOption<bool>("random-weapon-exclusives");
            if (!_randomStats && !_randomPrices && !_randomExclusives)
                return;

            var shopMsg = randomizer.FileRepository.GetMsgFile(ShopMsgPath).ToBuilder();
            var wpMsg = randomizer.FileRepository.GetMsgFile(WeaponCustomMsgPath).ToBuilder();
            var mainFile = randomizer.FileRepository.GetUserFile(WeaponCustomUserDataPath);
            var detailFile = randomizer.FileRepository.GetUserFile(WeaponDetailCustomUserDataPath);

            shopMsg.SetStringAll(new Guid("6f60b94f-1766-4c98-8335-a69958e2d927"), "Critical Hit Rate");
            shopMsg.SetStringAll(new Guid("db128948-0960-4147-814d-fec706a5c34a"), "Penetration Power");
            _addMessage = (s) => wpMsg.Create(s).Guid;

            var weaponStatCollection = new WeaponStatCollection(mainFile, detailFile);
            foreach (var wp in weaponStatCollection.Weapons)
            {
                RandomizeWeaponStats(wp, valueRng);
            }
            weaponStatCollection.Apply();

            randomizer.FileRepository.SetUserFile(WeaponCustomUserDataPath, mainFile);
            randomizer.FileRepository.SetUserFile(WeaponDetailCustomUserDataPath, detailFile);
            randomizer.FileRepository.SetMsgFile(WeaponCustomMsgPath, wpMsg.ToMsg());
            randomizer.FileRepository.SetMsgFile(ShopMsgPath, shopMsg.ToMsg());

            UpdateItemDefinitions(randomizer);
        }

        private void RandomizeWeaponStats(WeaponStats wp, Rng rng)
        {
            var group = WeaponStatsDefinition.Groups.FirstOrDefault(x => x.Include.Contains(wp.Id));
            if (group == null)
                return;

            if (_randomExclusives)
                wp.Modifiers = wp.Modifiers.RemoveAll(x => x is IWeaponExclusive);

            var rngSuper = () => rng.NextProbability(2);
            if (group.Power != null)
            {
                RandomizePower(wp, RandomizeFromRanges(rng, group.Power, 0.1f, rngSuper()));
                if (_randomExclusives)
                    AddExclusive(wp, WeaponUpgradeKind.Power, rng.NextFloat(1.5f, 4));
            }
            if (group.AmmoCapacity != null)
            {
                var values = RandomizeFromRanges(rng, group.AmmoCapacity, 1, rngSuper()).Select(x => (int)MathF.Round(x)).ToArray();
                _startAmmoCapacity[wp.Id] = values[0];
                RandomizeAmmoCapacity(wp, values);
                if (_randomExclusives)
                    AddExclusive(wp, WeaponUpgradeKind.AmmoCapacity, rng.NextFloat(1.5f, 4));
            }

            if (group.CriticalRate != null && wp.Modifiers.Any(x => x.Kind == WeaponUpgradeKind.CriticalRate))
            {
                if (rng.NextProbability(50))
                    RandomizeCriticalRate(wp, RandomizeFromRanges(rng, group.CriticalRate, 1, rngSuper()));
                else
                    AddExclusive(wp, WeaponUpgradeKind.CriticalRate, rng.Next(5, 21));
            }
            if (group.Penetration != null && wp.Modifiers.Any(x => x.Kind == WeaponUpgradeKind.Penetration))
            {
                if (rng.NextProbability(50))
                    RandomizePenetration(wp, RandomizeFromRanges(rng, group.Penetration, 1, rngSuper()));
                else
                    AddExclusive(wp, WeaponUpgradeKind.Penetration, rng.Next(5, 21));
            }

            var mask = 0b11;
            if (group.ReloadSpeed != null && group.ReloadRounds != null)
            {
                mask = rng.NextProbability(50) ? 0b01 : 0b10;
            }
            if (group.ReloadSpeed != null && (mask & 0b01) != 0)
            {
                RandomizeReloadSpeed(wp, RandomizeFromRanges(rng, group.ReloadSpeed, 0.1f, rngSuper()));
            }
            if (group.ReloadRounds != null && (mask & 0b10) != 0)
            {
                var values = RandomizeFromRanges(rng, group.ReloadRounds, 1, rngSuper()).Select(x => (int)MathF.Round(x)).ToArray();
                RandomizeReloadRounds(wp, values);
            }

            if (group.FireRate != null)
            {
                RandomizeFireRate(wp, RandomizeFromRanges(rng, group.FireRate, 0.1f, rngSuper()));
                if (_randomExclusives)
                    AddExclusive(wp, WeaponUpgradeKind.FireRate, rng.NextFloat(1.5f, 4));
            }

            if (_randomExclusives)
            {
                wp.Modifiers = wp.Modifiers
                    .Shuffle(rng)
                    .Take(5)
                    .OrderBy(x => x.Kind)
                    .ToImmutableArray();
            }
            else
            {
                wp.Modifiers = wp.Modifiers
                    .Shuffle(rng)
                    .OrderByDescending(x => x is IWeaponExclusive)
                    .Take(5)
                    .OrderBy(x => x.Kind)
                    .ToImmutableArray();
            }
        }

        private float[] RandomizeFromRanges(Rng rng, float[][] ranges, float minDelta, bool super)
        {
            bool reverse = false;
            for (var i = 0; i < ranges.Length; i++)
            {
                if (ranges[i][0] > ranges[i][1])
                {
                    (ranges[i][0], ranges[i][1]) = (ranges[i][1], ranges[i][0]);
                    reverse = true;
                }
            }

            var min = MathF.Round(rng.NextFloat(ranges[0][0], ranges[0][1]), 1);
            var max = !super
                ? MathF.Round(rng.NextFloat(ranges[1][0], ranges[1][1]), 1)
                : MathF.Round(rng.NextFloat(ranges[2][0], ranges[2][1]), 1);
            var delta = !reverse
                ? Math.Max(minDelta, MathF.Round((max - min) / 5, 1))
                : Math.Min(minDelta, -MathF.Round((min - max) / 5, 1));
            return Enumerable.Range(0, 5).Select(x => min + (delta * x)).ToArray();
        }

        private void RandomizePower(WeaponStats stat, float[] values)
        {
            var power = stat.Modifiers.OfType<PowerUpgrade>().First();
            var levels = power.Levels.ToArray();
            var multiplier = float.Parse(levels[0].Info);
            for (var i = 0; i < 5; i++)
            {
                levels[i] = power.Levels[i] with
                {
                    Damage = values[i],
                    Wince = values[i],
                    Break = values[i],
                    Stopping = values[i],
                    ExplosionRadiusScale = values[i],
                    ExplosionSensorRadiusScale = values[i],
                    Info = (values[i] * multiplier).ToString("0.00")
                };
            }
            power.Levels = [.. levels];
        }

        private void RandomizeAmmoCapacity(WeaponStats stat, int[] values)
        {
            var ammoCapacity = stat.Modifiers.OfType<AmmoCapacityUpgrade>().First();
            var levels = ammoCapacity.Levels.ToArray();
            for (var i = 0; i < 5; i++)
            {
                levels[i] = ammoCapacity.Levels[i] with { Value = values[i], Info = values[i].ToString() };
            }
            ammoCapacity.Levels = [.. levels];
        }

        private void RandomizeCriticalRate(WeaponStats stat, float[] values)
        {
            var cost = new[] { 0, 10_000, 15_000, 20_000, 30_000 };
            stat.Modifiers = stat.Modifiers.Add(new CriticalRateUpgrade
            {
                MessageId = _addMessage("Increase critical hit rate."),
                Levels = values.Zip(cost).Select(x =>
                {
                    var value = (int)MathF.Round(x.First);
                    return new CriticalRateUpgradeLevel(x.Second, value.ToString(), value);
                }).ToImmutableArray()
            });
        }

        private void RandomizePenetration(WeaponStats stat, float[] values)
        {
            var cost = new[] { 0, 10_000, 15_000, 20_000, 30_000 };
            stat.Modifiers = stat.Modifiers.Add(new PenetrationUpgrade
            {
                MessageId = _addMessage("Increase penetration."),
                Levels = values.Zip(cost).Select(x =>
                {
                    var value = (int)MathF.Round(x.First);
                    return new PenetrationUpgradeLevel(x.Second, value.ToString(), value);
                }).ToImmutableArray()
            });
        }

        private void RandomizeReloadSpeed(WeaponStats stat, float[] values)
        {
            var reloadSpeed = stat.Modifiers.OfType<ReloadSpeedUpgrade>().First();
            reloadSpeed.MessageId = new Guid("a3e8cc54-b462-4be3-9e77-e6660ecf0e17");
            var levels = reloadSpeed.Levels.ToArray();
            for (var i = 0; i < 5; i++)
            {
                var original = levels[0].Speed;
                if (original <= 0)
                    original = 1;

                var value = MathF.Round(original * values[i], 2);
                levels[i] = reloadSpeed.Levels[i] with { Num = 0, Speed = value, Info = values[i].ToString("0.00") };
            }
            reloadSpeed.Levels = [.. levels];
        }

        private void RandomizeReloadRounds(WeaponStats stat, int[] values)
        {
            var reloadSpeed = stat.Modifiers.OfType<ReloadSpeedUpgrade>().First();
            reloadSpeed.MessageId = new Guid("173bfc85-dbf2-4d39-8ba9-6e5284990c63");
            var levels = reloadSpeed.Levels.ToArray();
            for (var i = 0; i < 5; i++)
            {
                levels[i] = reloadSpeed.Levels[i] with { Num = values[i], Speed = 0, Info = values[i].ToString() };
            }
            reloadSpeed.Levels = [.. levels];
        }

        private void RandomizeFireRate(WeaponStats stat, float[] values)
        {
            var fireRate = stat.Modifiers.OfType<FireRateUpgrade>().First();
            var levels = fireRate.Levels.ToArray();
            var infoMultiplier = float.Parse(levels[0].Info) / levels[0].Speed;
            for (var i = 0; i < 5; i++)
            {
                var value = MathF.Round(levels[0].Speed * values[i], 2);
                var info = 1 / values[i];
                levels[i] = fireRate.Levels[i] with { Speed = value, Info = info.ToString("0.00") };
            }
            fireRate.Levels = [.. levels];
        }

        private void AddExclusive(WeaponStats wp, WeaponUpgradeKind kind, float rate)
        {
            rate = MathF.Round(rate / 0.5f) * 0.5f;
            wp.Modifiers = wp.Modifiers.Add(kind switch
            {
                WeaponUpgradeKind.CriticalRate =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.CriticalRate,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase the critical hit rate by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Critical Hit Rate")
                    },
                WeaponUpgradeKind.AmmoCapacity =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.AmmoCapacity,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase ammo capacity by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Ammo Capacity")
                    },
                WeaponUpgradeKind.Power =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Power,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase power by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Power")
                    },
                WeaponUpgradeKind.Penetration =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Penetration,
                        RateValue = rate,
                        MessageId = _addMessage($"Penetrate through {rate} targets."),
                        PerkMessageId = _addMessage($"{rate}x Penetration Power")
                    },
                WeaponUpgradeKind.FireRate =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.FireRate,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase rate of fire by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Rate of Fire")
                    },
                WeaponUpgradeKind.Durability =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Durability,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase durability by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Durability")
                    },
                WeaponUpgradeKind.CombatSpeed =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.CombatSpeed,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase attack speed by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Attack Speed")
                    },
                WeaponUpgradeKind.UnlimitedAmmo =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.UnlimitedAmmo,
                        RateValue = 1,
                        MessageId = _addMessage("Unlimited Ammo"),
                        PerkMessageId = _addMessage("Unlimited Ammo")
                    },
                WeaponUpgradeKind.Indestructible =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Indestructible,
                        RateValue = 1,
                        MessageId = _addMessage("Becomes indestructible."),
                        PerkMessageId = _addMessage("Indestructible")
                    },
                _ => throw new NotSupportedException()
            });
        }

        private void UpdateItemDefinitions(ChainsawRandomizer randomizer)
        {
            var userFile = randomizer.FileRepository.GetUserFile(ItemDefinitionUserDataPath);
            if (userFile == null)
                return;

            var itemRepo = ItemDefinitionRepository.Default;
            var root = userFile.RSZ!.ObjectList[0];
            var items = root.GetArray<RszInstance>("_Datas");
            foreach (var item in items)
            {
                var itemId = item.Get<int>("_ItemId");
                var itemDef = itemRepo.Find(itemId);
                if (itemDef == null)
                    continue;

                if (itemDef.WeaponId != null && _startAmmoCapacity.TryGetValue(itemDef.WeaponId.Value, out var ammoCapacity))
                {
                    item.Set("_WeaponDefineData._AmmoMax", ammoCapacity);
                }
            }

            randomizer.FileRepository.SetUserFile(ItemDefinitionUserDataPath, userFile);
        }

        private static WeaponStatsDefinition WeaponStatsDefinition { get; } = Resources.stats.DeserializeJson<WeaponStatsDefinition>();
    }

    public class WeaponStatsDefinition
    {
        public WeaponStatGroup[] Groups { get; set; } = [];
    }

    public class WeaponStatGroup
    {
        public int[] Include { get; set; } = [];
        public float[][]? Power { get; set; }
        public float[][]? AmmoCapacity { get; set; }
        public float[][]? CriticalRate { get; set; }
        public float[][]? Penetration { get; set; }
        public float[][]? ReloadSpeed { get; set; }
        public float[][]? ReloadRounds { get; set; }
        public float[][]? FireRate { get; set; }
    }
}
