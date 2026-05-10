using System;
using IntelOrca.Biohazard.BioRand.REE;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    [Order(ModifierOrders.Item)]
    internal class ItemModifier : Modifier
    {
        public override void LogState(IReeRandomizerContext context, RandomizerLogger logger)
        {
            var randomizer = (ChainsawRandomizer)context;
            var itemData = ChainsawItemData.FromRandomizer(randomizer);
            foreach (var item in itemData.Definitions)
            {
                var itemDefinition = ItemDefinitionRepository.Default.Find(item._ItemId);
                if (itemDefinition == null)
                    continue;

                if (!IsStackable(itemDefinition))
                    continue;

                var data = IsWeapon(itemDefinition) ? item._WeaponDefineData : item._ItemDefineData;
                logger.LogLine($"{itemDefinition.Name}, stack = {data._StackMax}");
            }
        }

        public override void Apply(IReeRandomizerContext context, RandomizerLogger logger)
        {
            var randomizer = (ChainsawRandomizer)context;
            var itemData = ChainsawItemData.FromRandomizer(randomizer);
            foreach (var item in itemData.Definitions)
            {
                var itemDefinition = ItemDefinitionRepository.Default.Find(item._ItemId);
                if (itemDefinition == null)
                    continue;

                if (!IsStackable(itemDefinition))
                    continue;

                var optionName = $"inventory-stack-limit-{itemDefinition.DropKind}";
                var stackSize = Math.Clamp(randomizer.GetConfigOption<int>(optionName), 0, 9999);
                if (stackSize == 0)
                    continue;

                var data = IsWeapon(itemDefinition) ? item._WeaponDefineData : item._ItemDefineData;
                data._StackMax = stackSize;
            }
            itemData.Save();
        }

        private static bool IsStackable(ItemDefinition definition)
        {
            // Green herbs combine
            if (definition.Id == ItemIds.HerbG) return false;
            return
                definition.Kind == ItemKinds.Ammo ||
                definition.Kind == ItemKinds.Grenade ||
                definition.Kind == ItemKinds.Knife ||
                definition.Kind == ItemKinds.Gunpowder ||
                definition.Kind == ItemKinds.Resource ||
                definition.Kind == ItemKinds.Egg ||
                definition.Kind == ItemKinds.Fish ||
                definition.Kind == ItemKinds.Viper ||
                definition.Kind == ItemKinds.Health;
        }

        private static bool IsWeapon(ItemDefinition definition)
        {
            return
                definition.Kind == ItemKinds.Weapon ||
                definition.Kind == ItemKinds.Grenade ||
                definition.Kind == ItemKinds.Knife ||
                definition.Kind == ItemKinds.Egg;
        }
    }
}
