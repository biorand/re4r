using System;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class ItemModifier : Modifier
    {
        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
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

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            var stackMultiplier = randomizer.GetConfigOption<double>("inventory-stack-multiplier", 1);
            if (stackMultiplier == 1)
                return;

            var itemData = ChainsawItemData.FromRandomizer(randomizer);
            foreach (var item in itemData.Definitions)
            {
                var itemDefinition = ItemDefinitionRepository.Default.Find(item._ItemId);
                if (itemDefinition == null)
                    continue;

                if (!IsStackable(itemDefinition))
                    continue;

                var data = IsWeapon(itemDefinition) ? item._WeaponDefineData : item._ItemDefineData;
                data._StackMax = Math.Clamp((int)Math.Round(data._StackMax * stackMultiplier), 1, 999);
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
