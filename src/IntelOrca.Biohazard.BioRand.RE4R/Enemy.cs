using System;
using System.Linq;
using IntelOrca.Biohazard.REE.Rsz;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class Enemy
    {
        public Area Area { get; }
        public RszGameObject GameObject { get; private set; }
        public RszObjectNode MainComponent { get; private set; }

        public Enemy(Area area, RszGameObject gameObject, RszObjectNode mainComponent)
        {
            Area = area;
            GameObject = gameObject;
            MainComponent = mainComponent;
        }

        public RszGameObject Apply()
        {
            GameObject = GameObject.AddOrUpdateComponent(MainComponent);
            return GameObject;
        }

        public Guid Guid => GameObject.Guid;
        public EnemyKindDefinition Kind => Area.EnemyClassFactory.FindEnemyKind(MainComponent.Type.Name)!;

        public Transform Transform
        {
            get => new Transform(GameObject);
            set => GameObject = value.UpdateGameObject(GameObject);
        }

        public ContextId ContextId
        {
            get
            {
                var contextId = (RszObjectNode)GetFieldValue("_ContextID")!;
                return ContextId.FromRsz(contextId);
            }
            set
            {
                MainComponent = MainComponent.SetField("_ContextID", value.ToRsz(FileRepository.RszRepository));
            }
        }

        public int StageID => GetFieldValue<int>("_StageID");

        public int? Health
        {
            get
            {
                var hasValue = GetFieldValue("STRUCT__HitPoint__HasValue");
                if (hasValue is not true)
                    return null;
                return GetFieldValue<int>("STRUCT__HitPoint__Value");
            }
            set
            {
                if (value == null)
                {
                    SetFieldValue("STRUCT__HitPoint__HasValue", false);
                    SetFieldValue("STRUCT__HitPoint__Value", 0);
                }
                else
                {
                    SetFieldValue("STRUCT__HitPoint__HasValue", true);
                    SetFieldValue("STRUCT__HitPoint__Value", value.Value);
                }
            }
        }

        private string MontageIdName
        {
            get
            {
                var arr = new[] { "_Ch1c0z2MontageID", "_Ch1c0z1MontageID", "_MontageID" };
                foreach (var a in arr)
                {
                    if (MainComponent.Type.Fields.Any(x => x.Name == a))
                    {
                        return a;
                    }
                }
                return arr.Last();
            }
        }

        public uint MontageId
        {
            get => (uint?)GetFieldValue(MontageIdName) ?? 0;
            set => SetFieldValue(MontageIdName, value);
        }

        public int Weapon
        {
            get => GetFieldValue("_EquipWeapon") as int? ?? 0;
            set => SetFieldValue("_EquipWeapon", value);
        }

        public int SecondaryWeapon
        {
            get => GetFieldValue("_SubWeapon") as int? ?? 0;
            set => SetFieldValue("_SubWeapon", value);
        }

        public Item? ItemDrop
        {
            get
            {
                var shouldDropItem = GetFieldValue("_ShouldDropItem");
                if (shouldDropItem is true)
                {
                    var shouldDropItemAtRandom = GetFieldValue("_ShouldDropItemAtRandom");
                    var dropItemId = GetFieldValue<int>("_DropItemID");
                    var dropItemCount = GetFieldValue<int>("_DropItemCount");
                    return new Item(dropItemId, dropItemCount);
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (value is Item drop)
                {
                    SetFieldValue("_ShouldDropItem", true);
                    SetFieldValue("_ShouldDropItemAtRandom", false);
                    SetFieldValue("_DropItemID", drop.Id);
                    SetFieldValue("_DropItemCount", drop.Count);
                }
                else
                {
                    SetFieldValue("_ShouldDropItem", false);
                    SetFieldValue("_ShouldDropItemAtRandom", false);
                    SetFieldValue("_DropItemID", -1);
                    SetFieldValue("_DropItemCount", 0);
                }
            }
        }

        public bool ShouldDropItemAtRandom
        {
            get => GetFieldValue("_ShouldDropItemAtRandom") is true;
            set => SetFieldValue("_ShouldDropItemAtRandom", value);
        }

        public bool IsLeftHanded
        {
            get => GetFieldValue("_IsLeftHanded") is true;
            set => SetFieldValue("_IsLeftHanded", value);
        }

        public uint RolePatternHash
        {
            get => GetFieldValue<uint>("_RolePatternHash");
            set => SetFieldValue("_RolePatternHash", value);
        }

        public uint PreFirstForceMovePatternHash
        {
            get => GetFieldValue<uint>("_PreFirstForceMovePatternHash");
            set => SetFieldValue("_PreFirstForceMovePatternHash", value);
        }

        public int? ParasiteKind
        {
            get => GetFieldValue<int?>("_ParasiteSetting");
            set => SetFieldValue("_ParasiteSetting", value);
        }

        public bool ForceParasiteAppearance
        {
            get => GetFieldValue<bool?>("_ForceParasiteAppearance") ?? false;
            set => SetFieldValue("_ForceParasiteAppearance", value);
        }

        public float ParasiteAppearanceProbability
        {
            get => GetFieldValue<float?>("_ParasiteAppearanceProbability") ?? 0;
            set => SetFieldValue("_ParasiteAppearanceProbability", value);
        }

        public object? GetFieldValue(string name)
        {
            if (MainComponent.Type.FindFieldIndex(name) == -1)
                return null;

            var val = MainComponent[name];
            if (val is RszValueNode valueNode)
                return RszSerializer.Deserialize(valueNode);
            return val;
        }

        public T? GetFieldValue<T>(string name) => (T?)GetFieldValue(name);

        public void SetFieldValue<T>(string name, T value)
        {
            if (MainComponent.Type.FindFieldIndex(name) != -1)
            {
                MainComponent = MainComponent.Set(name, value);
            }
        }

        public override string ToString()
        {
            var componentName = MainComponent.Type.Name;
            var cutOff = componentName.IndexOf("Spawn");
            if (cutOff != -1)
                componentName = componentName[..cutOff];
            cutOff = componentName.IndexOf(".");
            if (cutOff != -1)
                componentName = componentName[(cutOff + 1)..];
            return componentName.ToLower();
        }
    }
}
