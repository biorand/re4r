using System;
using RszTool;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class Enemy
    {
        public Area Area { get; }
        public ScnFile.GameObjectData GameObject { get; }
        public RszInstance MainComponent { get; }

        public Enemy(Area area, ScnFile.GameObjectData gameObject, RszInstance mainComponent)
        {
            Area = area;
            GameObject = gameObject;
            MainComponent = mainComponent;
        }

        public Guid Guid => GameObject.Guid;
        public EnemyKindDefinition Kind => Area.EnemyClassFactory.FindEnemyKind(MainComponent.Name)!;

        public ContextId ContextId
        {
            get
            {
                var contextId = (RszInstance)GetFieldValue("_ContextID")!;
                var category = (sbyte)contextId.GetFieldValue("_Category")!;
                var kind = (byte)contextId.GetFieldValue("_Kind")!;
                var group = (int)contextId.GetFieldValue("_Group")!;
                var index = (int)contextId.GetFieldValue("_Index")!;
                return new ContextId(category, kind, group, index);
            }
            set
            {
                var contextId = (RszInstance)GetFieldValue("_ContextID")!;
                contextId.SetFieldValue("_Category", value.Category);
                contextId.SetFieldValue("_Kind", value.Kind);
                contextId.SetFieldValue("_Group", value.Group);
                contextId.SetFieldValue("_Index", value.Index);
            }
        }

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

        public uint MontageId
        {
            get => (uint?)GetFieldValue("_MontageID") ?? 0;
            set => SetFieldValue("_MontageID", value);
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

        public object? GetFieldValue(string name)
        {
            return MainComponent.GetFieldValue(name);
        }

        public T GetFieldValue<T>(string name)
        {
            return (T)MainComponent.GetFieldValue(name)!;
        }

        public void SetFieldValue<T>(string name, T value)
        {
            var originalValue = MainComponent.GetFieldValue(name);
            if (value is not null && originalValue is not null)
            {
                var originalValueType = originalValue.GetType();
                if (value.GetType() != originalValueType)
                {
                    MainComponent.SetFieldValue(name, Convert.ChangeType(value, originalValueType)!);
                    return;
                }
            }
            MainComponent.SetFieldValue(name, value!);
        }

        public override string ToString()
        {
            var componentName = MainComponent.Name;
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
