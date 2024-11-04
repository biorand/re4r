using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text.Json.Serialization;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using MsgTool;
using RszTool;
using static chainsaw.WeaponDetailCustomUserdata;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal partial class WeaponModifier : Modifier
    {
        private const string WeaponCustomMsgPath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_wpcustom.msg.22";
        private const string ShopMsgPath = "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_shop.msg.22";

        private Func<string, Guid> _addMessage = _ => default;
        private Dictionary<int, int> _startAmmoCapacity = new();

        private static string GetMainPath(ChainsawRandomizer randomizer)
        {
            return randomizer.Campaign == Campaign.Leon ?
                "natives/stm/_chainsaw/appsystem/weaponcustom/weaponcustomuserdata.user.2" :
                "natives/stm/_anotherorder/appsystem/weaponcustom/weaponcustomuserdata_ao.user.2";
        }

        private static string GetDetailPath(ChainsawRandomizer randomizer)
        {
            return randomizer.Campaign == Campaign.Leon ?
                "natives/stm/_chainsaw/appsystem/weaponcustom/weapondetailcustomuserdata.user.2" :
                "natives/stm/_anotherorder/appsystem/weaponcustom/weapondetailcustomuserdata_ao.user.2";
        }

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var mainFile = randomizer.FileRepository.GetUserFile(GetMainPath(randomizer));
            var detailFile = randomizer.FileRepository.GetUserFile(GetDetailPath(randomizer));
            var wpCustomMsg = randomizer.FileRepository.GetMsgFile(WeaponCustomMsgPath);
            var weaponStatCollection = new WeaponStatCollection(mainFile, detailFile);
            foreach (var wp in weaponStatCollection.Weapons)
            {
                if (wp.ItemDefinition?.SupportsCampaign(randomizer.Campaign) != true)
                    continue;

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
                    else if (m is IWeaponRepair r)
                    {
                        var message = wpCustomMsg.GetString(r.MessageId, LanguageId.English)?.Replace("\r\n", " ");

                        logger.Push($"Repair ({message})");
                        logger.LogLine($"{r.Kind} {{ ... }}");
                        logger.Pop();
                    }
                    else if (m is IWeaponExclusive e)
                    {
                        var perkMessage = wpCustomMsg.GetString(e.PerkMessageId, LanguageId.English)?.Replace("\r\n", " ");
                        logger.Push($"Exclusive ({perkMessage})");
                        logger.LogLine($"{e.Kind} {{ Cost = {e.Cost} Rate = {e.RateValue} }}");
                        logger.Pop();
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

            var randomStats = randomizer.GetConfigOption<bool>("random-weapon-stats");
            var randomPrices = randomizer.GetConfigOption<bool>("random-weapon-upgrade-prices");
            var randomUpgrades = randomizer.GetConfigOption<bool>("random-weapon-upgrades");
            var randomExclusives = randomizer.GetConfigOption<bool>("random-weapon-exclusives");
            if (!randomStats && !randomUpgrades && !randomPrices && !randomExclusives)
                return;

            var shopMsg = randomizer.FileRepository.GetMsgFile(ShopMsgPath).ToBuilder();
            var wpMsg = randomizer.FileRepository.GetMsgFile(WeaponCustomMsgPath).ToBuilder();
            var mainFile = randomizer.FileRepository.GetUserFile(GetMainPath(randomizer));
            var detailFile = randomizer.FileRepository.GetUserFile(GetDetailPath(randomizer));

            shopMsg.SetStringAll(new Guid("6f60b94f-1766-4c98-8335-a69958e2d927"), "Critical Hit Rate");
            shopMsg.SetStringAll(new Guid("db128948-0960-4147-814d-fec706a5c34a"), "Penetration Power");
            _addMessage = (s) => wpMsg.Create(s).Guid;

            var weaponStatCollection = new WeaponStatCollection(mainFile, detailFile);
            foreach (var wp in weaponStatCollection.Weapons)
            {
                if (wp.ItemDefinition?.SupportsCampaign(randomizer.Campaign) != true)
                    continue;

                LogWeaponChanges(wp, logger, () =>
                {
                    if (randomExclusives)
                    {
                        RandomizeExclusives(randomizer, wp, valueRng);
                    }
                    RandomizeStats(randomizer, wp, valueRng, randomUpgrades);
                    RandomizePrices(rng, wp, randomPrices);
                });
            }
            weaponStatCollection.Apply();

            randomizer.FileRepository.SetUserFile(GetMainPath(randomizer), mainFile);
            randomizer.FileRepository.SetUserFile(GetDetailPath(randomizer), detailFile);
            randomizer.FileRepository.SetMsgFile(WeaponCustomMsgPath, wpMsg.ToMsg());
            randomizer.FileRepository.SetMsgFile(ShopMsgPath, shopMsg.ToMsg());

            UpdateItemDefinitions(randomizer);
            UpdateUnlocks(randomizer, weaponStatCollection);
        }

        private void LogWeaponChanges(WeaponStats wp, RandomizerLogger logger, Action action)
        {
            var before = CalculateTop(wp, WeaponUpgradeKind.Power, 0);
            action();
            var after = CalculateTop(wp, WeaponUpgradeKind.Power, before.Item2);
            logger.LogLine($"{wp.Name} | {before.Item2:0.00} ({before.Item1:N0}pts.) | {after.Item2:0.00} ({after.Item1:N0}pts.)");
        }

        private static (int, float) CalculateTop(WeaponStats wp, WeaponUpgradeKind kind, float fallback)
        {
            var upgrade = wp.Modifiers.OfType<IWeaponUpgrade>().FirstOrDefault(x => x.Kind == kind);
            var exclusive = wp.Modifiers.OfType<IWeaponExclusive>().FirstOrDefault(x => x.Kind == kind);

            var cost = upgrade == null ? 0 : upgrade.Cost.Skip(1).Sum();
            var info = upgrade == null ? fallback : float.Parse(upgrade.Levels[^1].Info);
            if (exclusive != null)
            {
                cost += exclusive.Cost;
                info *= exclusive.RateValue;
            }
            return (cost, info);
        }

        private void RandomizeStats(ChainsawRandomizer randomizer, WeaponStats wp, Rng rng, bool randomUpgrades)
        {
            var group = GetWeaponStatsGroup(wp);
            if (group == null)
                return;

            var exclusives = wp.Modifiers.OfType<IWeaponExclusive>().ToImmutableArray();

            var rngSuper = () => rng.NextProbability(5);
            if (group.Power != null)
            {
                RandomizePower(wp, RandomizeFromRanges(rng, group.Power, 0.01f, rngSuper()));
            }
            if (group.AmmoCapacity != null)
            {
                var values = RandomizeFromRanges(rng, group.AmmoCapacity, 1, rngSuper()).Select(x => (int)MathF.Round(x)).ToArray();
                _startAmmoCapacity[wp.Id] = values[0];
                RandomizeAmmoCapacity(wp, values);
            }

            if (group.CriticalRate != null && !exclusives.Any(x => x.Kind == WeaponUpgradeKind.CriticalRate))
            {
                RandomizeCriticalRate(wp, RandomizeFromRanges(rng, group.CriticalRate, 1, rngSuper()));
            }
            if (group.Penetration != null && !exclusives.Any(x => x.Kind == WeaponUpgradeKind.Penetration))
            {
                RandomizePenetration(wp, RandomizeFromRanges(rng, group.Penetration, 1, rngSuper()));
            }

            var mask = 0b11;
            if (group.ReloadSpeed != null && group.ReloadRounds != null)
            {
                mask = rng.NextProbability(50) ? 0b01 : 0b10;
            }
            if (!randomUpgrades)
            {
                var originalReloadSpeed = wp.Modifiers.OfType<ReloadSpeedUpgrade>().FirstOrDefault();
                if (originalReloadSpeed != null)
                {
                    mask = originalReloadSpeed.SubType == ReloadSpeedUpgrade.ReloadSpeedRate
                        ? 0b01
                        : 0b10;
                }
            }
            if (group.ReloadSpeed != null && (mask & 0b01) != 0)
            {
                RandomizeReloadSpeed(wp, RandomizeFromRanges(rng, group.ReloadSpeed, 0.01f, rngSuper()));
            }
            if (group.ReloadRounds != null && (mask & 0b10) != 0)
            {
                var values = RandomizeFromRanges(rng, group.ReloadRounds, 1, rngSuper()).Select(x => (int)MathF.Round(x)).ToArray();
                RandomizeReloadRounds(wp, values);
            }

            if (group.FireRate != null)
            {
                RandomizeFireRate(wp, RandomizeFromRanges(rng, group.FireRate, 0.01f, rngSuper()));
            }

            if (group.Durability != null)
            {
                RandomizeDurability(wp, RandomizeFromRanges(rng, group.Durability, 100, rngSuper()).Select(x => (int)MathF.Round(x)).ToArray());

                // Knives seem to break on hardcore if upgrades are different
                randomUpgrades = false;
            }

            if (randomUpgrades)
            {
                wp.Modifiers = wp.Modifiers
                    .Shuffle(rng)
                    .OrderByDescending(x => x is IWeaponExclusive)
                    .ThenByDescending(x => x is RepairUpgrade)
                    .ThenByDescending(x => x is PolishUpgrade)
                    .ThenByDescending(x => x is PowerUpgrade)
                    .Take(5)
                    .ToImmutableArray();
            }
            else
            {
                wp.Modifiers = wp.Modifiers
                    .Take(5)
                    .ToImmutableArray();
            }
        }

        private void RandomizeExclusives(ChainsawRandomizer randomizer, WeaponStats wp, Rng rng)
        {
            var group = GetWeaponStatsGroup(wp);
            if (group == null)
                return;

            var exclusives = ImmutableArray.CreateBuilder<IWeaponExclusive>();
            var exclusiveCount = rng.NextOf8020(1, 2, 3, 4);
            if (group.Power != null)
            {
                var min = (float)Math.Clamp(randomizer.GetConfigOption<double>("weapon-exclusive-power-min"), 1.5, 100);
                var max = (float)Math.Clamp(randomizer.GetConfigOption<double>("weapon-exclusive-power-max"), 1.5, 100);
                exclusives.Add(CreateExclusive(wp, WeaponUpgradeKind.Power, rng.NextFloat(min, max)));
            }
            if (group.AmmoCapacity != null)
            {
                exclusives.Add(CreateExclusive(wp, WeaponUpgradeKind.AmmoCapacity, rng.Next(2, 5)));
            }
            if (group.CriticalRate != null && wp.Id != 4600)
            {
                exclusives.Add(CreateExclusive(wp, WeaponUpgradeKind.CriticalRate, rng.Next(4, 13)));
            }
            if (group.Penetration != null)
            {
                exclusives.Add(CreateExclusive(wp, WeaponUpgradeKind.Penetration, rng.Next(5, 21)));
            }
            if (group.FireRate != null)
            {
                exclusives.Add(CreateExclusive(wp, WeaponUpgradeKind.FireRate, rng.NextFloat(1.5f, 4)));
            }
            if (group.Durability != null)
            {
                exclusives.Add(CreateExclusive(wp, WeaponUpgradeKind.Durability, rng.NextFloat(1.5f, 4)));
                exclusives.Add(CreateExclusive(wp, WeaponUpgradeKind.CombatSpeed, rng.NextFloat(1.5f, 3)));

                // Knives seem to break on hardcore if upgrades are different
                exclusiveCount = 1;
            }
            var newExclusives = exclusives
                .Shuffle(rng)
                .Take(exclusiveCount)
                .ToImmutableArray();
            wp.Modifiers = wp.Modifiers
                .RemoveAll(x => x is IWeaponExclusive)
                .AddRange(newExclusives);
        }

        private WeaponStatGroup? GetWeaponStatsGroup(WeaponStats wp)
        {
            return WeaponStatsDefinition.Groups.FirstOrDefault(x => x.Include.Contains(wp.Id));
        }

        private float[] RandomizeFromRanges(Rng rng, float[][] ranges, float minDelta, bool super)
        {
            bool reverse = false;
            ranges = ranges.ToArray();
            for (var i = 0; i < ranges.Length; i++)
            {
                ranges[i] = ranges[i].ToArray();
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
                var originalInfo = float.Parse(levels[0].Info);
                if (original <= 0)
                {
                    original = 1;
                    originalInfo = 1;
                }
                var value = MathF.Round(original * values[i], 2);
                var infoMultiplier = originalInfo / original;
                var info = infoMultiplier * value;

                levels[i] = reloadSpeed.Levels[i] with { Num = 0, Speed = value, Info = info.ToString("0.00") };
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
                var info = (levels[0].Speed + levels[0].Speed - levels[i].Speed) * infoMultiplier;
                levels[i] = fireRate.Levels[i] with { Speed = value, Info = info.ToString("0.00") };
            }
            fireRate.Levels = [.. levels];
        }

        private void RandomizeDurability(WeaponStats stat, int[] values)
        {
            var durability = stat.Modifiers.OfType<DurabilityUpgrade>().FirstOrDefault();
            if (durability == null)
            {
                var cost = new[] { 0, 5_000, 10_000, 15_000, 20_000 };
                durability = new DurabilityUpgrade
                {
                    MessageId = _addMessage("Increase durability."),
                    Levels = [.. cost.Select(x => new DurabilityUpgradeLevel(x, "0.0", 0))]
                };
                stat.Modifiers = stat.Modifiers.Add(durability);
            }
            var levels = durability.Levels.ToArray();
            for (var i = 0; i < 5; i++)
            {
                levels[i] = durability.Levels[i] with { Value = values[i], Info = (values[i] / 1000.0).ToString("0.0") };
            }
            durability.Levels = [.. levels];
        }

        private IWeaponExclusive CreateExclusive(WeaponStats wp, WeaponUpgradeKind kind, float rate)
        {
            rate = MathF.Round(rate / 0.25f) * 0.25f;
            return kind switch
            {
                WeaponUpgradeKind.CriticalRate =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.CriticalRate,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase the critical hit rate by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Critical Hit Rate"),
                        Cost = 80000
                    },
                WeaponUpgradeKind.AmmoCapacity =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.AmmoCapacity,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase ammo capacity by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Ammo Capacity"),
                        Cost = 70000
                    },
                WeaponUpgradeKind.Power =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Power,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase power by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Power"),
                        Cost = 100000
                    },
                WeaponUpgradeKind.Penetration =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Penetration,
                        RateValue = rate,
                        MessageId = _addMessage($"Penetrate through {rate} targets."),
                        PerkMessageId = _addMessage($"{rate}x Penetration Power"),
                        Cost = 70000
                    },
                WeaponUpgradeKind.FireRate =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.FireRate,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase rate of fire by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Rate of Fire"),
                        Cost = 80000
                    },
                WeaponUpgradeKind.Durability =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Durability,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase durability by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Durability"),
                        Cost = 80000
                    },
                WeaponUpgradeKind.CombatSpeed =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.CombatSpeed,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase attack speed by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Attack Speed"),
                        Cost = 60000
                    },
                WeaponUpgradeKind.UnlimitedAmmo =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.UnlimitedAmmo,
                        RateValue = 1,
                        MessageId = _addMessage("Unlimited Ammo"),
                        PerkMessageId = _addMessage("Unlimited Ammo"),
                        Cost = 10000
                    },
                WeaponUpgradeKind.Indestructible =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Indestructible,
                        RateValue = 1,
                        MessageId = _addMessage("Becomes indestructible."),
                        PerkMessageId = _addMessage("Indestructible"),
                        Cost = 10000
                    },
                _ => throw new NotSupportedException()
            };
        }

        private void RandomizePrices(Rng rng, WeaponStats wp, bool randomPrices)
        {
            var group = GetWeaponStatsGroup(wp);
            foreach (var m in wp.Modifiers)
            {
                if (m is IWeaponUpgrade upgrade)
                {
                    if (m.Kind == WeaponUpgradeKind.Power && group?.PowerCost != null)
                        upgrade.Cost = [.. group.PowerCost];
                    if (m.Kind == WeaponUpgradeKind.AmmoCapacity && group?.AmmoCapacityCost != null)
                        upgrade.Cost = [.. group.AmmoCapacityCost];
                    if (m.Kind == WeaponUpgradeKind.ReloadSpeed && group?.ReloadSpeedCost != null)
                        upgrade.Cost = [.. group.ReloadSpeedCost];

                    if (randomPrices)
                    {
                        var scale = rng.NextDouble(0.5, 2);
                        upgrade.Cost = upgrade.Cost.Select(x => (x * scale).RoundPrice()).ToImmutableArray();
                    }
                }
                else if (m is IWeaponExclusive exclusive)
                {
                    if (randomPrices)
                    {
                        exclusive.Cost = rng.Next(5, 15) * 10_000;
                    }
                }
            }
        }

        private void UpdateItemDefinitions(ChainsawRandomizer randomizer)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var itemData = ChainsawItemData.FromRandomizer(randomizer);
            foreach (var item in itemData.Definitions)
            {
                var itemDef = itemRepo.Find(item.ItemId);
                if (itemDef == null)
                    continue;

                if (itemDef.WeaponId != null && _startAmmoCapacity.TryGetValue(itemDef.WeaponId.Value, out var ammoCapacity))
                {
                    item.WeaponDefineData.AmmoMax = ammoCapacity;
                }
            }
            itemData.Save();
        }

        private void UpdateUnlocks(ChainsawRandomizer randomizer, WeaponStatCollection weaponStats)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var path = randomizer.Campaign == Campaign.Leon
                ? "natives/stm/_chainsaw/appsystem/ui/userdata/weaponcustomunlocksettinguserdata.user.2"
                : "natives/stm/_anotherorder/appsystem/ui/userdata/weaponcustomunlocksettinguserdata_ao.user.2";
            var findFlag = randomizer.Campaign == Campaign.Leon
                ? 0
                : 17;
            randomizer.FileRepository.ModifyUserFile(path, (rsz, root) =>
            {
                var userData = rsz.RszParser.Deserialize<chainsaw.WeaponCustomUnlockSettingUserdata>(root);
                foreach (var w in userData._Settings)
                {
                    var itemDefinition = itemRepo.Find(w._ItemId);
                    if (itemDefinition == null)
                        continue;

                    var stats = weaponStats.Weapons.FirstOrDefault(x => x.Id == itemDefinition.WeaponId);
                    if (stats == null)
                        continue;

                    var categories = stats.Modifiers
                        .Where(x => x is not IWeaponExclusive)
                        .Select(x => GetItemCustomCategory(x.Kind))
                        .Order()
                        .ToArray();

                    // Disable all chapters
                    for (var i = 0; i < w._Datas.Count; i++)
                    {
                        w._Datas[i]._IsApply = false;
                    }

                    // Enable first chapter
                    var firstChapterEntry = w._Datas.First(x => x._FlagType == findFlag);
                    firstChapterEntry._IsApply = true;
                    firstChapterEntry._UnlockDatas = categories.Select(x => new chainsaw.WeaponCustomUnlocksingleSetting.UnlockData()
                    {
                        _CustomCategory = x,
                        _UnlockLevel = 4
                    }).ToList();
                }

                rsz.InstanceCopyValues(rsz.ObjectList[0], rsz.RszParser.Serialize(userData));
            });
        }

        private static int GetItemCustomCategory(WeaponUpgradeKind kind)
        {
            return kind switch
            {
                WeaponUpgradeKind.Power or WeaponUpgradeKind.PowerShotGunAround => 0,
                WeaponUpgradeKind.AmmoCapacity => 2,
                WeaponUpgradeKind.CriticalRate => 3,
                WeaponUpgradeKind.Penetration => 4,
                WeaponUpgradeKind.Repair => 7,
                WeaponUpgradeKind.Polish => 8,
                WeaponUpgradeKind.Durability => 9,
                WeaponUpgradeKind.ReloadSpeed => 10,
                WeaponUpgradeKind.FireRate => 11,
                _ => throw new NotSupportedException()
            };
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
        public float[][]? Durability { get; set; }

        [JsonPropertyName("power:cost")]
        public int[]? PowerCost { get; set; }
        [JsonPropertyName("ammoCapacity:cost")]
        public int[]? AmmoCapacityCost { get; set; }
        [JsonPropertyName("reloadSpeed:cost")]
        public int[]? ReloadSpeedCost { get; set; }
    }
}
