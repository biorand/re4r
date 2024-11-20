using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;
using static chainsaw.ShellBaseAttackInfo;
using static chainsaw.WeaponCustomUserdata;
using static chainsaw.WeaponDetailCustomUserdata;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal partial class WeaponModifier
    {
        internal enum WeaponUpgradeKind
        {
            Invalid = -1,
            Power,
            PowerShotGunAround,
            CriticalRate,
            Penetration,
            AmmoCapacity,
            ReloadSpeed,
            FireRate,
            CombatSpeed,
            Durability,
            UnlimitedAmmo,
            Indestructible,
            Repair,
            Polish,
        }

        internal enum WeaponUpgradePath
        {
            None,
            PowerDamage,
            PowerWince,
            PowerBreak,
            PowerStopping,
            PowerExplosionRadiusScale,
            PowerExplosionSensorRadiusScale,
            CriticalRate,
            Penetration,
            AmmoCapacity,
            ReloadSpeed,
            ReloadRounds,
            FireRate,
            CombatSpeed,
            Durability,
            UnlimitedAmmo,
            Indestructible,
        }

        internal class Categories
        {
            public const int Power = 0;
            public const int Stabilization = 1;
            public const int AmmoCapacity = 2;

            public const int CriticalRate = 0;
            public const int Penetration = 1;
            public const int UsableAmmoList = 2;
            public const int Reload = 3;
            public const int Repair = 4;
            public const int Polish = 5;
            public const int Durability = 6;
            public const int ReloadSpeed = 7;
            public const int FireRate = 8;
            public const int AmmoCost = 9;
            public const int FlameDistance = 10;
            public const int Others = 11;
        }

        internal interface IWeaponModifier
        {
            WeaponUpgradeKind Kind { get; }
            object Main { get; }
            object? Detail { get; }
            Guid MessageId { get; }
        }

        internal interface IWeaponRepair : IWeaponModifier
        {
        }

        internal interface IWeaponUpgrade : IWeaponModifier
        {
            IReadOnlyList<IWeaponUpgradeLevel> Levels { get; }
            ImmutableArray<int> Cost { get; set; }
        }

        internal interface IWeaponExclusive : IWeaponModifier
        {
            public Guid PerkMessageId { get; }
            public float RateValue { get; }
            public int Cost { get; set; }
        }

        internal interface IWeaponUpgradeLevel
        {
            int Cost { get; }
            string Info { get; }
        }

        internal class WeaponStatCollection
        {
            private readonly UserFile _main;
            private readonly UserFile _detail;
            private WeaponCustomUserdata _userDataMain;
            private WeaponDetailCustomUserdata _userDataDetail;
            private ImmutableArray<WeaponStats> _weapons;

            public WeaponStatCollection(UserFile main, UserFile detail)
            {
                _main = main;
                _detail = detail;
                _userDataMain = main.RszParser.Deserialize<WeaponCustomUserdata>(_main.RSZ!.ObjectList[0]);
                _userDataDetail = detail.RszParser.Deserialize<WeaponDetailCustomUserdata>(_detail.RSZ!.ObjectList[0]);

                var weapons = ImmutableArray.CreateBuilder<WeaponStats>();
                foreach (var wpMain in _userDataMain._WeaponStages)
                {
                    var wpDetail = _userDataDetail._WeaponDetailStages.First(x => x._WeaponID == wpMain._WeaponID);
                    weapons.Add(new WeaponStats(wpMain, wpDetail));
                }
                _weapons = weapons.ToImmutable();
            }

            public void Apply()
            {
                _userDataMain._WeaponStages = _weapons.Select(x => x.Main).ToList();
                _userDataDetail._WeaponDetailStages = _weapons.Select(x => x.Detail).ToList();
                _main.RSZ!.InstanceCopyValues(_main.RSZ!.ObjectList[0], _main.RszParser.Serialize(_userDataMain));
                _detail.RSZ!.InstanceCopyValues(_detail.RSZ!.ObjectList[0], _detail.RszParser.Serialize(_userDataDetail));
            }

            public ImmutableArray<WeaponStats> Weapons => _weapons;
        }

        internal class WeaponStats
        {
            private readonly RaderChartGuiSingleSettingData _radar;
            private readonly ImmutableArray<AttachmentCustom> _attachments;

            public int Id { get; }
            public ImmutableArray<IWeaponModifier> Modifiers { get; set; }
            public ItemDefinition? ItemDefinition => ItemDefinitionRepository.Default.FromWeaponId(Id);

            public WeaponStats(WeaponStage main, WeaponDetailStage detail)
            {
                Id = main._WeaponID;
                _radar = main._RaderChartGuiSingleSettingData;
                _attachments = [.. detail._WeaponDetailCustom._AttachmentCustoms];

                var modifiers = ImmutableArray.CreateBuilder<IWeaponModifier>();
                foreach (var c in main._WeaponCustom._Commons)
                {
                    var d = detail._WeaponDetailCustom._CommonCustoms.FirstOrDefault(x => x._CommonCustomCategory == c._CommonCustomCategory);
                    if (d == null)
                        continue;

                    modifiers.Add(c._CommonCustomCategory switch
                    {
                        Categories.Power => new PowerUpgrade(c._CustomAttackUp, d._AttackUp),
                        Categories.AmmoCapacity => new AmmoCapacityUpgrade(c._CustomAmmoMaxUp, d._AmmoMaxUp),
                        _ => throw new NotSupportedException()
                    });
                }
                foreach (var i in main._WeaponCustom._Individuals)
                {
                    var d = detail._WeaponDetailCustom._IndividualCustoms.FirstOrDefault(x => x._IndividualCustomCategory == i._IndividualCustomCategory);
                    d ??= new IndividualCustom();

                    if (i._IndividualCustomCategory == Categories.Reload)
                        continue;

                    modifiers.Add(i._IndividualCustomCategory switch
                    {
                        Categories.CriticalRate => new CriticalRateUpgrade(i._CustomCriticalRate, d._CriticalRate),
                        Categories.Penetration => new PenetrationUpgrade(i._CustomThroughNum, d._ThroughNums),
                        Categories.UsableAmmoList => throw new NotSupportedException(),
                        Categories.Reload => throw new NotSupportedException(),
                        Categories.Repair => new RepairUpgrade(i._CustomRepair),
                        Categories.Polish => new PolishUpgrade(i._CustomPolish),
                        Categories.Durability => new DurabilityUpgrade(i._CustomStrength, d._Strength),
                        Categories.ReloadSpeed => new ReloadSpeedUpgrade(i._CustomReloadSpeed, d._ReloadSpeed),
                        Categories.FireRate => new FireRateUpgrade(i._CustomRapid, d._Rapid),
                        Categories.AmmoCost => throw new NotSupportedException(),
                        Categories.FlameDistance => throw new NotSupportedException(),
                        Categories.Others => throw new NotSupportedException(),
                        _ => throw new NotSupportedException()
                    });
                }
                foreach (var e in main._WeaponCustom._LimitBreak)
                {
                    var d = detail._WeaponDetailCustom._LimitBreakCustoms.FirstOrDefault(x => x._LimitBreakCustomCategory == e._LimitBreakCustomCategory);
                    if (d == null)
                        continue;

                    modifiers.Add(new WeaponExclusive(e, d));
                }
                Modifiers = modifiers.ToImmutable();
            }

            public WeaponStage Main
            {
                get
                {
                    var raws = Modifiers.Select(x => x.Main).ToArray();
                    return new WeaponStage()
                    {
                        _WeaponID = Id,
                        _RaderChartGuiSingleSettingData = _radar,
                        _WeaponCustom = new WeaponCustom()
                        {
                            _Commons = raws.OfType<Common>().ToList(),
                            _Individuals = raws.OfType<Individual>().ToList(),
                            _LimitBreak = raws.OfType<LimitBreak>().ToList()
                        }
                    };
                }
            }

            public WeaponDetailStage Detail
            {
                get
                {
                    var raws = Modifiers
                        .Select(x => x.Detail).ToArray();
                    return new WeaponDetailStage()
                    {
                        _WeaponID = Id,
                        _WeaponDetailCustom = new WeaponDetailCustom()
                        {
                            _CommonCustoms = raws.OfType<CommonCustom>().ToList(),
                            _IndividualCustoms = raws.OfType<IndividualCustom>().ToList(),
                            _AttachmentCustoms = _attachments.ToList(),
                            _LimitBreakCustoms = raws.OfType<LimitBreakCustom>().ToList()
                        }
                    };
                }
            }

            public string Name => ItemDefinitionRepository.Default.FromWeaponId(Id)?.Name ?? $"wp{Id}";

            public override string ToString() => Name;
        }

        internal class PowerUpgrade(CustomAttackUp main, AttackUp detail) : IWeaponUpgrade
        {
            public PowerUpgrade() : this(new CustomAttackUp(), new AttackUp()) { }

            public WeaponUpgradeKind Kind => WeaponUpgradeKind.Power;
            public object Main => new Common()
            {
                _CommonCustomCategory = Categories.Power,
                _CustomAttackUp = main
            };
            public object Detail => new CommonCustom()
            {
                _CommonCustomCategory = Categories.Power,
                _AttackUp = detail
            };
            public Guid MessageId
            {
                get => main._MessageId;
                set => main._MessageId = value;
            }
            public ImmutableArray<PowerUpgradeLevel> Levels
            {
                get
                {
                    var result = ImmutableArray.CreateBuilder<PowerUpgradeLevel>();
                    for (var i = 0; i < main._AttackUpCustomStages.Count; i++)
                    {
                        var cost = main._AttackUpCustomStages[i]._Cost;
                        var info = main._AttackUpCustomStages[i]._Info;
                        result.Add(new PowerUpgradeLevel(
                            cost,
                            info,
                            GetValue(detail._DamageRates, i),
                            GetValue(detail._WinceRates, i),
                            GetValue(detail._BreakRates, i),
                            GetValue(detail._StoppingRates, i),
                            GetValue(detail._ExplosionRadiusScale, i),
                            GetValue(detail._ExplosionSensorRadiusScale, i)));
                    }
                    return result.ToImmutable();
                }
                set
                {
                    main._AttackUpCustomStages.Resize(value.Length);
                    for (var i = 0; i < main._AttackUpCustomStages.Count; i++)
                    {
                        main._AttackUpCustomStages[i] ??= new AttackUpCustomStage();
                        main._AttackUpCustomStages[i]._Cost = value[i].Cost;
                        main._AttackUpCustomStages[i]._Info = value[i].Info;

                        main._AttackUpCustomStages[i]._AttackUpParams.Clear();
                        if (i != 0)
                        {
                            var entries = new[] {
                                value[i].Damage,
                                value[i].Wince,
                                value[i].Break,
                                value[i].Stopping,
                                value[i].ExplosionRadiusScale,
                                value[i].ExplosionSensorRadiusScale
                            };
                            for (var j = 0; j < entries.Length; j++)
                            {
                                if (entries[j] != 0)
                                {
                                    main._AttackUpCustomStages[i]._AttackUpParams.Add(new AttackUpParam()
                                    {
                                        _Level = i,
                                        _AttackUp = j
                                    });
                                }
                            }
                        }

                        if (value[i].Damage != 0)
                            SetValue(detail._DamageRates, i, value[i].Damage);
                        if (value[i].Wince != 0)
                            SetValue(detail._WinceRates, i, value[i].Wince);
                        if (value[i].Break != 0)
                            SetValue(detail._BreakRates, i, value[i].Break);
                        if (value[i].Stopping != 0)
                            SetValue(detail._StoppingRates, i, value[i].Stopping);
                        if (value[i].ExplosionRadiusScale != 0)
                            SetValue(detail._ExplosionRadiusScale, i, value[i].ExplosionRadiusScale);
                        if (value[i].ExplosionSensorRadiusScale != 0)
                            SetValue(detail._ExplosionSensorRadiusScale, i, value[i].ExplosionSensorRadiusScale);
                    }
                }
            }
            IReadOnlyList<IWeaponUpgradeLevel> IWeaponUpgrade.Levels => Levels;
            public ImmutableArray<int> Cost
            {
                get => Levels.Select(x => x.Cost).ToImmutableArray();
                set => Levels = Levels.Zip(value).Select(x => x.First with { Cost = x.Second }).ToImmutableArray();
            }

            private static float GetValue(List<CurveVariable> list, int index) => list.Count > index ? list[index]._BaseValue : 0;
            private static float GetValue(List<float> list, int index) => list.Count > index ? list[index] : 0;
            private static void SetValue(List<CurveVariable> list, int index, float value)
            {
                if (list.Count <= index)
                    list.Resize(index + 1);

                list[index] ??= new CurveVariable();
                list[index]._BaseValue = value;
            }
            private static void SetValue(List<float> list, int index, float value)
            {
                list.SetItem(index, value);
            }
        }

        internal record PowerUpgradeLevel(
            int Cost,
            string Info,
            float Damage,
            float Wince,
            float Break,
            float Stopping,
            float ExplosionRadiusScale,
            float ExplosionSensorRadiusScale) : IWeaponUpgradeLevel
        {
        }

        internal class AmmoCapacityUpgrade(CustomAmmoMaxUp main, AmmoMaxUp detail) : IWeaponUpgrade
        {
            public WeaponUpgradeKind Kind => WeaponUpgradeKind.AmmoCapacity;
            public object Main => new Common()
            {
                _CommonCustomCategory = Categories.AmmoCapacity,
                _CustomAmmoMaxUp = main
            };
            public object Detail => new CommonCustom()
            {
                _CommonCustomCategory = Categories.AmmoCapacity,
                _AmmoMaxUp = detail
            };
            public Guid MessageId => main._MessageId;
            public ImmutableArray<AmmoUpgradeLevel> Levels
            {
                get
                {
                    var result = ImmutableArray.CreateBuilder<AmmoUpgradeLevel>();
                    for (var i = 0; i < main._AmmoMaxUpCustomStages.Count; i++)
                    {
                        var cost = main._AmmoMaxUpCustomStages[i]._Cost;
                        var info = main._AmmoMaxUpCustomStages[i]._Info;
                        var value = detail._AmmoMaxs.Count > i
                            ? detail._AmmoMaxs[i]
                            : detail._ReloadNum.Count > i
                                ? detail._ReloadNum[i]
                                : 0;
                        result.Add(new AmmoUpgradeLevel(cost, info, value));
                    }
                    return result.ToImmutable();
                }
                set
                {
                    main._AmmoMaxUpCustomStages.Resize(value.Length);
                    detail._AmmoMaxs.Resize(value.Length);
                    for (var i = 0; i < main._AmmoMaxUpCustomStages.Count; i++)
                    {
                        var l = main._AmmoMaxUpCustomStages[i];
                        l._Cost = value[i].Cost;
                        l._Info = value[i].Info;
                        detail._AmmoMaxs[i] = value[i].Value;
                    }
                }
            }
            public ImmutableArray<int> Cost
            {
                get => Levels.Select(x => x.Cost).ToImmutableArray();
                set => Levels = Levels.Zip(value).Select(x => x.First with { Cost = x.Second }).ToImmutableArray();
            }
            IReadOnlyList<IWeaponUpgradeLevel> IWeaponUpgrade.Levels => Levels;
        }

        internal record AmmoUpgradeLevel(int Cost, string Info, int Value) : IWeaponUpgradeLevel
        {
        }

        internal class RepairUpgrade(CustomRepair main) : IWeaponRepair
        {
            public WeaponUpgradeKind Kind => WeaponUpgradeKind.Repair;
            public object Main => new Individual()
            {
                _IndividualCustomCategory = Categories.Repair,
                _CustomRepair = main
            };
            public object? Detail => null;
            public Guid MessageId => main._MessageId;
        }

        internal record RepairUpgradeLevel(int Cost, string Info) : IWeaponUpgradeLevel
        {
        }

        internal class PolishUpgrade(CustomPolish main) : IWeaponRepair
        {
            public WeaponUpgradeKind Kind => WeaponUpgradeKind.Polish;
            public object Main => new Individual()
            {
                _IndividualCustomCategory = Categories.Polish,
                _CustomPolish = main
            };
            public object? Detail => null;
            public Guid MessageId => main._MessageId;
        }

        internal record PolishUpgradeLevel(int Cost, string Info) : IWeaponUpgradeLevel
        {
        }

        internal class DurabilityUpgrade(CustomStrength main, Strength detail) : IWeaponUpgrade
        {
            public DurabilityUpgrade() : this(new CustomStrength(), new Strength()) { }

            public WeaponUpgradeKind Kind => WeaponUpgradeKind.Durability;
            public object Main => new Individual()
            {
                _IndividualCustomCategory = Categories.Durability,
                _CustomStrength = main
            };
            public object Detail => new IndividualCustom()
            {
                _IndividualCustomCategory = Categories.Durability,
                _Strength = detail
            };
            public Guid MessageId
            {
                get => main._MessageId;
                set => main._MessageId = value;
            }
            public ImmutableArray<DurabilityUpgradeLevel> Levels
            {
                get
                {
                    var result = ImmutableArray.CreateBuilder<DurabilityUpgradeLevel>();
                    for (var i = 0; i < main._StrengthCustomStages.Count; i++)
                    {
                        result.Add(new DurabilityUpgradeLevel(
                            main._StrengthCustomStages[i]._Cost,
                            main._StrengthCustomStages[i]._Info,
                            detail._DurabilityMaxes.GetItem(i)));
                    }
                    return result.ToImmutable();
                }
                set
                {
                    main._StrengthCustomStages.Resize(value.Length);
                    detail._DurabilityMaxes.Resize(value.Length);
                    for (var i = 0; i < main._StrengthCustomStages.Count; i++)
                    {
                        main._StrengthCustomStages[i] ??= new StrengthCustomStage();
                        main._StrengthCustomStages[i]._Cost = value[i].Cost;
                        main._StrengthCustomStages[i]._Info = value[i].Info;
                        detail._DurabilityMaxes[i] = value[i].Value;
                    }
                }
            }
            public ImmutableArray<int> Cost
            {
                get => Levels.Select(x => x.Cost).ToImmutableArray();
                set => Levels = Levels.Zip(value).Select(x => x.First with { Cost = x.Second }).ToImmutableArray();
            }
            IReadOnlyList<IWeaponUpgradeLevel> IWeaponUpgrade.Levels => Levels;
        }

        internal record DurabilityUpgradeLevel(int Cost, string Info, int Value) : IWeaponUpgradeLevel
        {
        }

        internal class CriticalRateUpgrade(CustomCriticalRate main, CriticalRate detail) : IWeaponUpgrade
        {
            public CriticalRateUpgrade() : this(new CustomCriticalRate(), new CriticalRate()) { }
            public WeaponUpgradeKind Kind => WeaponUpgradeKind.CriticalRate;
            public object Main => new Individual()
            {
                _IndividualCustomCategory = Categories.CriticalRate,
                _CustomCriticalRate = main
            };
            public object Detail => new IndividualCustom()
            {
                _IndividualCustomCategory = Categories.CriticalRate,
                _CriticalRate = detail
            };
            public Guid MessageId
            {
                get => main._MessageId;
                set => main._MessageId = value;
            }
            public ImmutableArray<CriticalRateUpgradeLevel> Levels
            {
                get
                {
                    var result = ImmutableArray.CreateBuilder<CriticalRateUpgradeLevel>();
                    for (var i = 0; i < main._CriticalRateCustomStages.Count; i++)
                    {
                        var cost = main._CriticalRateCustomStages[i]._Cost;
                        var info = main._CriticalRateCustomStages[i]._Info;
                        var value = detail._CriticalRate_Normal.Count > i
                            ? detail._CriticalRate_Normal[i]
                            : 0;
                        result.Add(new CriticalRateUpgradeLevel(cost, info, value));
                    }
                    return result.ToImmutable();
                }
                set
                {
                    main._CriticalRateCustomStages.Resize(value.Length);
                    detail._CriticalRate_Normal.Resize(value.Length);
                    detail._CriticalRate_Fit.Resize(value.Length);
                    for (var i = 0; i < main._CriticalRateCustomStages.Count; i++)
                    {
                        main._CriticalRateCustomStages[i] ??= new CriticalRateCustomStage();
                        main._CriticalRateCustomStages[i]._Cost = value[i].Cost;
                        main._CriticalRateCustomStages[i]._Info = value[i].Info;
                        if (i != 0)
                        {
                            main._CriticalRateCustomStages[i]._CriticalRateParams =
                            [
                                new CriticalRateParam()
                                {
                                    _Level = i,
                                    _CriticalRate = 0
                                },
                                new CriticalRateParam()
                                {
                                    _Level = i,
                                    _CriticalRate = 1
                                }
                            ];
                        }
                        detail._CriticalRate_Normal[i] = value[i].Value;
                        detail._CriticalRate_Fit[i] = value[i].Value;
                    }
                }
            }
            public ImmutableArray<int> Cost
            {
                get => Levels.Select(x => x.Cost).ToImmutableArray();
                set => Levels = Levels.Zip(value).Select(x => x.First with { Cost = x.Second }).ToImmutableArray();
            }
            IReadOnlyList<IWeaponUpgradeLevel> IWeaponUpgrade.Levels => Levels;
        }

        internal record CriticalRateUpgradeLevel(int Cost, string Info, float Value) : IWeaponUpgradeLevel
        {
        }

        internal class PenetrationUpgrade(CustomThroughNum main, ThroughNum detail) : IWeaponUpgrade
        {
            public PenetrationUpgrade() : this(new CustomThroughNum(), new ThroughNum()) { }
            public WeaponUpgradeKind Kind => WeaponUpgradeKind.Penetration;
            public object Main => new Individual()
            {
                _IndividualCustomCategory = Categories.Penetration,
                _CustomThroughNum = main
            };
            public object Detail => new IndividualCustom()
            {
                _IndividualCustomCategory = Categories.Penetration,
                _ThroughNums = detail
            };
            public Guid MessageId
            {
                get => main._MessageId;
                set => main._MessageId = value;
            }
            public ImmutableArray<PenetrationUpgradeLevel> Levels
            {
                get
                {
                    var result = ImmutableArray.CreateBuilder<PenetrationUpgradeLevel>();
                    for (var i = 0; i < main._ThroughNumCustomStages.Count; i++)
                    {
                        var cost = main._ThroughNumCustomStages[i]._Cost;
                        var info = main._ThroughNumCustomStages[i]._Info;
                        var value = detail._ThroughNum_Normal.Count > i
                            ? detail._ThroughNum_Normal[i]
                            : 0;
                        result.Add(new PenetrationUpgradeLevel(cost, info, value));
                    }
                    return result.ToImmutable();
                }
                set
                {
                    main._ThroughNumCustomStages.Resize(value.Length);
                    detail._ThroughNum_Normal.Resize(value.Length);
                    detail._ThroughNum_Fit.Resize(value.Length);
                    for (var i = 0; i < main._ThroughNumCustomStages.Count; i++)
                    {
                        var l = main._ThroughNumCustomStages[i] ??= new ThroughNumCustomStage();
                        l._Cost = value[i].Cost;
                        l._Info = value[i].Info;
                        if (i != 0)
                        {
                            l._ThroughNumParams =
                            [
                                new ThroughNumParam()
                                {
                                    _Level = i,
                                    _ThroughNum = 0
                                },
                                new ThroughNumParam()
                                {
                                    _Level = i,
                                    _ThroughNum = 1
                                }
                            ];
                        }
                        detail._ThroughNum_Normal[i] = value[i].Value;
                        detail._ThroughNum_Fit[i] = value[i].Value;
                    }
                }
            }
            public ImmutableArray<int> Cost
            {
                get => Levels.Select(x => x.Cost).ToImmutableArray();
                set => Levels = Levels.Zip(value).Select(x => x.First with { Cost = x.Second }).ToImmutableArray();
            }
            IReadOnlyList<IWeaponUpgradeLevel> IWeaponUpgrade.Levels => Levels;
        }

        internal record PenetrationUpgradeLevel(int Cost, string Info, int Value) : IWeaponUpgradeLevel
        {
        }

        internal class ReloadSpeedUpgrade(CustomReloadSpeed main, ReloadSpeed detail) : IWeaponUpgrade
        {
            public const int ReloadNum = 0;
            public const int ReloadSpeedRate = 1;

            public ReloadSpeedUpgrade() : this(new CustomReloadSpeed(), new ReloadSpeed()) { }
            public WeaponUpgradeKind Kind => WeaponUpgradeKind.ReloadSpeed;
            public int SubType => main._ReloadSpeedCustomStages.GetItem(1)?._ReloadSpeedParams.GetItem(0)?._ReloadSpeed != 0
                ? ReloadSpeedRate
                : ReloadNum;
            public object Main => new Individual()
            {
                _IndividualCustomCategory = Categories.ReloadSpeed,
                _CustomReloadSpeed = main
            };
            public object Detail => new IndividualCustom()
            {
                _IndividualCustomCategory = Categories.ReloadSpeed,
                _ReloadSpeed = detail
            };
            public Guid MessageId
            {
                get => main._MessageId;
                set => main._MessageId = value;
            }
            public ImmutableArray<ReloadSpeedUpgradeLevel> Levels
            {
                get
                {
                    var result = ImmutableArray.CreateBuilder<ReloadSpeedUpgradeLevel>();
                    for (var i = 0; i < main._ReloadSpeedCustomStages.Count; i++)
                    {
                        var cost = main._ReloadSpeedCustomStages[i]._Cost;
                        var info = main._ReloadSpeedCustomStages[i]._Info;
                        result.Add(new ReloadSpeedUpgradeLevel(
                            cost,
                            info,
                            detail._ReloadNums.GetItem(i),
                            detail._ReloadSpeedRates.GetItem(i)));
                    }
                    return result.ToImmutable();
                }
                set
                {
                    var type = value.Any(x => x.Speed != 0) ? ReloadSpeedRate : ReloadNum;

                    main._ReloadSpeedCustomStages.Resize(value.Length);
                    if (type == ReloadNum)
                    {
                        detail._ReloadNums.Resize(value.Length);
                        detail._ReloadSpeedRates.Resize(0);
                    }
                    else
                    {
                        detail._ReloadNums.Resize(0);
                        detail._ReloadSpeedRates.Resize(value.Length);
                    }
                    for (var i = 0; i < main._ReloadSpeedCustomStages.Count; i++)
                    {
                        var l = main._ReloadSpeedCustomStages[i] ??= new ReloadSpeedCustomStage();
                        l._Cost = value[i].Cost;
                        l._Info = value[i].Info;
                        if (i != 0)
                        {
                            l._ReloadSpeedParams =
                            [
                                new ReloadSpeedParam()
                                {
                                    _Level = i,
                                    _ReloadSpeed = type
                                }
                            ];
                        }
                        if (type == ReloadNum)
                            detail._ReloadNums[i] = value[i].Num;
                        else
                            detail._ReloadSpeedRates[i] = value[i].Speed;
                    }
                }
            }
            public ImmutableArray<int> Cost
            {
                get => Levels.Select(x => x.Cost).ToImmutableArray();
                set => Levels = Levels.Zip(value).Select(x => x.First with { Cost = x.Second }).ToImmutableArray();
            }
            IReadOnlyList<IWeaponUpgradeLevel> IWeaponUpgrade.Levels => Levels;
        }

        internal record ReloadSpeedUpgradeLevel(int Cost, string Info, int Num, float Speed) : IWeaponUpgradeLevel
        {
        }

        internal class FireRateUpgrade(CustomRapid main, Rapid detail) : IWeaponUpgrade
        {
            private const int RapidSpeed = 0;
            private const int RapidPumpActionSpeed = 1;

            public FireRateUpgrade() : this(new CustomRapid(), new Rapid()) { }
            public WeaponUpgradeKind Kind => WeaponUpgradeKind.FireRate;
            public int SubType => main._RapidCustomStages.GetItem(1)?._RapidParams.GetItem(0)?._Rapid ?? 0;
            public object Main => new Individual()
            {
                _IndividualCustomCategory = Categories.FireRate,
                _CustomRapid = main
            };
            public object Detail => new IndividualCustom()
            {
                _IndividualCustomCategory = Categories.FireRate,
                _Rapid = detail
            };
            public Guid MessageId
            {
                get => main._MessageId;
                set => main._MessageId = value;
            }
            public ImmutableArray<FireRateUpgradeLevel> Levels
            {
                get
                {
                    var result = ImmutableArray.CreateBuilder<FireRateUpgradeLevel>();
                    for (var i = 0; i < main._RapidCustomStages.Count; i++)
                    {
                        var cost = main._RapidCustomStages[i]._Cost;
                        var info = main._RapidCustomStages[i]._Info;
                        result.Add(new FireRateUpgradeLevel(
                            cost,
                            info,
                            detail._RapidSpeed.GetItem(i),
                            detail._PumpActionRapidSpeed.GetItem(i)));
                    }
                    return result.ToImmutable();
                }
                set
                {
                    var type = value.Any(x => x.Speed != 0) ? RapidSpeed : RapidPumpActionSpeed;

                    main._RapidCustomStages.Resize(value.Length);
                    if (SubType == RapidSpeed)
                    {
                        detail._RapidSpeed.Resize(value.Length);
                        detail._PumpActionRapidSpeed.Resize(0);
                    }
                    else
                    {
                        detail._RapidSpeed.Resize(0);
                        detail._PumpActionRapidSpeed.Resize(value.Length);
                    }
                    for (var i = 0; i < main._RapidCustomStages.Count; i++)
                    {
                        var l = main._RapidCustomStages[i] ??= new RapidCustomStage();
                        l._Cost = value[i].Cost;
                        l._Info = value[i].Info;
                        if (i != 0)
                        {
                            l._RapidParams =
                            [
                                new RapidParam()
                                {
                                    _Level = i,
                                    _Rapid = SubType
                                }
                            ];
                        }
                        if (SubType == RapidSpeed)
                            detail._RapidSpeed[i] = value[i].Speed;
                        else
                            detail._PumpActionRapidSpeed[i] = value[i].PumpSpeed;
                    }
                }
            }
            public ImmutableArray<int> Cost
            {
                get => Levels.Select(x => x.Cost).ToImmutableArray();
                set => Levels = Levels.Zip(value).Select(x => x.First with { Cost = x.Second }).ToImmutableArray();
            }
            IReadOnlyList<IWeaponUpgradeLevel> IWeaponUpgrade.Levels => Levels;
        }

        internal record FireRateUpgradeLevel(int Cost, string Info, float Speed, float PumpSpeed) : IWeaponUpgradeLevel
        {
        }

        internal class WeaponExclusive(LimitBreak main, LimitBreakCustom detail) : IWeaponExclusive
        {
            private static readonly WeaponUpgradeKind[] _categoryToKind =
            [
                WeaponUpgradeKind.CriticalRate,
                WeaponUpgradeKind.Power,
                WeaponUpgradeKind.PowerShotGunAround,
                WeaponUpgradeKind.Penetration,
                WeaponUpgradeKind.AmmoCapacity,
                WeaponUpgradeKind.FireRate,
                WeaponUpgradeKind.Durability,
                WeaponUpgradeKind.UnlimitedAmmo,
                WeaponUpgradeKind.CombatSpeed,
                WeaponUpgradeKind.Indestructible,
                WeaponUpgradeKind.Invalid,
            ];

            public WeaponExclusive() : this(new LimitBreak(), new LimitBreakCustom())
            {
                ((LimitBreak)Main)._CustomLimitBreak._LimitBreakCustomStages = [
                    new LimitBreakCustomStage()
                    {
                        _Cost = 0,
                        _Info = ""
                    }
                ];
            }
            public WeaponUpgradeKind Kind
            {
                get => _categoryToKind[Category];
                set => Category = Array.IndexOf(_categoryToKind, value);
            }
            public int Category
            {
                get => main._LimitBreakCustomCategory;
                private set
                {
                    main._LimitBreakCustomCategory = value;
                    detail._LimitBreakCustomCategory = value;
                }
            }
            public object Main => main;
            public object Detail => detail;
            public Guid MessageId
            {
                get => main._CustomLimitBreak._MessageId;
                set => main._CustomLimitBreak._MessageId = value;
            }
            public Guid PerkMessageId
            {
                get => main._CustomLimitBreak._PerksMessageId;
                set => main._CustomLimitBreak._PerksMessageId = value;
            }
            public float RateValue
            {
                get => main._CustomLimitBreak._RateValue;
                set
                {
                    main._CustomLimitBreak._RateValue = value;
                    detail._LimitBreakCriticalRate._CriticalRateNormalScale = value;
                    detail._LimitBreakCriticalRate._CriticalRateFitScale = value;
                    detail._LimitBreakAttackUp._DamageRateScale = value;
                    detail._LimitBreakAttackUp._WinceRateScale = value;
                    detail._LimitBreakAttackUp._BreakRateScale = value;
                    detail._LimitBreakAttackUp._StoppingRateScale = value;
                    detail._LimitBreakThroughNum._ThroughNumNormal = (int)value;
                    detail._LimitBreakThroughNum._ThroughNumFit = (int)value;
                    detail._LimitBreakAmmoMaxUp._AmmoMaxScale = (int)value;
                    detail._LimitBreakRapid._RapidSpeedScale = value;
                    detail._LimitBreakStrength._DurabilityMaxScale = (int)value;
                    detail._LimitBreakOKReload._IsOKReload = true;
                    detail._LimitBreakCombatSpeed._CombatSpeed = value;
                    detail._LimitBreakUnbreakable._IsUnbreakable = true;
                }
            }
            public int Cost
            {
                get => main._CustomLimitBreak._LimitBreakCustomStages.FirstOrDefault()?._Cost ?? 0;
                set
                {
                    var first = main._CustomLimitBreak._LimitBreakCustomStages.FirstOrDefault();
                    if (first != null)
                        first._Cost = value;
                }
            }

            public WeaponExclusive WithPower(float pDamage, float pWince, float pBreak, float pStopping)
            {
                detail._LimitBreakAttackUp._DamageRateScale = pDamage;
                detail._LimitBreakAttackUp._WinceRateScale = pWince;
                detail._LimitBreakAttackUp._BreakRateScale = pBreak;
                detail._LimitBreakAttackUp._StoppingRateScale = pStopping;
                return this;
            }
        }
    }
}
