using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using chainsaw;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using RszTool;
using static chainsaw.WeaponCustomUserdata;
using static chainsaw.WeaponDetailCustomUserdata;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal partial class WeaponModifier
    {
        internal enum WeaponUpgradeKind
        {
            Power,
            AmmoCapacity,
            Penetration,
            ReloadSpeed,
            RateOfFire,
            CriticalRate,
            Durability,
        }

        internal interface IWeaponModifier
        {
            WeaponUpgradeKind Kind { get; }
            object Main { get; }
            object Detail { get; }
            Guid MessageId { get; }
        }

        internal interface IWeaponUpgrade : IWeaponModifier
        {
            IReadOnlyList<IWeaponUpgradeLevel> Levels { get; }
        }

        internal interface IWeaponExclusive : IWeaponModifier
        {
            public Guid PerkMessageId { get; }
            public float RateValue { get; }
            public int Cost { get; }
        }

        internal interface IWeaponUpgradeLevel
        {
            int Cost { get; }
            string Info { get; }
            float Value { get; }
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

            public int Id { get; }
            public ImmutableArray<IWeaponModifier> Modifiers { get; set; }

            public WeaponStats(WeaponStage main, WeaponDetailStage detail)
            {
                Id = main._WeaponID;
                _radar = main._RaderChartGuiSingleSettingData;

                var modifiers = ImmutableArray.CreateBuilder<IWeaponModifier>();
                foreach (var c in main._WeaponCustom._Commons)
                {
                    var d = detail._WeaponDetailCustom._CommonCustoms.FirstOrDefault(x => x._CommonCustomCategory == c._CommonCustomCategory);
                    if (d == null)
                        continue;

                    modifiers.Add(c._CommonCustomCategory switch
                    {
                        0 => new PowerUpgrade(c._CustomAttackUp, d._AttackUp),
                        2 => new AmmoCapacityUpgrade(c._CustomAmmoMaxUp, d._AmmoMaxUp),
                        _ => throw new NotSupportedException()
                    });
                }
                foreach (var i in main._WeaponCustom._Individuals)
                {
                    var d = detail._WeaponDetailCustom._IndividualCustoms.FirstOrDefault(x => x._IndividualCustomCategory == i._IndividualCustomCategory);
                    if (d == null)
                        continue;

                    if (i._IndividualCustomCategory == 1)
                    {
                        modifiers.Add(new PenetrationUpgrade(i._CustomThroughNum, d._ThroughNums));
                    }
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
            public WeaponUpgradeKind Kind => WeaponUpgradeKind.Power;
            public object Main => new Common()
            {
                _CommonCustomCategory = 0,
                _CustomAttackUp = main
            };
            public object Detail => new CommonCustom()
            {
                _CommonCustomCategory = 0,
                _AttackUp = detail
            };
            public Guid MessageId => main._MessageId;
            public ImmutableArray<PowerUpgradeLevel> Levels
            {
                get
                {
                    var result = ImmutableArray.CreateBuilder<PowerUpgradeLevel>();
                    for (var i = 0; i < main._AttackUpCustomStages.Count; i++)
                    {
                        var cost = main._AttackUpCustomStages[i]._Cost;
                        var info = main._AttackUpCustomStages[i]._Info;
                        var value = detail._DamageRates.Count > i
                            ? detail._DamageRates[i]._BaseValue
                            : 0;
                        result.Add(new PowerUpgradeLevel(cost, info, value));
                    }
                    return result.ToImmutable();
                }
                set
                {
                    if (value.Length > main._AttackUpCustomStages.Count)
                        throw new NotSupportedException();
                    main._AttackUpCustomStages.Resize(value.Length);
                    for (var i = 0; i < main._AttackUpCustomStages.Count; i++)
                    {
                        var l = main._AttackUpCustomStages[i];
                        l._Cost = value[i].Cost;
                        l._Info = value[i].Info;
                        detail._DamageRates[i]._BaseValue = value[i].Value;
                    }
                }
            }
            IReadOnlyList<IWeaponUpgradeLevel> IWeaponUpgrade.Levels => Levels;
        }

        internal record PowerUpgradeLevel(int Cost, string Info, float Value) : IWeaponUpgradeLevel
        {
        }

        internal class AmmoCapacityUpgrade(CustomAmmoMaxUp main, AmmoMaxUp detail) : IWeaponUpgrade
        {
            public WeaponUpgradeKind Kind => WeaponUpgradeKind.AmmoCapacity;
            public object Main => new Common()
            {
                _CommonCustomCategory = 2,
                _CustomAmmoMaxUp = main
            };
            public object Detail => new CommonCustom()
            {
                _CommonCustomCategory = 2,
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
                    for (var i = 0; i < main._AmmoMaxUpCustomStages.Count; i++)
                    {
                        var l = main._AmmoMaxUpCustomStages[i];
                        l._Cost = value[i].Cost;
                        l._Info = value[i].Info;
                        detail._AmmoMaxs[i] = value[i].Value;
                    }
                }
            }
            IReadOnlyList<IWeaponUpgradeLevel> IWeaponUpgrade.Levels => Levels;
        }

        internal record AmmoUpgradeLevel(int Cost, string Info, int Value) : IWeaponUpgradeLevel
        {
            float IWeaponUpgradeLevel.Value => Value;
        }

        internal class PenetrationUpgrade(CustomThroughNum main, ThroughNum detail) : IWeaponUpgrade
        {
            public WeaponUpgradeKind Kind => WeaponUpgradeKind.Penetration;
            public object Main => main;
            public object Detail => detail;
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
                    // detail._ThroughNum_Fit.Resize(value.Length);
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
                                    }
                            ];
                        }
                        detail._ThroughNum_Normal[i] = value[i].Value;
                        // detail._ThroughNum_Fit[i] = value[i].Value;
                    }
                }
            }
            IReadOnlyList<IWeaponUpgradeLevel> IWeaponUpgrade.Levels => Levels;
        }

        internal record PenetrationUpgradeLevel(int Cost, string Info, int Value) : IWeaponUpgradeLevel
        {
            float IWeaponUpgradeLevel.Value => Value;
        }

        internal class WeaponExclusive(LimitBreak main, LimitBreakCustom detail) : IWeaponExclusive
        {
            public WeaponUpgradeKind Kind => WeaponUpgradeKind.Power;
            public int Category => main._LimitBreakCustomCategory;
            public object Main => main;
            public object Detail => detail;
            public Guid MessageId => main._CustomLimitBreak._MessageId;
            public Guid PerkMessageId => main._CustomLimitBreak._PerksMessageId;
            public float RateValue => main._CustomLimitBreak._RateValue;
            public int Cost => main._CustomLimitBreak._LimitBreakCustomStages.FirstOrDefault()?._Cost ?? 0;
        }
    }
}
