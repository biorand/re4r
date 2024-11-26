#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace chainsaw
{
    internal class CameraRecoilParam
    {
        public RszTool.via.Range _YawRangeDeg { get; set; }
        public RszTool.via.Range _PitchRangeDeg { get; set; }
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
            public RszTool.via.Range _Period { get; set; }
            public RszTool.via.Range _TranslationXRange { get; set; }
            public RszTool.via.Range _TranslationYRange { get; set; }
            public RszTool.via.Range _TranslationZRange { get; set; }
            public RszTool.via.Range _RotationXRange { get; set; }
            public RszTool.via.Range _RotationYRange { get; set; }
            public RszTool.via.Range _RotationZRange { get; set; }
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
            public RszTool.via.Range CorrectHitRateNormalizeRange { get; set; }
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
            public System.String ParentJointName { get; set; }
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
        public System.String CameraJoint { get; set; }
        public System.Collections.Generic.List<System.Single> _Rates { get; set; }
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
        public System.Collections.Generic.List<WeaponDetailStage> _WeaponDetailStages { get; set; }
        internal class WeaponDetailStage
        {
            public System.Int32 _WeaponID { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.WeaponDetailCustom _WeaponDetailCustom { get; set; }
        }
        internal class AmmoCost
        {
            public System.Collections.Generic.List<System.Int32> _AmmoCostNum { get; set; }
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
            public chainsaw.WeaponDetailCustomUserdata.CriticalRate _CriticalRate { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.ThroughNum _ThroughNums { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.ReloadSpeed _ReloadSpeed { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.Strength _Strength { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.Rapid _Rapid { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.AmmoCost _AmmoCost { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.FlameDistance _FlameDistance { get; set; }
            public System.Collections.Generic.List<System.String> _Others { get; set; }
            public System.Int32 _ItemID { get; set; }
            public System.Collections.Generic.List<System.Int32> _UsableAmmoList { get; set; }
        }
        internal class LimitBreakRapid
        {
            public System.Single _RapidSpeedScale { get; set; }
        }
        internal class FlameDistance
        {
            public System.Collections.Generic.List<System.Single> _ShellDistance { get; set; }
        }
        internal class CommonCustom
        {
            public System.Int32 _CommonCustomCategory { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.AttackUp _AttackUp { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.Stabilization _Stabilization { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.AmmoMaxUp _AmmoMaxUp { get; set; }
            public chainsaw.WeaponDetailCustomUserdata.AttackUp _ShotGunAroundAttackUp { get; set; }
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
            public System.Collections.Generic.List<System.Int32> _AmmoMaxs { get; set; }
            public System.Collections.Generic.List<System.Int32> _ReloadNum { get; set; }
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
            public System.Collections.Generic.List<chainsaw.CameraRecoilParam> _CustomLevelCameraRecoilParam { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponHandShakeParam> _CustomLevelWeaponHandShakeParam { get; set; }
            public System.Collections.Generic.List<System.Int32> _MeshPartsNums { get; set; }
            public System.Collections.Generic.List<System.Int32> _HideMeshPartsNums { get; set; }
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
            public System.Collections.Generic.List<System.Single> _RandomRadiuses { get; set; }
            public System.Collections.Generic.List<System.Single> _RandomRadius_Fits { get; set; }
            public System.Collections.Generic.List<System.Int32> _TerrainHitSoundTypes { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponReticleFitParam> _ReticleFitParams { get; set; }
            public System.Collections.Generic.List<chainsaw.CameraRecoilParam> _CameraRecoilParams { get; set; }
            public System.Collections.Generic.List<chainsaw.CameraShakeParam> _CameraShakeParams { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponHandShakeParam> _WeaponHandShakeParams { get; set; }
            public System.Collections.Generic.List<System.UInt32> _ReticleGuiTypes { get; set; }
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
            public System.Collections.Generic.List<chainsaw.WeaponDetailCustomUserdata.CommonCustom> _CommonCustoms { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponDetailCustomUserdata.IndividualCustom> _IndividualCustoms { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponDetailCustomUserdata.AttachmentCustom> _AttachmentCustoms { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponDetailCustomUserdata.LimitBreakCustom> _LimitBreakCustoms { get; set; }
        }
        internal class AttachmentCustom
        {
            public System.Int32 _ItemID { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponDetailCustomUserdata.AttachmentParam> _AttachmentParams { get; set; }
        }
        internal class LimitBreakCombatSpeed
        {
            public System.Single _CombatSpeed { get; set; }
        }
    }
    internal class WeaponEquipParam
    {
        public System.String ParentJointName { get; set; }
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
        public RszTool.via.Range _PointRange { get; set; }
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
        public System.Collections.Generic.List<Setting> _Settings { get; set; }
        internal class Setting
        {
            public System.Int32 _Category { get; set; }
            public RszTool.via.Range _Range { get; set; }
            public System.Single _Rate { get; set; }
            public System.Collections.Generic.List<chainsaw.StabilityEvaluationSetting> _StabilityEvaluationSettings { get; set; }
            public System.Collections.Generic.List<chainsaw.SpCategoryEvaluationSettingBase> _SpCategoryEvaluationSettings { get; set; }
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
        public System.Int32 PartsItemId { get; set; }
    }
    internal class StabilityEvaluationSetting
    {
        public System.Int32 PartsItemId { get; set; }
        public System.Single Value { get; set; }
    }
    internal class WeaponCustomUserdata
    {
        public System.Collections.Generic.List<WeaponStage> _WeaponStages { get; set; }
        public System.Collections.Generic.List<ItemStage> _ItemStages { get; set; }
        public System.Collections.Generic.List<chainsaw.RaderChartGuiSingleSettingData> _RaderChartGuiSingleSettingDatas { get; set; }
        internal class ReloadSpeedCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.ReloadSpeedParam> _ReloadSpeedParams { get; set; } = [];
        }
        internal class CustomFlameDistance
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.FlameDistanceCustomStage> _FlameDistanceCustomStages { get; set; }
        }
        internal class UsableAmmoCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; }
            public chainsaw.WeaponCustomUserdata.ChangeLevel _ChangeLevel { get; set; }
        }
        internal class WeaponCustom
        {
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.Common> _Commons { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.Individual> _Individuals { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.LimitBreak> _LimitBreak { get; set; }
        }
        internal class StabilizationParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _Stabilization { get; set; }
        }
        internal class StabilizationCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.StabilizationParam> _StabilizationParams { get; set; }
        }
        internal class RapidCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.RapidParam> _RapidParams { get; set; } = [];
        }
        internal class CustomAmmoMaxUp
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AmmoMaxUpCustomStage> _AmmoMaxUpCustomStages { get; set; }
        }
        internal class StrengthParam
        {
            public System.Int32 _Level { get; set; }
            public System.Int32 _Strength { get; set; }
        }
        internal class CustomPolish
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.PolishCustomStage> _PolishCustomStages { get; set; }
        }
        internal class Individual
        {
            public System.Int32 _IndividualCustomCategory { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomCriticalRate _CustomCriticalRate { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomThroughNum _CustomThroughNum { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomUsableAmmo _CustomUsableAmmo { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomReloadSpeed _CustomReloadSpeed { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomStage _CustomReload { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomRepair _CustomRepair { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomPolish _CustomPolish { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomStrength _CustomStrength { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomRapid _CustomRapid { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomOtherIndividual _CustomOtherIndividual { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomAmmoCost _CustomAmmoCost { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomFlameDistance _CustomFlameDistance { get; set; }
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
            public System.String _Info { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.CriticalRateParam> _CriticalRateParams { get; set; } = [];
        }
        internal class RepairParam
        {
            public System.Int32 _Level { get; set; }
        }
        internal class CustomOtherIndividual
        {
            public System.Guid _MessageId { get; set; }
            public System.String _str { get; set; }
        }
        internal class CustomAmmoCost
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AmmoCostCustomStage> _AmmoCostCustomStages { get; set; }
        }
        internal class CustomAttackUp
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AttackUpCustomStage> _AttackUpCustomStages { get; set; } = [];
        }
        internal class CustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; }
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
            public System.String _Info { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.FlameDistanceParam> _FlameDistanceParams { get; set; }
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
            public System.String _Info { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AttackUpParam> _AttackUpParams { get; set; } = [];
        }
        internal class AmmoMaxUpCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AmmoMaxUpParam> _AmmoMaxUpParams { get; set; }
        }
        internal class ItemCustom
        {
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.Common> _Commons { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.Individual> _Individuals { get; set; }
        }
        internal class CustomUsableAmmo
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.UsableAmmoCustomStage> _UsableAmmoCustomStages { get; set; }
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
            public System.String _Info { get; set; }
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
            public chainsaw.WeaponCustomUserdata.CustomAttackUp _CustomAttackUp { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomStabilization _CustomStabilization { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomAmmoMaxUp _CustomAmmoMaxUp { get; set; }
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
            public System.String _Info { get; set; }
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
            public System.String _Info { get; set; }
        }
        internal class LimitBreak
        {
            public System.Int32 _LimitBreakCustomCategory { get; set; }
            public chainsaw.WeaponCustomUserdata.CustomLimitBreak _CustomLimitBreak { get; set; } = new();
        }
        internal class AmmoCostCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.AmmoCostParam> _AmmoCostParams { get; set; }
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
            public System.String _Info { get; set; }
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
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.RepairCustomStage> _RepairCustomStages { get; set; }
        }
        internal class CustomStabilization
        {
            public System.Guid _MessageId { get; set; }
            public System.Collections.Generic.List<chainsaw.WeaponCustomUserdata.StabilizationCustomStage> _StabilizationCustomStages { get; set; }
        }
        internal class PolishCustomStage
        {
            public System.Int32 _Cost { get; set; }
            public System.String _Info { get; set; }
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
        public System.Collections.Generic.List<chainsaw.WeaponCustomUnlocksingleSetting> _Settings { get; set; }
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
        public System.Collections.Generic.List<System.Numerics.Vector4> v0 { get; set; } = [];
        public System.Single v1 { get; set; }
        public System.Single v2 { get; set; }
        public System.Int32 v3 { get; set; }
        public System.Single v4 { get; set; }
        public System.Int32 v5 { get; set; }
        public System.Int32 v6 { get; set; }
        internal enum Wrap
        {
        }
        internal class WrappedArrayContainer_Keys
        {
        }
    }
}
