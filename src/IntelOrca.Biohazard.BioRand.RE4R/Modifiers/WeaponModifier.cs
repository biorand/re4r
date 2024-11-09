using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Linq;
using System.Text;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using MsgTool;
using static IntelOrca.Biohazard.BioRand.RE4R.Modifiers.WeaponModifier;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal partial class WeaponModifier : Modifier
    {
        private const string WeaponCustomMsgPath = "natives/stm/_chainsaw/message/mes_main_item/ch_mes_main_wpcustom.msg.22";
        private const string ShopMsgPath = "natives/stm/_chainsaw/message/mes_main_sys/ch_mes_main_sys_shop.msg.22";

        private Func<string, Guid> _addMessage = _ => default;
        private Dictionary<(int, WeaponUpgradePath), float> _baseStats = new();

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
            _baseStats.Clear();

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
                    if (randomPrices)
                    {
                        RandomizePrices(rng, wp, randomPrices);
                    }
                });
            }
            weaponStatCollection.Apply();

            randomizer.FileRepository.SetUserFile(GetMainPath(randomizer), mainFile);
            randomizer.FileRepository.SetUserFile(GetDetailPath(randomizer), detailFile);
            randomizer.FileRepository.SetMsgFile(WeaponCustomMsgPath, wpMsg.ToMsg());
            randomizer.FileRepository.SetMsgFile(ShopMsgPath, shopMsg.ToMsg());

            UpdateBaseStats(randomizer);
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
            var exclusives = wp.Modifiers.OfType<IWeaponExclusive>().ToImmutableArray();

            if (RandomizeFromRanges(rng, wp, WeaponUpgradePath.PowerDamage) is StatRange pDamage)
            {
                var pWince = RandomizeFromRanges(rng, wp, WeaponUpgradePath.PowerWince)!.Value;
                var pBreak = RandomizeFromRanges(rng, wp, WeaponUpgradePath.PowerBreak)!.Value;
                var pStopping = RandomizeFromRanges(rng, wp, WeaponUpgradePath.PowerStopping)!.Value;
                var pExplosion = RandomizeFromRanges(rng, wp, WeaponUpgradePath.PowerExplosionRadiusScale);
                var pExplosionSensor = RandomizeFromRanges(rng, wp, WeaponUpgradePath.PowerExplosionSensorRadiusScale);
                RandomizePower(wp, pDamage, pDamage, pWince, pBreak, pStopping, pExplosion, pExplosionSensor);
            }

            // if (RandomizeFromRanges(rng, wp, WeaponUpgradePath.PowerDamage) is StatRange pDamage)
            // {
            //     RandomizePower(wp, pDamage, pDamage, null, null, null, null, null);
            // 
            //     var pWince = RandomizeFromRanges(rng, wp, WeaponUpgradePath.PowerWince)!.Value;
            //     var pBreak = RandomizeFromRanges(rng, wp, WeaponUpgradePath.PowerBreak)!.Value;
            //     var pStopping = RandomizeFromRanges(rng, wp, WeaponUpgradePath.PowerStopping)!.Value;
            //     RandomizePower(wp, pStopping, null, pWince, pBreak, pStopping, null, null, "Increase stopping power.");
            // }

            if (RandomizeFromRanges(rng, wp, WeaponUpgradePath.AmmoCapacity) is StatRange ammoCapacity)
            {
                RandomizeAmmoCapacity(wp, ammoCapacity);
            }

            if (!exclusives.Any(x => x.Kind == WeaponUpgradeKind.CriticalRate) &&
                RandomizeFromRanges(rng, wp, WeaponUpgradePath.CriticalRate) is StatRange criticalRate)
            {
                RandomizeCriticalRate(wp, criticalRate);
            }

            if (!exclusives.Any(x => x.Kind == WeaponUpgradeKind.Penetration) &&
                RandomizeFromRanges(rng, wp, WeaponUpgradePath.Penetration) is StatRange penetration)
            {
                RandomizePenetration(wp, penetration);
            }

            switch (GetReloadSubKind(rng, wp, randomUpgrades))
            {
                case WeaponUpgradePath.ReloadSpeed:
                    var reloadSpeed = RandomizeFromRanges(rng, wp, WeaponUpgradePath.ReloadSpeed);
                    RandomizeReloadSpeed(wp, reloadSpeed!.Value);
                    break;
                case WeaponUpgradePath.ReloadRounds:
                    var reloadRounds = RandomizeFromRanges(rng, wp, WeaponUpgradePath.ReloadRounds);
                    RandomizeReloadRounds(wp, reloadRounds!.Value);
                    break;
            }

            if (RandomizeFromRanges(rng, wp, WeaponUpgradePath.FireRate) is StatRange fireRate)
            {
                RandomizeFireRate(wp, fireRate);
            }

            if (RandomizeFromRanges(rng, wp, WeaponUpgradePath.Durability) is StatRange durability)
            {
                RandomizeDurability(wp, durability);

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

        private WeaponUpgradePath GetReloadSubKind(Rng rng, WeaponStats wp, bool randomUpgrades)
        {
            var hasReloadSpeed = Supports(wp, WeaponUpgradePath.ReloadSpeed);
            var hasReloadRounds = Supports(wp, WeaponUpgradePath.ReloadRounds);
            if (!hasReloadSpeed && !hasReloadRounds)
                return WeaponUpgradePath.None;

            if (!hasReloadRounds)
                return WeaponUpgradePath.ReloadSpeed;

            if (!hasReloadSpeed)
                return WeaponUpgradePath.ReloadRounds;

            if (!randomUpgrades)
            {
                var originalReloadSpeed = wp.Modifiers.OfType<ReloadSpeedUpgrade>().FirstOrDefault();
                if (originalReloadSpeed != null)
                {
                    return originalReloadSpeed.SubType == ReloadSpeedUpgrade.ReloadSpeedRate
                        ? WeaponUpgradePath.ReloadSpeed
                        : WeaponUpgradePath.ReloadRounds;
                }
            }

            return rng.NextOf(WeaponUpgradePath.ReloadSpeed, WeaponUpgradePath.ReloadRounds);
        }

        private void RandomizeExclusives(ChainsawRandomizer randomizer, WeaponStats wp, Rng rng)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var paths = new[]
            {
                WeaponUpgradePath.PowerDamage,
                WeaponUpgradePath.PowerStopping,
                WeaponUpgradePath.AmmoCapacity,
                WeaponUpgradePath.CriticalRate,
                WeaponUpgradePath.Penetration,
                WeaponUpgradePath.FireRate,
                WeaponUpgradePath.Durability,
                WeaponUpgradePath.CombatSpeed
            };

            var exclusives = new List<IWeaponExclusive>();
            foreach (var path in paths)
            {
                var property = $"{WeaponStatTable.GetPropertyName(path)}";
                var min = WeaponStatTable.Default.GetValue(wp.Id, $"{property}/exclusive/min");
                var max = WeaponStatTable.Default.GetValue(wp.Id, $"{property}/exclusive/max");
                if (min == 0)
                    continue;

                if (path == WeaponUpgradePath.PowerDamage)
                {
                    min = (float)Math.Clamp(randomizer.GetConfigOption<double>("weapon-exclusive-power-min"), 1.5, 100);
                    max = (float)Math.Clamp(randomizer.GetConfigOption<double>("weapon-exclusive-power-max"), 1.5, 100);
                }

                var value = rng.NextFloat(min, max);
                exclusives.Add(CreateExclusive(wp, path, value));
            }


            var exclusiveCount = rng.NextOf8020(1, 2, 3, 4);

            // Knives seem to break on hardcore if upgrades are different
            if (itemRepo.FromWeaponId(wp.Id)!.Class == ItemClasses.Knife)
            {
                exclusiveCount = 1;
            }

            var newExclusives = exclusives
                .Shuffle(rng)
                .DistinctBy(x => x.Kind)
                .Take(exclusiveCount)
                .ToImmutableArray();
            wp.Modifiers = wp.Modifiers
                .RemoveAll(x => x is IWeaponExclusive)
                .AddRange(newExclusives);
        }

        private float? GetBaseStat(int wp, WeaponUpgradePath path)
        {
            if (_baseStats.TryGetValue((wp, path), out var value))
                return value;
            return null;
        }

        private void SetBaseStat(WeaponStats wp, WeaponUpgradePath path, float value)
        {
            _baseStats[(wp.Id, path)] = value;
        }

        private bool Supports(WeaponStats wp, WeaponUpgradePath path)
        {
            var property = WeaponStatTable.GetPropertyName(path);
            var table = WeaponStatTable.Default;
            var l1min = table.GetValue(wp.Id, $"{property}/level 1/min");
            if (l1min == 0)
                return false;

            return true;
        }

        private StatRange? RandomizeFromRanges(Rng rng, WeaponStats wp, WeaponUpgradePath path)
        {
            var property = WeaponStatTable.GetPropertyName(path);
            var table = WeaponStatTable.Default;
            var super = rng.NextProbability(5);
            var highRoller = super ? " (high roller)" : "";
            var l1min = table.GetValue(wp.Id, $"{property}/level 1/min");
            if (l1min == 0)
                return null;

            if (table.GetValue(wp.Id, $"{property}/level 5 (high roller)/min") == 0)
                highRoller = "";

            var l1max = table.GetValue(wp.Id, $"{property}/level 1/max");
            var l5min = table.GetValue(wp.Id, $"{property}/level 5{highRoller}/min");
            var l5max = table.GetValue(wp.Id, $"{property}/level 5{highRoller}/max");
            var l1 = r(l1min, l1max);
            var l5 = r(l5min, l5max);
            var values = Enumerable.Range(1, 5).Select(x => lerp(l1, l5, x)).ToArray();
            var cost = Enumerable.Range(1, 5).Select(getCost).ToArray();
            return new StatRange(cost, values);

            float r(float a, float b) => MathF.Round(b < a ? rng.NextFloat(b, a) : rng.NextFloat(a, b), 2);
            float lerp(float a, float b, int level)
            {
                var t = (level - 1) / 4.0f;
                return MathF.Round(a + ((b - a) * t), 2);
            }
            int getCost(int level)
            {
                var value = table.GetValue(wp.Id, $"{property} cost/level {level}");
                if (value != 0)
                    return (int)value;

                var upgrade = wp.Modifiers
                    .OfType<IWeaponUpgrade>()
                    .FirstOrDefault(x => x.Kind == GetUpgradeKind(path));
                if (upgrade != null)
                    return Math.Max(0, upgrade.Cost[level - 1]);
                return 0;
            }
        }

        private static WeaponUpgradeKind GetUpgradeKind(WeaponUpgradePath path)
        {
            return path switch
            {
                WeaponUpgradePath.PowerDamage => WeaponUpgradeKind.Power,
                WeaponUpgradePath.PowerWince => WeaponUpgradeKind.Power,
                WeaponUpgradePath.PowerBreak => WeaponUpgradeKind.Power,
                WeaponUpgradePath.PowerStopping => WeaponUpgradeKind.Power,
                WeaponUpgradePath.PowerExplosionRadiusScale => WeaponUpgradeKind.Power,
                WeaponUpgradePath.PowerExplosionSensorRadiusScale => WeaponUpgradeKind.Power,
                WeaponUpgradePath.CriticalRate => WeaponUpgradeKind.CriticalRate,
                WeaponUpgradePath.Penetration => WeaponUpgradeKind.Penetration,
                WeaponUpgradePath.AmmoCapacity => WeaponUpgradeKind.AmmoCapacity,
                WeaponUpgradePath.ReloadSpeed => WeaponUpgradeKind.ReloadSpeed,
                WeaponUpgradePath.ReloadRounds => WeaponUpgradeKind.ReloadSpeed,
                WeaponUpgradePath.FireRate => WeaponUpgradeKind.FireRate,
                WeaponUpgradePath.CombatSpeed => WeaponUpgradeKind.CombatSpeed,
                WeaponUpgradePath.Durability => WeaponUpgradeKind.Durability,
                _ => throw new NotSupportedException(),
            };
        }

        private void RandomizePower(
            WeaponStats stat,
            StatRange pMain,
            StatRange? pDamage,
            StatRange? pWince,
            StatRange? pBreak,
            StatRange? pStopping,
            StatRange? pExplosion,
            StatRange? pExplosionSensor,
            string? message = null)
        {
            var power = new PowerUpgrade();
            if (message == null)
            {
                power = stat.Modifiers.OfType<PowerUpgrade>().First();
            }
            else
            {
                power.MessageId = _addMessage(message);
                stat.Modifiers = stat.Modifiers.Add(power);
            }

            var multiplier = message != null ? 1 : float.Parse(power.Levels[0].Info);
            var levels = Enumerable.Range(0, 5)
                .Select(i => new PowerUpgradeLevel(
                    pMain.Cost[i],
                    (pMain.Values[i] * multiplier).ToString("0.00"),
                    pDamage?.Values[i] ?? 0,
                    pWince?.Values[i] ?? 0,
                    pBreak?.Values[i] ?? 0,
                    pStopping?.Values[i] ?? 0,
                    pExplosion?.Values[i] ?? 0,
                    pExplosionSensor?.Values[i] ?? 0))
                .ToImmutableArray();
            power.Levels = [.. levels];

            setBaseStat(WeaponUpgradePath.PowerDamage, pDamage);
            setBaseStat(WeaponUpgradePath.PowerWince, pWince);
            setBaseStat(WeaponUpgradePath.PowerBreak, pBreak);
            setBaseStat(WeaponUpgradePath.PowerStopping, pStopping);
            setBaseStat(WeaponUpgradePath.PowerExplosionRadiusScale, pExplosion);
            setBaseStat(WeaponUpgradePath.PowerExplosionSensorRadiusScale, pExplosionSensor);

            void setBaseStat(WeaponUpgradePath path, StatRange? sr)
            {
                if (sr != null)
                {
                    SetBaseStat(stat, path, sr.Value.Values[0]);
                }
            }
        }

        private void RandomizeAmmoCapacity(WeaponStats stat, StatRange sr)
        {
            var ammoCapacity = stat.Modifiers.OfType<AmmoCapacityUpgrade>().First();
            var levels = ammoCapacity.Levels.ToArray();
            for (var i = 0; i < 5; i++)
            {
                var capacity = (int)Math.Round(sr.Values[i]);
                levels[i] = ammoCapacity.Levels[i] with
                {
                    Cost = sr.Cost[i],
                    Value = capacity,
                    Info = capacity.ToString()
                };

                if (i == 0)
                    SetBaseStat(stat, WeaponUpgradePath.AmmoCapacity, capacity);
            }
            ammoCapacity.Levels = [.. levels];
        }

        private void RandomizeCriticalRate(WeaponStats stat, StatRange sr)
        {
            var m = new CriticalRateUpgrade
            {
                MessageId = _addMessage("Increase critical hit rate."),
                Levels = Enumerable.Range(0, 5).Select(i =>
                {
                    var value = (int)MathF.Round(sr.Values[i]);
                    return new CriticalRateUpgradeLevel(sr.Cost[i], $"{value}%", value);
                }).ToImmutableArray()
            };
            stat.Modifiers = stat.Modifiers.Add(m);
            SetBaseStat(stat, WeaponUpgradePath.CriticalRate, m.Levels[0].Value);
        }

        private void RandomizePenetration(WeaponStats stat, StatRange sr)
        {
            stat.Modifiers = stat.Modifiers.Add(new PenetrationUpgrade
            {
                MessageId = _addMessage("Increase penetration."),
                Levels = Enumerable.Range(0, 5).Select(i =>
                {
                    var value = (int)MathF.Round(sr.Values[i]);
                    return new PenetrationUpgradeLevel(sr.Cost[i], value.ToString(), value);
                }).ToImmutableArray()
            });
        }

        private void RandomizeReloadSpeed(WeaponStats stat, StatRange sr)
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
                var value = MathF.Round(original * sr.Values[i], 2);
                var infoMultiplier = originalInfo / original;
                var info = infoMultiplier * value;

                levels[i] = reloadSpeed.Levels[i] with { Num = 0, Speed = value, Info = info.ToString("0.00") };
            }
            reloadSpeed.Levels = [.. levels];
        }

        private void RandomizeReloadRounds(WeaponStats stat, StatRange sr)
        {
            var reloadSpeed = stat.Modifiers.OfType<ReloadSpeedUpgrade>().First();
            reloadSpeed.MessageId = new Guid("173bfc85-dbf2-4d39-8ba9-6e5284990c63");
            var levels = reloadSpeed.Levels.ToArray();
            for (var i = 0; i < 5; i++)
            {
                var rounds = (int)MathF.Round(sr.Values[i]);
                levels[i] = reloadSpeed.Levels[i] with { Num = rounds, Speed = 0, Info = rounds.ToString() };
            }
            reloadSpeed.Levels = [.. levels];
        }

        private void RandomizeFireRate(WeaponStats stat, StatRange sr)
        {
            var fireRate = stat.Modifiers.OfType<FireRateUpgrade>().First();
            var levels = fireRate.Levels.ToArray();
            var infoMultiplier = float.Parse(levels[0].Info) / levels[0].Speed;
            for (var i = 0; i < 5; i++)
            {
                var value = MathF.Round(levels[0].Speed * sr.Values[i], 2);
                var info = (levels[0].Speed + levels[0].Speed - levels[i].Speed) * infoMultiplier;
                levels[i] = fireRate.Levels[i] with
                {
                    Cost = sr.Cost[i],
                    Speed = value,
                    Info = info.ToString("0.00")
                };
            }
            fireRate.Levels = [.. levels];
        }

        private void RandomizeDurability(WeaponStats stat, StatRange sr)
        {
            var durability = stat.Modifiers.OfType<DurabilityUpgrade>().FirstOrDefault();
            if (durability == null)
            {
                durability = new DurabilityUpgrade
                {
                    MessageId = _addMessage("Increase durability."),
                    Levels = Enumerable.Range(0, 5)
                        .Select(i => new DurabilityUpgradeLevel(0, "0.0", 0))
                        .ToImmutableArray()
                };
                stat.Modifiers = stat.Modifiers.Add(durability);
            }

            var levels = durability.Levels.ToArray();
            for (var i = 0; i < 5; i++)
            {
                var value = (int)MathF.Round(sr.Values[i] / 100) * 100;
                levels[i] = durability.Levels[i] with
                {
                    Value = value,
                    Info = (value / 1000.0).ToString("0.0")
                };
            }
            durability.Levels = [.. levels];
        }

        private IWeaponExclusive CreateExclusive(WeaponStats wp, WeaponUpgradePath path, float rate)
        {
            rate = MathF.Round(rate / 0.25f) * 0.25f;
            return path switch
            {
                WeaponUpgradePath.PowerDamage =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Power,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase damage by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Damage"),
                        Cost = 100000
                    }.WithPower(rate, 1, 1, 1),
                WeaponUpgradePath.PowerStopping =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Power,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase stopping power by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Stopping Power"),
                        Cost = 100000
                    }.WithPower(1, rate, 1, rate),
                WeaponUpgradePath.CriticalRate =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.CriticalRate,
                        RateValue = MathF.Round(rate),
                        MessageId = _addMessage($"Increase the critical hit rate by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Critical Hit Rate"),
                        Cost = 80000
                    },
                WeaponUpgradePath.Penetration =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Penetration,
                        RateValue = MathF.Round(rate),
                        MessageId = _addMessage($"Penetrate through {rate} targets."),
                        PerkMessageId = _addMessage($"{rate}x Penetration Power"),
                        Cost = 70000
                    },
                WeaponUpgradePath.AmmoCapacity =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.AmmoCapacity,
                        RateValue = MathF.Round(rate),
                        MessageId = _addMessage($"Increase ammo capacity by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Ammo Capacity"),
                        Cost = 70000
                    },
                WeaponUpgradePath.FireRate =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.FireRate,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase rate of fire by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Rate of Fire"),
                        Cost = 80000
                    },
                WeaponUpgradePath.Durability =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.Durability,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase durability by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Durability"),
                        Cost = 80000
                    },
                WeaponUpgradePath.CombatSpeed =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.CombatSpeed,
                        RateValue = rate,
                        MessageId = _addMessage($"Increase attack speed by {rate}x."),
                        PerkMessageId = _addMessage($"{rate}x Attack Speed"),
                        Cost = 60000
                    },
                WeaponUpgradePath.UnlimitedAmmo =>
                    new WeaponExclusive
                    {
                        Kind = WeaponUpgradeKind.UnlimitedAmmo,
                        RateValue = 1,
                        MessageId = _addMessage("Unlimited Ammo"),
                        PerkMessageId = _addMessage("Unlimited Ammo"),
                        Cost = 10000
                    },
                WeaponUpgradePath.Indestructible =>
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
            foreach (var m in wp.Modifiers)
            {
                if (m is IWeaponUpgrade upgrade)
                {
                    var scale = rng.NextDouble(0.5, 2);
                    upgrade.Cost = upgrade.Cost.Select(x => (x * scale).RoundPrice()).ToImmutableArray();
                }
                else if (m is IWeaponExclusive exclusive)
                {
                    exclusive.Cost = rng.Next(5, 15) * 10_000;
                }
            }
        }

        private void UpdateBaseStats(ChainsawRandomizer randomizer)
        {
            UpdateBaseAmmoCapacity(randomizer);

            if (randomizer.Campaign == Campaign.Ada)
                return;

            var patterns = new string[] {
                "natives/stm/_chainsaw/appsystem/shell/bullet/wp{0}/wp{0}shellinfo.user.2",
                "natives/stm/_chainsaw/appsystem/shell/bullet/wp{0}/wp{0}shellinfo_around.user.2",
                "natives/stm/_chainsaw/appsystem/shell/bullet/wp{0}/wp{0}shellinfo_center.user.2",
            };

            var fileRepository = randomizer.FileRepository;
            foreach (var wpGroup in _baseStats.GroupBy(x => x.Key.Item1))
            {
                var wp = wpGroup.Key;
                foreach (var p in patterns)
                {
                    var filePath = string.Format(p, wp);
                    if (!fileRepository.Exists(filePath))
                        continue;

                    randomizer.FileRepository.ModifyUserFile(filePath, (rsz, root) =>
                    {
                        foreach (var kvp in wpGroup)
                        {
                            var path = kvp.Key.Item2;
                            var value = kvp.Value;
                            switch (path)
                            {
                                case WeaponUpgradePath.PowerDamage:
                                    root.Set("_AttackInfo._DamageRate._BaseValue", value);
                                    break;
                                case WeaponUpgradePath.PowerWince:
                                    root.Set("_AttackInfo._WinceRate._BaseValue", value);
                                    break;
                                case WeaponUpgradePath.PowerBreak:
                                    root.Set("_AttackInfo._BreakRate._BaseValue", value);
                                    break;
                                case WeaponUpgradePath.PowerStopping:
                                    root.Set("_AttackInfo._StoppingRate._BaseValue", value);
                                    break;
                                case WeaponUpgradePath.CriticalRate:
                                    root.Set("_AttackInfo._CriticalRate", value);
                                    root.Set("_AttackInfo._CriticalRate_Fit", value);
                                    break;
                            }
                        }
                    });
                }
            }

            foreach (var kvp in _baseStats)
            {
                var wp = kvp.Key.Item1;
                var path = kvp.Key.Item2;
                var value = kvp.Value;


            }
        }

        private void UpdateBaseAmmoCapacity(ChainsawRandomizer randomizer)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var itemData = ChainsawItemData.FromRandomizer(randomizer);
            foreach (var item in itemData.Definitions)
            {
                var itemDef = itemRepo.Find(item.ItemId);
                if (itemDef == null)
                    continue;

                if (itemDef.WeaponId != null && GetBaseStat(itemDef.WeaponId.Value, WeaponUpgradePath.AmmoCapacity) is float ammoCapacity)
                {
                    item.WeaponDefineData.AmmoMax = (int)ammoCapacity;
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
    }

    internal readonly struct StatRange
    {
        public int[] Cost { get; }
        public float[] Values { get; }

        public StatRange(int[] cost, float[] values)
        {
            Cost = cost;
            Values = values;
        }
    }

    internal sealed class WeaponStatTable
    {
        public static WeaponStatTable Default { get; } = new WeaponStatTable(Resources.wpstats);

        private readonly string[][] _cells;
        private readonly Dictionary<string, int> _rowMap = new();
        private readonly Dictionary<int, int> _colMap = new();

        private WeaponStatTable(byte[] wpstats)
        {
            var content = Encoding.UTF8.GetString(wpstats);
            var lines = content.ReplaceLineEndings("\n").Split('\n');
            _cells = lines.Select(x => x.Split(',').Select(x => x.Trim()).ToArray()).ToArray();

            _rowMap = new Dictionary<string, int>();
            for (var i = 0; i < _cells.Length; i++)
            {
                var propertyName = string.Join("/", _cells[i].Take(3).Where(x => x != ""));
                if (propertyName == "")
                    continue;

                _rowMap[propertyName] = i;

                if (propertyName == "id")
                {
                    var ids = _cells[i].Skip(3).Select(int.Parse).ToArray();
                    for (var j = 0; j < ids.Length; j++)
                    {
                        _colMap[ids[j]] = 3 + j;
                    }
                }
            }
        }

        public float GetValue(int wp, string property)
        {
            if (!_colMap.TryGetValue(wp, out var columnIndex))
                return 0;

            if (!_rowMap.TryGetValue(property, out var rowIndex))
                return 0;

            var cell = _cells[rowIndex][columnIndex];
            if (cell == "")
                return 0;

            return float.Parse(cell);
        }

        public static string GetPropertyName(WeaponUpgradePath path)
        {
            return path switch
            {
                WeaponUpgradePath.PowerDamage => "damage",
                WeaponUpgradePath.PowerWince => "wince",
                WeaponUpgradePath.PowerBreak => "break",
                WeaponUpgradePath.PowerStopping => "stopping",
                WeaponUpgradePath.PowerExplosionRadiusScale => "explosion radius scale",
                WeaponUpgradePath.PowerExplosionSensorRadiusScale => "explosion sensor radius scale",
                WeaponUpgradePath.CriticalRate => "critical rate",
                WeaponUpgradePath.Penetration => "penetration",
                WeaponUpgradePath.AmmoCapacity => "ammo capacity",
                WeaponUpgradePath.ReloadSpeed => "reload speed",
                WeaponUpgradePath.ReloadRounds => "reload rounds",
                WeaponUpgradePath.FireRate => "fire rate",
                WeaponUpgradePath.CombatSpeed => "combat speed",
                WeaponUpgradePath.Durability => "durability",
                _ => throw new NotSupportedException()
            };
        }
    }
}
