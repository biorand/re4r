#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

using System.Numerics;
using IntelOrca.Biohazard.REE.Rsz;
using IntelOrca.Biohazard.REE.Rsz.Native;

namespace chainsaw
{
    internal class CameraRecoilParam
    {
        public Range _YawRangeDeg { get; set; }
        public Range _PitchRangeDeg { get; set; }
        public via.AnimationCurve _Curve { get; set; }
        public System.Single _CurveTime { get; set; }
        public System.Single _InvalidCancelTime { get; set; }
    }
    internal class CameraShakeParam
    {
        public System.Int32 _Type { get; set; }
        public LifeParam _Life { get; set; }
        public MoveParam _Move { get; set; }
        internal enum CalculationType
        {
        }
        internal class MoveParam
        {
            public Range _Period { get; set; }
            public Range _TranslationXRange { get; set; }
            public Range _TranslationYRange { get; set; }
            public Range _TranslationZRange { get; set; }
            public Range _RotationXRange { get; set; }
            public Range _RotationYRange { get; set; }
            public Range _RotationZRange { get; set; }
            public System.Boolean _UseDistanceAttenuation { get; set; }
            public System.Single _DistanceAttenuationStart { get; set; }
            public System.Single _DistanceAttenuationEnd { get; set; }
            public System.Boolean _UseAngleAttenuation { get; set; }
            public System.Single _AngleAttenuationConeOffset { get; set; }
            public System.Single _AngleAttenuationSpread { get; set; }
        }
        internal class LifeParam
        {
            public System.Boolean _IsLoop { get; set; }
            public System.Single _LifeTime { get; set; }
            public System.Boolean _UseLifeAttenuation { get; set; }
            public System.Boolean _HasCurveData { get; set; }
            public via.AnimationCurve _LifeCurve { get; set; }
        }
    }
    internal class CharacterBuriedArmCorrectorUnit
    {
        internal class Parameter
        {
            public chainsaw.DampingParam CorrectDampingParam { get; set; }
            public chainsaw.ExtraJoint.Parameter TargetPositionParameter { get; set; }
            public chainsaw.CharacterBuriedArmCorrectorUnit.Sensor.Parameter CorrectSensorParameter { get; set; }
            public chainsaw.ExtraJoint.Parameter CorrectPositionParameter { get; set; }
            public System.Numerics.Quaternion CorrectRotation { get; set; }
            public via.AnimationCurve CorrectRotationCurve { get; set; }
            public Range CorrectHitRateNormalizeRange { get; set; }
        }
        internal class ArmMotionCorrector
        {
        }
        internal class Sensor
        {
            internal class Parameter
            {
                public System.Single _Radius { get; set; }
                public chainsaw.ExtraJoint.Parameter _StartPosition { get; set; }
                public chainsaw.ExtraJoint.Parameter _EndPosition { get; set; }
            }
        }
        internal class ArmCorrector
        {
        }
    }
    internal class DampingParam
    {
        public System.Single _DampingRate { get; set; }
        public System.Single _DampingTime { get; set; }
    }
    internal class ExtraJoint
    {
        internal class Parameter
        {
            public System.Numerics.Vector3 LocalPosition { get; set; }
            public System.Numerics.Quaternion LocalRotation { get; set; }
            public System.Numerics.Vector3 LocalScale { get; set; }
            public System.String ParentJointName { get; set; } = "";
            public System.UInt32 ParentJointNameHash { get; set; }
        }
    }
    internal class ScopeParam
    {
        public System.Single _FOVMin { get; set; }
        public System.Single _FOVMax { get; set; }
        public System.Numerics.Vector3 _CameraOffSet { get; set; }
        public System.Single SpeedAtFovMin { get; set; }
        public System.Single SpeedAtFovMax { get; set; }
        public System.Single PCSpeedScale { get; set; }
        public System.String CameraJoint { get; set; } = "";
        public System.Collections.Generic.List<System.Single> _Rates { get; set; } = [];
    }
    internal class ShellBaseAttackInfo
    {
        public System.UInt32 _VibrationTriggerID { get; set; }
        public System.Boolean _DecayByDistCamToVibrationOwner { get; set; }
        public System.Single _DecayDistLimitNear { get; set; }
        public System.Single _DecayDistLimitFar { get; set; }
        public CurveVariable _DamageRate { get; set; }
        public CurveVariable _WinceRate { get; set; }
        public CurveVariable _BreakRate { get; set; }
        public CurveVariable _StoppingRate { get; set; }
        internal class CurveVariable
        {
            public System.Single _BaseValue { get; set; }
            public via.AnimationCurve _RateCurve { get; set; } = new();
        }
    }
    internal class WeaponDetailCustomUserdata
    {
        public System.Collections.Generic.List<WeaponDetailStage> _WeaponDetailStages { get; set; } = [];
        internal class WeaponDetailStage
        {
            public System.Int32 _WeaponID { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.WeaponDetailCustom _WeaponDetailCustom { get; set; }
        }
        internal class AmmoCost
        {
            public System.Collections.Generic.List<System.Int32> _AmmoCostNum { get; set; } = [];
        }
        internal class LimitBreakAmmoMaxUp
        {
            public System.Single _AmmoMaxScale { get; set; }
            public System.Single _ReloadNumScale { get; set; }
        }
        internal class LimitBreakCustom
        {
            public System.Int32 _LimitBreakCustomCategory { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.LimitBreakCriticalRate _LimitBreakCriticalRate { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.LimitBreakAttackUp _LimitBreakAttackUp { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.LimitBreakAttackUp _LimitBreakShotGunAroundAttackUp { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.LimitBreakThroughNum _LimitBreakThroughNum { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.LimitBreakAmmoMaxUp _LimitBreakAmmoMaxUp { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.LimitBreakRapid _LimitBreakRapid { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.LimitBreakStrength _LimitBreakStrength { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.LimitBreakOKReload _LimitBreakOKReload { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.LimitBreakCombatSpeed _LimitBreakCombatSpeed { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.LimitBreakUnbreakable _LimitBreakUnbreakable { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.LimitBreakBlastRange_1011 _LimitBreakBlastRange_1011 { get; set; } = new();
        }
        internal class IndividualCustom
        {
            public System.Int32 _IndividualCustomCategory { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.CriticalRate _CriticalRate { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.ThroughNum _ThroughNums { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.ReloadSpeed _ReloadSpeed { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.Strength _Strength { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.Rapid _Rapid { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.AmmoCost _AmmoCost { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.FlameDistance _FlameDistance { get; set; } = new();
            public System.Collections.Generic.List<System.String> _Others { get; set; } = [];
            public System.Int32 _ItemID { get; set; }
            public System.Collections.Generic.List<System.Int32> _UsableAmmoList { get; set; } = [];
        }
        internal class LimitBreakRapid
        {
            public System.Single _RapidSpeedScale { get; set; }
        }
        internal class FlameDistance
        {
            public System.Collections.Generic.List<System.Single> _ShellDistance { get; set; } = [];
        }
        internal class CommonCustom
        {
            public System.Int32 _CommonCustomCategory { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.AttackUp _AttackUp { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.Stabilization _Stabilization { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.AmmoMaxUp _AmmoMaxUp { get; set; } = new();
            public chainsaw.WeaponDetailCustomUserdata.AttackUp _ShotGunAroundAttackUp { get; set; } = new();
        }
        internal class LimitBreakUnbreakable
        {
            public System.Boolean _IsUnbreakable { get; set; }
        }
        internal class AttackUp
        {
            public System.Collections.Generic.List<chainsaw.ShellBaseAttackInfo.CurveVariable> _DamageRates { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.ShellBaseAttackInfo.CurveVariable> _WinceRates { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.ShellBaseAttackInfo.CurveVariable> _BreakRates { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.ShellBaseAttackInfo.CurveVariable> _StoppingRates { get; set; } = [];
            public System.Collections.Generic.List<System.Single> _ExplosionRadiusScale { get; set; } = [];
            public System.Collections.Generic.List<System.Single> _ExplosionSensorRadiusScale { get; set; } = [];
        }
        internal class LimitBreakBlastRange_1011
        {
            public System.Single _BlastRangeScale { get; set; }
        }
        internal class ReloadSpeed
        {
            public System.Collections.Generic.List<System.Int32> _ReloadNums { get; set; } = [];
            public System.Collections.Generic.List<System.Single> _ReloadSpeedRates { get; set; } = [];
        }
        internal class AmmoMaxUp
        {
            public System.Collections.Generic.List<System.Int32> _AmmoMaxs { get; set; } = [];
            public System.Collections.Generic.List<System.Int32> _ReloadNum { get; set; } = [];
        }
        internal class AttachmentParam
        {
            public System.Int32 _AttachmentParamName { get; set; }
            public System.Single _RandomRadius_Normal { get; set; }
            public System.Single _RandomRadius_Fit { get; set; }
            public chainsaw.WeaponReticleFitParam _ReticleFitParam { get; set; }
            public chainsaw.CameraRecoilParam _CameraRecoilParam { get; set; }
            public chainsaw.CameraShakeParam _CameraShakeParam { get; set; }
            public chainsaw.WeaponHandShakeParam _WeaponHandShakeParam { get; set; }
            public System.Collections.Generic.List<chainsaw.CameraRecoilParam> _CustomLevelCameraRecoilParam { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.WeaponHandShakeParam> _CustomLevelWeaponHandShakeParam { get; set; } = [];
            public System.Collections.Generic.List<System.Int32> _MeshPartsNums { get; set; } = [];
            public System.Collections.Generic.List<System.Int32> _HideMeshPartsNums { get; set; } = [];
            public chainsaw.ScopeParam _ScopeParam { get; set; }
            public System.UInt32 _ReticleGuiType { get; set; }
            public chainsaw.WeaponEquipParam _EquipParam { get; set; }
            public System.Int32 _GenerateFollowTarget { get; set; }
            public chainsaw.CharacterBuriedArmCorrectorUnit.Parameter _BuriedArmParam { get; set; }
        }
        internal class ThroughNum
        {
            public System.Collections.Generic.List<System.Int32> _ThroughNum_Normal { get; set; } = [];
            public System.Collections.Generic.List<System.Int32> _ThroughNum_Fit { get; set; } = [];
        }
        internal class LimitBreakThroughNum
        {
            public System.Int32 _ThroughNumNormal { get; set; }
            public System.Int32 _ThroughNumFit { get; set; }
        }
        internal class Stabilization
        {
            public System.Collections.Generic.List<System.Single> _RandomRadiuses { get; set; } = [];
            public System.Collections.Generic.List<System.Single> _RandomRadius_Fits { get; set; } = [];
            public System.Collections.Generic.List<System.Int32> _TerrainHitSoundTypes { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.WeaponReticleFitParam> _ReticleFitParams { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.CameraRecoilParam> _CameraRecoilParams { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.CameraShakeParam> _CameraShakeParams { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.WeaponHandShakeParam> _WeaponHandShakeParams { get; set; } = [];
            public System.Collections.Generic.List<System.UInt32> _ReticleGuiTypes { get; set; } = [];
        }
        internal class LimitBreakOKReload
        {
            public System.Boolean _IsOKReload { get; set; }
        }
        internal class LimitBreakStrength
        {
            public System.Single _DurabilityMaxScale { get; set; }
        }
        internal class Strength
        {
            public System.Collections.Generic.List<System.Int32> _DurabilityMaxes { get; set; } = [];
        }
        internal class LimitBreakCriticalRate
        {
            public System.Single _CriticalRateNormalScale { get; set; }
            public System.Single _CriticalRateFitScale { get; set; }
        }
        internal class LimitBreakAttackUp
        {
            public System.Single _DamageRateScale { get; set; }
            public System.Single _WinceRateScale { get; set; }
            public System.Single _BreakRateScale { get; set; }
            public System.Single _StoppingRateScale { get; set; }
        }
        internal class Rapid
        {
            public System.Collections.Generic.List<System.Single> _RapidSpeed { get; set; } = [];
            public System.Collections.Generic.List<System.Single> _PumpActionRapidSpeed { get; set; } = [];
        }
        internal class CriticalRate
        {
            public System.Collections.Generic.List<System.Single> _CriticalRate_Normal { get; set; } = [];
            public System.Collections.Generic.List<System.Single> _CriticalRate_Fit { get; set; } = [];
        }
        internal class WeaponDetailCustom
        {
            public System.Collections.Generic.List<chainsaw.WeaponDetailCustomUserdata.CommonCustom> _CommonCustoms { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.WeaponDetailCustomUserdata.IndividualCustom> _IndividualCustoms { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.WeaponDetailCustomUserdata.AttachmentCustom> _AttachmentCustoms { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.WeaponDetailCustomUserdata.LimitBreakCustom> _LimitBreakCustoms { get; set; } = [];
        }
        internal class AttachmentCustom
        {
            public System.Int32 _ItemID { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponDetailCustomUserdata.AttachmentParam> _AttachmentParams { get; set; } = [];
        }
        internal class LimitBreakCombatSpeed
        {
            public System.Single _CombatSpeed { get; set; }
        }
    }
    internal class WeaponEquipParam
    {
        public System.String ParentJointName { get; set; } = "";
        public System.Numerics.Vector3 LocalPosition { get; set; }
        public System.Numerics.Quaternion LocalRotation { get; set; }
        public System.Numerics.Vector3 LocalScale { get; set; }
    }
    internal class WeaponHandShakeParam
    {
        public System.Single Time { get; set; }
        public via.AnimationCurve Curve { get; set; }
        public System.Single RStickOffset { get; set; }
    }
    internal class WeaponReticleFitParam
    {
        public Range _PointRange { get; set; }
        public System.Single _HoldAddPoint { get; set; }
        public System.Single _MoveSubPoint { get; set; }
        public System.Single _CameraSubPoint { get; set; }
        public System.Single _KeepFitLimitPoint { get; set; }
        public System.Single _ShootSubPoint { get; set; }
    }
    internal class RaderChartGuiSingleSettingData
    {
        public System.Int32 _ItemId { get; set; }
        public System.Int32 _ColorPresetType { get; set; }
        public System.Collections.Generic.List<Setting> _Settings { get; set; } = [];
        internal class Setting
        {
            public System.Int32 _Category { get; set; }
            public Range _Range { get; set; }
            public System.Single _Rate { get; set; }
            public System.Collections.Generic.List<chainsaw.StabilityEvaluationSetting> _StabilityEvaluationSettings { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.SpCategoryEvaluationSettingBase> _SpCategoryEvaluationSettings { get; set; } = [];
        }
    }
    internal class SpCategoryEvaluationSettingBase
    {
        public System.Single Value { get; set; }
    }
    internal class SpCategory00EvaluationSetting : SpCategoryEvaluationSettingBase
    {
        public System.Int32 PartsItemId { get; set; }
    }
    internal class SpCategory01EvaluationSetting : SpCategoryEvaluationSettingBase
    {
        public System.Int32 PartsItemId { get; set; }
    }
    internal class SpCategory02EvaluationSetting : SpCategoryEvaluationSettingBase
    {
        public System.Int32 PartsItemId { get; set; }
    }
    internal class SpCategory03EvaluationSetting : SpCategoryEvaluationSettingBase
    {
    }
    internal class StabilityEvaluationSetting
    {
        public System.Int32 PartsItemId { get; set; }
        public System.Single Value { get; set; }
    }
    internal class WeaponCustomUserdata
    {
        public System.Collections.Generic.List<WeaponStage> _WeaponStages { get; set; } = [];
        public System.Collections.Generic.List<ItemStage> _ItemStages { get; set; } = [];
        public System.Collections.Generic.List<chainsaw.RaderChartGuiSingleSettingData> _RaderChartGuiSingleSettingDatas { get; set; } = [];
        internal class ReloadSpeedCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.ReloadSpeedParam> _ReloadSpeedParams { get; set; } = [];
        }
        internal class CustomFlameDistance
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.FlameDistanceCustomStage> _FlameDistanceCustomStages { get; set; } = [];
        }
        internal class UsableAmmoCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
            public chainsaw.WeaponCustomUserdata.ChangeLevel _ChangeLevel { get; set; }
        }
        internal class WeaponCustom
        {
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.Common> _Commons { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.Individual> _Individuals { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.LimitBreak> _LimitBreak { get; set; } = [];
        }
        internal class StabilizationParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _Stabilization { get; set; }
        }
        internal class StabilizationCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.StabilizationParam> _StabilizationParams { get; set; } = [];
        }
        internal class RapidCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.RapidParam> _RapidParams { get; set; } = [];
        }
        internal class CustomAmmoMaxUp
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AmmoMaxUpCustomStage> _AmmoMaxUpCustomStages { get; set; } = [];
        }
        internal class StrengthParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _Strength { get; set; }
        }
        internal class CustomPolish
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.PolishCustomStage> _PolishCustomStages { get; set; } = [];
        }
        internal class Individual
        {
            public System.Int32 _IndividualCustomCategory { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomCriticalRate _CustomCriticalRate { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomThroughNum _CustomThroughNum { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomUsableAmmo _CustomUsableAmmo { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomReloadSpeed _CustomReloadSpeed { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomStage _CustomReload { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomRepair _CustomRepair { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomPolish _CustomPolish { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomStrength _CustomStrength { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomRapid _CustomRapid { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomOtherIndividual _CustomOtherIndividual { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomAmmoCost _CustomAmmoCost { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomFlameDistance _CustomFlameDistance { get; set; } = new();
        }
        internal class CustomThroughNum
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.ThroughNumCustomStage> _ThroughNumCustomStages { get; set; } = [];
        }
        internal class AttackUpParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _AttackUp { get; set; }
        }
        internal class AmmoCostParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _AmmoCost { get; set; }
        }
        internal class CriticalRateCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.CriticalRateParam> _CriticalRateParams { get; set; } = [];
        }
        internal class RepairParam
        {
            public System.Int32 _Level { get; set; }
        }
        internal class CustomOtherIndividual
        {
            public System.Guid _MessageId { get; set; }
            public System.String _str { get; set; } = "";
        }
        internal class CustomAmmoCost
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AmmoCostCustomStage> _AmmoCostCustomStages { get; set; } = [];
        }
        internal class CustomAttackUp
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AttackUpCustomStage> _AttackUpCustomStages { get; set; } = [];
        }
        internal class CustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
        }
        internal class CustomLimitBreak
        {
            public System.Guid _MessageId { get; set; }
            public System.Guid _PerksMessageId { get; set; }
            public System.Single _RateValue { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.LimitBreakCustomStage> _LimitBreakCustomStages { get; set; } = [];
            public System.Collections.Generic.List<System.Int32> _AutoCustomCategories { get; set; } = [];
        }
        internal class FlameDistanceCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.FlameDistanceParam> _FlameDistanceParams { get; set; } = [];
        }
        internal class CriticalRateParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _CriticalRate { get; set; }
        }
        internal class FlameDistanceParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _FlameDistance { get; set; }
        }
        internal class AttackUpCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AttackUpParam> _AttackUpParams { get; set; } = [];
        }
        internal class AmmoMaxUpCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AmmoMaxUpParam> _AmmoMaxUpParams { get; set; } = [];
        }
        internal class ItemCustom
        {
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.Common> _Commons { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.Individual> _Individuals { get; set; } = [];
        }
        internal class CustomUsableAmmo
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.UsableAmmoCustomStage> _UsableAmmoCustomStages { get; set; } = [];
        }
        internal class ReloadSpeedParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _ReloadSpeed { get; set; }
        }
        internal class CustomStrength
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.StrengthCustomStage> _StrengthCustomStages { get; set; } = [];
        }
        internal class AmmoMaxUpParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _AmmoMaxUp { get; set; }
        }
        internal class ChangeLevel
        {
            public System.Int32 _Level { get; set; }
        }
        internal class LimitBreakParam
        {
            public System.Int32 _Level { get; set; }
        }
        internal class RepairCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
        }
        internal class CustomCriticalRate
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.CriticalRateCustomStage> _CriticalRateCustomStages { get; set; } = [];
        }
        internal class CustomElementBase
        {
            public System.Guid _MessageId { get; set; }
        }
        internal class Common
        {
            public System.Int32 _CommonCustomCategory { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomAttackUp _CustomAttackUp { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomStabilization _CustomStabilization { get; set; } = new();
            public chainsaw.WeaponCustomUserdata.CustomAmmoMaxUp _CustomAmmoMaxUp { get; set; } = new();
        }
        internal class CustomReloadSpeed
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.ReloadSpeedCustomStage> _ReloadSpeedCustomStages { get; set; } = [];
            public LoopReloadFrame _LoopReloadFrameInfo { get; set; } = new LoopReloadFrame();
            internal class LoopReloadFrame
            {
                public System.Single _StartFrame { get; set; }
                public System.Single _LoopFrame { get; set; }
                public System.Single _EndFrame { get; set; }
            }
        }
        internal class ThroughNumCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.ThroughNumParam> _ThroughNumParams { get; set; } = [];
        }
        internal class CustomRapid
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.RapidCustomStage> _RapidCustomStages { get; set; } = [];
        }
        internal class LimitBreakCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
        }
        internal class LimitBreak
        {
            public System.Int32 _LimitBreakCustomCategory { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomLimitBreak _CustomLimitBreak { get; set; } = new();
        }
        internal class AmmoCostCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AmmoCostParam> _AmmoCostParams { get; set; } = [];
        }
        internal class RapidParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _Rapid { get; set; }
        }
        internal class ThroughNumParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _ThroughNum { get; set; }
        }
        internal class PolishParam
        {
            public System.Int32 _Level { get; set; }
        }
        internal class StrengthCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.StrengthParam> _StrengthParams { get; set; } = [];
        }
        internal class ItemStage
        {
            public System.Int32 _ItemID { get; set; }
            public chainsaw.WeaponCustomUserdata.ItemCustom _ItemCustom { get; set; }
        }
        internal class CustomRepair
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.RepairCustomStage> _RepairCustomStages { get; set; } = [];
        }
        internal class CustomStabilization
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.StabilizationCustomStage> _StabilizationCustomStages { get; set; } = [];
        }
        internal class PolishCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; } = "";
        }
        internal class WeaponStage
        {
            public System.Int32 _WeaponID { get; set; }
            public chainsaw.WeaponCustomUserdata.WeaponCustom _WeaponCustom { get; set; }
            public chainsaw.RaderChartGuiSingleSettingData _RaderChartGuiSingleSettingData { get; set; }
        }
    }
    internal class WeaponCustomUnlockSettingUserdata
    {
        public System.Collections.Generic.List<chainsaw.WeaponCustomUnlocksingleSetting> _Settings { get; set; } = [];
    }
    internal class WeaponCustomUnlocksingleSetting
    {
        public System.Int32 _ItemId { get; set; }
        public System.Collections.Generic.List<Data> _Datas { get; set; } = [];
        internal class UnlockData
        {
            public System.Int32 _CustomCategory { get; set; }
            public System.Int32 _UnlockLevel { get; set; }
        }
        internal class Data
        {
            public System.Int32 _FlagType { get; set; }
            public System.Boolean _IsApply { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUnlocksingleSetting.UnlockData> _UnlockDatas { get; set; } = [];
        }
    }
    internal class ItemCraftBonusSetting
    {
        public System.Collections.Generic.List<Data> _Datas { get; set; } = [];
        internal class Data
        {
            public System.Int32 _HasCount { get; set; }
            public System.Int32 _BonusCount { get; set; }
            public System.Single _Probability { get; set; }
        }
    }
    internal class ItemCraftGenerateNumUniqueSetting
    {
        public System.Int32 _ItemId { get; set; }
        public System.Int32 _GenerateNumMin { get; set; }
        public System.Int32 _Durability { get; set; }
        public System.Int32 _GenerateNum { get; set; }
    }
    internal class ItemCraftMaterial
    {
        public System.Int32 _ItemID { get; set; }
        public System.Int32 _RequiredNum { get; set; }
    }
    internal class ItemCraftRecipe
    {
        public System.Collections.Generic.List<chainsaw.ItemCraftResultSetting> _ResultSettings { get; set; } = [];
        public System.Collections.Generic.List<chainsaw.ItemCraftMaterial> _RequiredItems { get; set; } = [];
        public chainsaw.ItemCraftBonusSetting _BonusSetting { get; set; } = new();
        public System.Int32 _RecipeID { get; set; }
        public System.Int32 _Category { get; set; }
        public System.Single _CraftTime { get; set; }
        public System.Boolean _DrawWave { get; set; }
    }
    internal class ItemCraftResult
    {
        public System.Int32 _ItemID { get; set; }
        public System.Int32 _GeneratedNumMin { get; set; }
        public System.Int32 _GeneratedNumMax { get; set; }
        public chainsaw.ItemCraftGenerateNumUniqueSetting _GenerateNumUniqueSetting { get; set; } = new();
        public via.AnimationCurve _ProbabilityCurve { get; set; } = new();
        public System.Boolean _IsEnableProbabilityCurve { get; set; }
    }
    internal class ItemCraftResultSetting
    {
        public System.Int32 _Difficulty { get; set; }
        public chainsaw.ItemCraftResult _Result { get; set; } = new();
    }
    internal class ItemCraftSettingUserdata
    {
        public System.Collections.Generic.List<System.Int32> _MaterialItemIds { get; set; } = [];
        public System.Collections.Generic.List<System.Int32> _RecipeIdOrders { get; set; } = [];
        public System.Collections.Generic.List<chainsaw.ItemCraftRecipe> _Datas { get; set; } = [];
    }
    internal class InGameShopStockAdditionSettingUserdata
    {
        public System.Collections.Generic.List<chainsaw.InGameShopStockAdditionSingleSetting> _Settings { get; set; } = [];
    }
    internal class InGameShopStockAdditionSingleSetting
    {
        public System.Int32 _FlagType { get; set; }
        public System.Collections.Generic.List<Setting> _Settings { get; set; } = [];
        internal class Data
        {
            public System.Int32 _AddItemId { get; set; }
            public System.Int32 _AddCount { get; set; }
        }
        internal class Setting
        {
            public System.Int32 _Difficulty { get; set; }
            public System.Collections.Generic.List<chainsaw.InGameShopStockAdditionSingleSetting.Data> _Datas { get; set; } = [];
        }
    }
    internal class ItemMessageIdSettingUserdata
    {
        public System.Collections.Generic.List<Setting> _Settings { get; set; } = [];
        internal class Setting
        {
            public System.UInt32 _VariationHash { get; set; }
            public System.UInt32 _ExContentsGroupHash { get; set; }
            public System.Int32 _ItemId { get; set; }
            public System.Guid _NameMsgId { get; set; }
            public System.Guid _CaptionMsgId { get; set; }
        }
    }
    internal class CharmEffectSettingUserdata
    {
        public System.Collections.Generic.List<chainsaw.CharmEffectSingleSettingData> _Settings { get; set; } = [];
    }
    internal class CharmEffectSingleSettingData
    {
        public System.Int32 _ItemId { get; set; }
        public System.Collections.Generic.List<chainsaw.StatusEffectSetting> _Effects { get; set; } = [];
    }
    internal class StatusEffectSetting
    {
        public System.Int32 _StatusEffectID { get; set; }
        public System.Single _Value { get; set; }
    }
    internal class EnemyChapterParamUserData
    {
        public System.Collections.Generic.List<ChapterParamElement> _ChapterParamList { get; set; } = [];
        internal class ChapterParamElement
        {
            public System.Int32 _ChapterID { get; set; }
            public System.Collections.Generic.List<chainsaw.EnemyChapterParamUserData.RandomTableElement> _RandomTable { get; set; } = [];
        }
        internal class RandomTableElement
        {
            public System.Single Weight { get; set; }
            public System.Single Value { get; set; }
        }
    }
    internal class InGameShopRewardDisplaySetting
    {
        public System.Int32 _Mode { get; set; }
        public System.Int32 _StartTiming { get; set; }
        public System.Int32 _EndTiming { get; set; }
        public System.Guid _StartGlobalFlag { get; set; }
        public System.Guid _EndGlobalFlag { get; set; }
    }
    internal class InGameShopRewardSettingUserdata
    {
        public System.Collections.Generic.List<chainsaw.InGameShopRewardSingleSetting> _Settings { get; set; } = [];
    }
    internal class InGameShopRewardSingleSetting
    {
        public System.Boolean _Enable { get; set; }
        public System.Int32 _RewardId { get; set; }
        public System.Int32 _SpinelCount { get; set; }
        public System.Int32 _RewardItemId { get; set; }
        public System.Int32 _ItemCount { get; set; }
        public System.Int32 _Progress { get; set; }
        public System.Int32 _RecieveType { get; set; }
        public chainsaw.InGameShopRewardDisplaySetting _DisplaySetting { get; set; } = new();
    }
    internal class InGameShopItemCaptionSetting
    {
        public System.Guid _CaptionMsgId { get; set; }
    }
    internal class InGameShopItemSaleSetting
    {
        public System.Collections.Generic.List<chainsaw.InGameShopItemSaleSingleSetting> _Settings { get; set; } = [];
    }
    internal class InGameShopItemSaleSingleSetting
    {
        public System.Int32 _Mode { get; set; }
        public System.Int32 _SaleType { get; set; }
        public System.Int32 _StartTiming { get; set; }
        public System.Int32 _EndTiming { get; set; }
        public System.Guid _StartGlobalFlag { get; set; }
        public System.Guid _EndGlobalFlag { get; set; }
        public System.Int32 _SaleRate { get; set; }
    }
    internal class InGameShopItemSettingUserdata
    {
        public chainsaw.gui.shop.InGameShopAdjustParam _AdjustParam { get; set; } = new();
        public System.Boolean _IsRegistRepairSettings { get; set; }
        public System.Collections.Generic.List<chainsaw.gui.shop.InGameShopRepairSetting> _RepairSettings { get; set; } = [];
        public System.Collections.Generic.List<Data> _Datas { get; set; } = [];
        internal class Data
        {
            public System.Int32 _ItemId { get; set; }
            public System.Collections.Generic.List<chainsaw.gui.shop.ItemPriceSetting> _PriceSettings { get; set; } = [];
            public chainsaw.InGameShopItemUnlockSetting _UnlockSetting { get; set; } = new();
            public chainsaw.InGameShopItemStockSetting _StockSetting { get; set; } = new();
            public chainsaw.InGameShopItemCaptionSetting _CaptionSetting { get; set; } = new();
            public chainsaw.InGameShopItemSaleSetting _SaleSetting { get; set; } = new();
        }
    }
    internal class InGameShopItemStockSetting
    {
        public System.Int32 _Difficulty { get; set; }
        public System.Boolean _EnableStockSetting { get; set; }
        public System.Boolean _EnableSelectCount { get; set; }
        public System.Int32 _MaxStock { get; set; }
        public System.Int32 _DefaultStock { get; set; }
    }
    internal class InGameShopItemUnlockSetting
    {
        public System.UInt32 _UnlockCondition { get; set; }
        public System.Guid _UnlockFlag { get; set; }
        public System.Int32 _UnlockTiming { get; set; }
        public System.UInt32 _SpCondition { get; set; }
    }
    internal class InGameShopPurchaseCategorySettingUserdata
    {
        public System.Collections.Generic.List<chainsaw.InGameShopPurchaseCategorySingleSetting> _Settings { get; set; } = [];
    }
    internal class InGameShopPurchaseCategorySingleSetting
    {
        public System.Int32 _Category { get; set; }
        public System.Int32 _Priority { get; set; }
        public System.Guid _MessageId { get; set; }
        public System.Collections.Generic.List<Data> _Datas { get; set; } = [];
        internal class Data
        {
            public System.Int32 _ItemId { get; set; }
            public System.Int32 _SortPriority { get; set; }
        }
    }
    internal class CharacterWeaponDamageRateUserData
    {
        public System.Collections.Generic.List<chainsaw.CharacterWeaponDamageRateUserData.Data> _DataList { get; set; } = [];
        internal class Data
        {
            public System.Int32 _WeaponID { get; set; }
            public System.Boolean STRUCT__DamageRate__HasValue { get; set; }
            public System.Single STRUCT__DamageRate__Value { get; set; }
            public System.Boolean STRUCT__WinceRate__HasValue { get; set; }
            public System.Single STRUCT__WinceRate__Value { get; set; }
            public System.Boolean STRUCT__BreakRate__HasValue { get; set; }
            public System.Single STRUCT__BreakRate__Value { get; set; }
            public System.Boolean STRUCT__StoppingRate__HasValue { get; set; }
            public System.Single STRUCT__StoppingRate__Value { get; set; }
            public System.Single _Probability { get; set; }
        }
    }
    internal class AttacheCaseSkinEffectSettingUserdata
    {
        public System.Collections.Generic.List<chainsaw.AttacheCaseSkinEffectSingleSettingData> _Settings { get; set; } = [];
    }
    internal class AttacheCaseSkinEffectSingleSettingData
    {
        public System.Int32 _ItemId { get; set; }
        public System.Collections.Generic.List<chainsaw.StatusEffectSetting> _Effects { get; set; } = [];
    }

    internal class ScenarioFlagData
    {
        public System.Collections.Generic.List<Data> Datas { get; set; } = [];
        internal class Block
        {
            public System.Int32 Group { get; set; }
            public System.Int32 Num { get; set; }
            public System.Boolean ReadOnly { get; set; }
            public System.Boolean ResetInNewGame { get; set; }
        }
        internal class Data
        {
            public System.String DataName { get; set; } = "";
            public System.Int32 DigitNum { get; set; }
            public System.Int32 DigitIndex { get; set; }
            public System.Collections.Generic.List<chainsaw.ScenarioFlagData.Block> Block { get; set; } = [];
        }
    }

    internal class WeaponPartsCombineDefinitionUserdata
    {
        public System.Collections.Generic.List<WeaponPartsCombineDefinition> _Datas { get; set; } = [];
    }

    internal class WeaponPartsCombineDefinition
    {
        public int _ItemId { get; set; }
        public System.Collections.Generic.List<int> _TargetItemIds { get; set; } = [];
    }

    internal class ItemDefinitionUserData
    {
        public System.Collections.Generic.List<chainsaw.ItemDefinitionUserData.Data> _Datas { get; set; } = [];

        public class Data
        {
            public System.Int32 _ItemId { get; set; }
            public chainsaw.ItemDefiniition _ItemDefineData { get; set; } = new();
            public chainsaw.WeaponItemDefinition _WeaponDefineData { get; set; } = new();
        }
    }

    internal class ItemDefiniition
    {
        public System.Int32 _ItemSize { get; set; }
        public System.Int32 _StackMax { get; set; }
        public System.Int32 _DefaultDurabilityMax { get; set; }
        public System.Collections.Generic.List<chainsaw.ItemUseResult> _UseResults { get; set; } = [];
        public chainsaw.EquipRequirement _EquipRequirement { get; set; } = new();
        public chainsaw.AdditionalRequirement _AdditionalRequirement { get; set; } = new();
    }

    internal class WeaponItemDefinition : ItemDefiniition
    {
        public System.Int32 _AmmoMax { get; set; }
        public System.Int32 _AmmoCost { get; set; }
        public System.Collections.Generic.List<System.Int32> _UsableAmmoList { get; set; } = [];
        public System.Collections.Generic.List<System.Int32> _TradableWeaponList { get; set; } = [];
    }

    internal class ItemUseResult
    {
        public System.Int32 _ResultType { get; set; }
        public chainsaw.ItemUseResultInfoBase _ResultInfo { get; set; } = new();
    }

    internal class ItemUseResultInfoBase
    {
    }

    internal class EquipRequirement : ItemUseResultInfoBase
    {
        public System.UInt32 _EquipableTarget { get; set; }
    }

    internal class AdditionalRequirement : ItemUseResultInfoBase
    {
        public System.UInt32 _DedicatedTarget { get; set; }
    }

    internal class ItemUseResult_HealHitPoint : ItemUseResultInfoBase
    {
        public System.Boolean _FullHealHitPoint { get; set; }
        public System.Int32 _HealHitPoint { get; set; }
    }

    internal class ItemUseResult_IncreaseHitPoint : ItemUseResultInfoBase
    {
        public System.Int32 _IncreaseHitPoint { get; set; }
    }

    internal class DropItemSaveDataTable
    {
        public System.Collections.Generic.List<chainsaw.DropItemSaveDataTable.Data> Datas { get; set; } = [];

        public class Data
        {
            public chainsaw.ContextID ID { get; set; } = new();
            public chainsaw.DropItemContext.SaveData ItemData { get; set; } = new();
            public chainsaw.DropItemContext.StaticData ItemStatic { get; set; } = new();
        }
    }

    internal class DropItemContext
    {
        public ContextID _ID { get; set; }

        public class SaveData
        {
            public System.Int32 ItemID { get; set; }
            public System.Int32 Count { get; set; }
            public System.Int32 AmmoItemID { get; set; }
            public System.Int32 AmmoCount { get; set; }
            public System.Int32 Durability { get; set; }
            public System.Int32 StageID { get; set; }
            public System.UInt32 Attr { get; set; }
            public System.Int32 StatusEffect { get; set; }
            public System.Boolean STRUCT_Position__HasValue { get; set; }
            public System.Numerics.Vector3 STRUCT_Position__Value { get; set; } = new();
            public System.Boolean STRUCT_DisplayPosition__HasValue { get; set; }
            public System.Numerics.Vector3 STRUCT_DisplayPosition__Value { get; set; } = new();
            public System.Boolean STRUCT_DisplayRotation__HasValue { get; set; }
            public System.Numerics.Quaternion STRUCT_DisplayRotation__Value { get; set; } = new();
            public System.Boolean STRUCT_ColliderScale__HasValue { get; set; }
            public System.Single STRUCT_ColliderScale__Value { get; set; }
        }

        public class StaticData
        {
            public System.Boolean HasRelation { get; set; }
            public chainsaw.DropItemContext.RelationData Relation { get; set; } = new();
            public System.Numerics.Vector3 InitPosition { get; set; } = new();
            public System.Boolean IgnoreTreasureMap { get; set; }
            public System.Int32 MapFloorID { get; set; }
            public System.Boolean IsDLC { get; set; }
            public System.Int32 SubMapStageID { get; set; }
            public System.Numerics.Vector3 SubMapPosition { get; set; } = new();
        }

        public class RelationData
        {
            public chainsaw.ContextID Target { get; set; } = new();
        }
    }

    internal class ContextID : System.IEquatable<chainsaw.ContextID>
    {
        public System.SByte _Category { get; set; }
        public System.Byte _Kind { get; set; }
        public System.Int32 _Group { get; set; }
        public System.Int32 _Index { get; set; }

        public override string ToString() => $"CTXID({_Category},{_Kind},{_Group},{_Index})";

        public override bool Equals(object? obj) => obj is chainsaw.ContextID id && Equals(id);

        public bool Equals(chainsaw.ContextID? other) =>
            other is chainsaw.ContextID b &&
            _Category == b._Category &&
            _Kind == b._Kind &&
            _Group == b._Group &&
            _Index == b._Index;

        public override int GetHashCode() => System.HashCode.Combine(_Category, _Kind, _Group, _Index);
        public static bool operator ==(chainsaw.ContextID left, chainsaw.ContextID right) => left.Equals(right);
        public static bool operator !=(chainsaw.ContextID left, chainsaw.ContextID right) => !(left == right);
    }

    internal class GimmickSaveDataTable
    {
        public System.Collections.Generic.List<chainsaw.GimmickSaveDataTable.Data> Datas { get; set; } = [];

        public class Data
        {
            public chainsaw.ContextID ID { get; set; } = new();
            public chainsaw.GimmickContext.SaveData Save { get; set; } = new();
            public chainsaw.GimmickContext.StaticData Static { get; set; } = new();
            public System.Collections.Generic.List<chainsaw.GimmickContext.MapData> Maps { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.GimmickManager.AccessPoint> AccessPoints { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.GmContextAIMapEff.ShapeData> AIMapData { get; set; } = [];
            public string ContextType { get; set; } = "";
        }
    }

    internal class GimmickContext
    {
        public chainsaw.ContextID ID { get; set; } = new();

        public class SaveData
        {
            public System.Collections.Generic.List<System.Byte> Attr { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.GimmickContext.TriggerDone> TriggerSave { get; set; } = [];
            public System.Boolean IsDetected { get; set; }
            public System.Collections.Generic.List<chainsaw.GimmickContext.AccessoryData> AccDatas { get; set; } = [];
        }

        public class TriggerDone
        {
            public System.UInt32 SaveID { get; set; }
            public System.UInt32 Done { get; set; }
        }

        public class AccessoryData
        {
        }

        public class StaticData
        {
        }

        public class MapData
        {
            public string MapName { get; set; } = "";
            public System.Numerics.Vector3 MapPosition { get; set; } = new();
            public System.Int32 _StageID { get; set; }
            public System.Collections.Generic.List<System.Int32> MapFloorIDs { get; set; } = [];
            public System.Boolean NeedCheckClearing { get; set; }
        }
    }

    internal class GimmickManager
    {
        public class AccessPoint
        {
            public System.Numerics.Vector3 Position { get; set; } = new();
            public System.Int32 Access { get; set; }
        }
    }
    internal class GmContextLadder
    {
        public class StaticDataLadder : chainsaw.GimmickContext.StaticData
        {
            public Point PointTop { get; set; } = new();
            public Point PointBottom { get; set; } = new();
            public bool IsEnemyOnly { get; set; }
            public System.Collections.Generic.List<chainsaw.RuleStratum.StratumBool> HideRule { get; set; } = [];
            public System.Collections.Generic.List<chainsaw.RuleStratum.StratumBool> SleepRule { get; set; } = [];
            public class Point
            {
                public Vector3 Position { get; set; }
                public float Rotation { get; set; }
                public int Stage { get; set; }
            }
        }
    }
    internal class GmContextAIMapEff
    {
        public chainsaw.ContextID ID { get; set; } = new();

        public class ShapeData
        {
            public System.Numerics.Vector3 Position { get; set; } = new();
            public System.Single RotationY { get; set; }
            public string ShapeName { get; set; } = "";
            public System.Collections.Generic.List<System.Int32> Stage { get; set; } = [];
        }
    }

    internal class FlagCondition
    {
        public System.Collections.Generic.List<CheckFlagInfo> _CheckFlags { get; set; } = [];
        public int _Logic { get; set; }
    }

    internal class FlagConditionStrict : FlagCondition
    {
    }

    internal class CheckFlagInfo
    {
        public System.Guid _CheckFlag { get; set; }
        public bool _CompareValue { get; set; }
    }
    internal class OptionSettings<T>
    {
        public System.Collections.Generic.List<T> _Params { get; set; } = [];
    }
    internal class CheckFlagSettings
    {
        public bool Enabled { get; set; }
        public OptionSettings<chainsaw.CheckFlagSettings.Param> _Params { get; set; } = new();
        internal class Param
        {
            public uint _KeyHash { get; set; }
            public uint _BindTriggerNameHash { get; set; }
            public FlagCondition _FlagCondition { get; set; } = new();
        }
    }
    internal class SetFlagSettings
    {
        public bool Enabled { get; set; }
        public OptionSettings<chainsaw.SetFlagSettings.Param> _Params { get; set; } = new();
        internal class Param
        {
            public uint _KeyHash { get; set; }
            public uint _BindTriggerNameHash { get; set; }
            public System.Collections.Generic.List<SetFlagData> _SetFlags { get; set; } = new();
        }
        internal class SetFlagData
        {
            public System.Guid _Flag { get; set; }
        }
    }
    internal class InventoryCatalogUserData
    {
        public System.Int32 _PTAS { get; set; }
        public System.Int32 _SpinelCount { get; set; }
        public System.Collections.Generic.List<chainsaw.InventoryCatalogUserData.Data> _Datas { get; set; } = [];
        internal class Data
        {
            public System.Int32 CharacterKindID { get; set; }
            public chainsaw.InventorySaveData InventoryData { get; set; } = new();
            public chainsaw.KeyItemInventorySaveData KeyInventorySaveData { get; set; } = new();
            public chainsaw.TreasureInventorySaveData TreasureInventorySaveData { get; set; } = new();
            public chainsaw.UniqueInventorySaveData UniqueInventorySaveData { get; set; } = new();
            public chainsaw.CharacterInitialSettings CharacterData { get; set; } = new();
        }
    }
    internal class InventorySaveData
    {
        public System.Guid SetupID { get; set; }
        public chainsaw.ContextID ContextID { get; set; } = new();
        public System.Boolean IsTakeOverData { get; set; }
        public chainsaw.InventorySizeSaveData InventorySize { get; set; } = new();
        public System.Collections.Generic.List<chainsaw.InventoryItemSaveData> InventoryItems { get; set; } = [];
        public System.Collections.Generic.List<chainsaw.InventoryEquipSaveData> EquipInfos { get; set; } = [];
        public System.Collections.Generic.List<chainsaw.InventoryShortcutSaveData> ShortcutInfos { get; set; } = [];
        public System.Collections.Generic.List<chainsaw.InventoryActiveShortcutSaveData> ActiveShortcutInfos { get; set; } = [];
    }
    internal class InventorySizeSaveData
    {
        public System.Int32 CurrInventorySize { get; set; }
    }
    internal class InventoryItemSaveData
    {
        public chainsaw.Item Item { get; set; } = new();
        public System.Int32 SlotType { get; set; }
        public System.Int32 STRUCT_SlotIndex_Row { get; set; }
        public System.Int32 STRUCT_SlotIndex_Column { get; set; }
        public System.Int32 CurrDirection { get; set; }
    }
    internal class Item
    {
        public System.Guid _ID { get; set; }
        public System.Int32 _ItemId { get; set; }
        public System.UInt32 _CurrentCondition { get; set; }
        public System.Int32 _CurrentDurability { get; set; }
        public System.Int32 _CurrentItemCount { get; set; }
    }
    internal class WeaponItem : Item
    {
        public System.Int32 _CurrentAmmo { get; set; }
        public System.Int32 _CurrentAmmoCount { get; set; }
        public System.Int32 _CurrentTacticalAmmoCount { get; set; }
        public chainsaw.WeaponPartsCustom _CurrentWeaponPartsCustom { get; set; } = new();
        public chainsaw.CustomLevelInWeapon _CustomLevelInWeapon { get; set; } = new();
        public System.Int32 _LimitBreakCustomPattern { get; set; }
    }
    internal class UniqueItem : Item
    {
    }
    internal class WeaponPartsCustom
    {
        public System.Collections.Generic.List<chainsaw.WeaponPartsCustomSingleData> _Datas { get; set; } = [];
    }
    internal class WeaponPartsCustomSingleData
    {
        public System.Guid _ID { get; set; }
        public System.Int32 _ItemId { get; set; }
    }
    internal class CustomLevelInWeapon
    {
        public System.Boolean _IsReflect { get; set; }
        public System.Boolean _IsReticleFit { get; set; }
        public System.Collections.Generic.List<chainsaw.CommonLevelInWeapon> _CommonLevelInWeapon { get; set; } = [];
        public System.Collections.Generic.List<chainsaw.IndividualLevelInWeapon> _IndividualLevelInWeapon { get; set; } = [];
        public System.Collections.Generic.List<chainsaw.LimitBreakLevelInWeapon> _LimitBreakLevelInWeapon { get; set; } = [];
    }
    internal class CommonLevelInWeapon
    {
        public System.Int32 _NowLevel { get; set; }
        public System.Int32 _CommonCustomCategory { get; set; }
        public System.Int32 _DamageRateLevel { get; set; }
        public System.Int32 _WinceRateLevel { get; set; }
        public System.Int32 _BreakRateLevel { get; set; }
        public System.Int32 _StoppingRateLevel { get; set; }
        public System.Int32 _ExplosionRadiusLevel { get; set; }
        public System.Int32 _ExplosionSensorRadiusLevel { get; set; }
        public System.Int32 _RandomRadiusLevel { get; set; }
        public System.Int32 _RandomRadius_FitLevel { get; set; }
        public System.Int32 _ReticleFitParamLevel { get; set; }
        public System.Int32 _CameraRecoilParamLevel { get; set; }
        public System.Int32 _CameraShakeParamLevel { get; set; }
        public System.Int32 _WeaponHandShakeParamLevel { get; set; }
        public System.Int32 _ReticleGuiTypeLevel { get; set; }
        public System.Int32 _AmmoMaxLevel { get; set; }
        public System.Int32 _ReloadNumLevel { get; set; }
    }
    internal class IndividualLevelInWeapon
    {
        public System.Int32 _NowLevel { get; set; }
        public System.Int32 _IndividualCustomCategory { get; set; }
        public System.Int32 _CriticalRate_NormalLevel { get; set; }
        public System.Int32 _CriticalRate_FitLevel { get; set; }
        public System.Int32 _ThroughNum_NormalLevel { get; set; }
        public System.Int32 _ThroughNum_FitLevel { get; set; }
        public System.Int32 _ReloadNumLevel { get; set; }
        public System.Int32 _ReloadSpeedRateLevel { get; set; }
        public System.Int32 _DurabilityMaxLevel { get; set; }
        public System.Int32 _RapidSpeedLevel { get; set; }
        public System.Int32 _PumpActionRapidSpeedLevel { get; set; }
        public System.Int32 _AmmoCostLevel { get; set; }
        public System.Int32 _FlameDistanceLevel { get; set; }
    }
    internal class LimitBreakLevelInWeapon
    {
        public System.Int32 _NowLevel { get; set; }
        public System.Int32 _LimitBreakCustomCategory { get; set; }
    }
    internal class InventoryEquipSaveData
    {
        public System.Guid ID { get; set; }
    }
    internal class InventoryShortcutSaveData
    {
        public System.Guid ID { get; set; }
        public System.Int32 EquipType { get; set; }
        public System.Int32 ShortcutType { get; set; }
        public System.Int32 Direction { get; set; }
        public System.Int32 ItemId { get; set; }
        public System.Int32 ItemCount { get; set; }
    }
    internal class InventoryActiveShortcutSaveData
    {
        public System.Int32 EquipType { get; set; }
        public System.Int32 ShortcutType { get; set; }
        public System.Int32 ActiveDirection { get; set; }
    }
    internal class KeyItemInventorySaveData
    {
        public System.Guid SetupID { get; set; }
        public chainsaw.ContextID ContextID { get; set; } = new();
        public System.Boolean IsTakeOverData { get; set; }
        public System.Collections.Generic.List<chainsaw.KeyItemInventoryItemSaveData> Items { get; set; } = [];
    }
    internal class KeyItemInventoryItemSaveData
    {
        public chainsaw.Item Item { get; set; } = new();
        public System.Int32 STRUCT_SlotIndex_Row { get; set; }
        public System.Int32 STRUCT_SlotIndex_Column { get; set; }
    }
    internal class TreasureInventorySaveData
    {
        public System.Guid SetupID { get; set; }
        public chainsaw.ContextID ContextID { get; set; } = new();
        public System.Boolean IsTakeOverData { get; set; }
        public System.Collections.Generic.List<chainsaw.TreasureInventoryItemSaveData> Items { get; set; } = [];
    }
    internal class TreasureInventoryItemSaveData
    {
        public chainsaw.Item Item { get; set; } = new();
        public System.Int32 STRUCT_SlotIndex_Row { get; set; }
        public System.Int32 STRUCT_SlotIndex_Column { get; set; }
    }
    internal class UniqueInventorySaveData
    {
        public System.Guid SetupID { get; set; }
        public chainsaw.ContextID ContextID { get; set; } = new();
        public System.Boolean IsTakeOverData { get; set; }
        public System.Collections.Generic.List<chainsaw.UniqueInventoryItemSaveData> Items { get; set; } = [];
    }
    internal class UniqueInventoryItemSaveData
    {
        public chainsaw.Item Item { get; set; } = new();
    }
    internal class CharacterInitialSettings
    {
        public System.Int32 _CharacterMaxHP { get; set; }
    }
    internal class RuleStratum
    {
        internal class StratumBool
        {
            public Rule _Enable { get; set; } = new();
            public bool Value { get; set; }
        }
        internal class Rule
        {
            public int Logic { get; set; }
            public System.Collections.Generic.List<Container> Matters { get; set; } = [];
        }
        internal class Container
        {
            public Particle _Data { get; set; } = new();
        }
        internal class Particle
        {
        }
        internal class ParticleChapter : Particle
        {
            public int Compare { get; set; }
            public int Chapter { get; set; }
        }
        internal class ParticleFlag : Particle
        {
            public chainsaw.FlagCondition Flags { get; set; }
        }
    }
    internal class DetailSearchFileUserdata
    {
        public int _ID { get; set; }
        public int _BrowseType { get; set; }
        public chainsaw.BrowsingSimpleSetting _BrowseSetting { get; set; } = new();
    }
    internal class BrowsingSimpleSetting
    {
        public bool _DisplayOffAfterAccess { get; set; }
    }
    internal class FileSettingUserdata
    {
        public System.Collections.Generic.List<chainsaw.FileSettingUserdata.Data> _Datas { get; set; } = [];

        internal class Data
        {
            public bool _Enable { get; set; }
            public int _FileID { get; set; }
            public int _LocationType { get; set; }
            public System.Guid _MsgID { get; set; }
            public System.Collections.Generic.List<chainsaw.FileSettingUserdata.EachPage> _EachPage { get; set; } = [];
        }
        internal class EachPage
        {
            public int _BackTextureID { get; set; }
        }
    }
    internal class GmContextReadFile
    {
        internal class StaticDataReadFile : chainsaw.GimmickContext.StaticData
        {
            public Vector3 Position { get; set; }
            public int DocID { get; set; }
            public int Stage { get; set; }
        }
    }
    internal class CampaignInitialSettingUserData
    {
        public System.Collections.Generic.List<chainsaw.CampaignInitialSetting> _CampaignInitialSettingList { get; set; } = [];
    }
    internal class CampaignInitialSetting
    {
        public int _Campaign { get; set; }
        public int _Chapter { get; set; }
        public int _SpecialJumpSequence { get; set; }
        public System.Collections.Generic.List<chainsaw.CampaignInitialSetting.CharacterProperty> _CharacterList { get; set; } = [];
        public RszUserDataNode _InventoryCatalogUserData { get; set; }
        public RszUserDataNode _FileCatalogUserdata { get; set; }
        public System.Collections.Generic.List<System.Guid> _FlagList { get; set; } = [];
        public int _SaveCount { get; set; }
        public chainsaw.MainCampaignGameClearData _MainCampaignGameClearInfo { get; set; }
        public chainsaw.JustClearFlag _JustClear { get; set; }
        internal class CharacterProperty
        {
            public int _Character { get; set; }
            public chainsaw.PlayerCostumeSelector _Costume { get; set; }
            public System.Collections.Generic.List<int> _AccessoryIds { get; set; } = [];
            public int _NetworkPlayerID { get; set; }
            public int _DefaultRole { get; set; }
            public chainsaw.GameLocator _Locator { get; set; }
        }
    }
    internal class PlayerCostumeSelector
    {
        public uint _ID { get; set; }
    }
    internal class GameLocator
    {
        public int _Stage { get; set; }
        public System.Numerics.Vector3 _Position { get; set; }
        public System.Numerics.Quaternion _Rotation { get; set; }
    }
    internal class MainCampaignGameClearData
    {
        public System.Collections.Generic.List<System.Guid> _ClearedCampaignGuids { get; set; } = [];
    }
    internal class JustClearFlag
    {
        public int _NextChapterKey { get; set; }
        public bool _MainCampaign { get; set; }
    }
    internal class EventChapterChangeUserData
    {
        public System.Collections.Generic.List<chainsaw.EventChapterChangeUserData.Item> _ItemList { get; set; } = [];
        internal class Item
        {
            public int NextCampaign { get; set; }
            public int ChapterID { get; set; }
            public int PrevMovieID { get; set; }
            public int PrevTimelineID { get; set; }
            public int NextMovieID { get; set; }
            public int NextTimelineID { get; set; }
            public System.Collections.Generic.List<chainsaw.EventChapterChangeUserData.Item.NextReserveEvent> NextReserveEventList { get; set; } = [];
            internal class NextReserveEvent
            {
                public int MovieID { get; set; }
                public int TimelineID { get; set; }
            }
        }
    }
}

namespace chainsaw.gui.shop
{
    internal class InGameShopAdjustParam
    {
        public AdjustParam00 _Param00 { get; set; }
        internal class AdjustParam00
        {
            public System.Boolean _IsRegister { get; set; }
            public System.Single _MaxHpRatio { get; set; }
        }
        internal class AdjustParamBase
        {
            public System.Boolean _IsRegister { get; set; }
        }
    }
    internal class InGameShopRepairSetting
    {
        public System.Int32 _ItemId { get; set; }
        public System.Collections.Generic.List<Setting> _Settings { get; set; } = [];
        internal class Setting
        {
            public System.Int32 _Difficulty { get; set; }
            public System.Int32 _Commission { get; set; }
            public System.Single _DurabilityCost { get; set; }
            public System.Int32 _RepairCost { get; set; }
        }
    }
    internal class ItemPrice
    {
        public System.Int32 _PurchasePrice { get; set; }
        public System.Int32 _SellingPrice { get; set; }
    }
    internal class ItemPriceSetting
    {
        public System.Int32 _Difficulty { get; set; }
        public chainsaw.gui.shop.ItemPrice _Price { get; set; } = new();
    }
}
namespace via
{
    internal class AnimationCurve
    {
        public System.Collections.Generic.List<KeyFrame> Keys { get; set; } = [];
        public System.Single MinValue { get; set; }
        public System.Single MaxValue { get; set; }
        public System.Single LoopStartTime { get; set; }
        public System.Single LoopEndTime { get; set; }
        public System.UInt32 LoopCount { get; set; }
        public System.Int32 LoopWrapNo { get; set; }
        internal enum Wrap
        {
        }
        internal class WrappedArrayContainer_Keys
        {
        }
    }
}
