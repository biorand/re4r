using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Services;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class InventoryModifier : Modifier
    {
        private ChainsawPlayerInventory? _inventory;

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            _inventory ??= ChainsawPlayerInventory.FromData(randomizer.FileRepository, randomizer.Campaign);
            var inventory = _inventory;

            logger.LogLine($"PTAS = {inventory.PTAS}");
            logger.LogLine($"Spinels = {inventory.SpinelCount}");

            var itemsById = inventory.PlayerData.InventoryData.InventoryItems
                .GroupBy(x => x.Item._ItemId)
                .OrderBy(x => x.Key)
                .ToArray();
            foreach (var itemGroup in itemsById)
            {
                var item = itemGroup.First();
                var count = itemGroup.Sum(x => x.Item._CurrentItemCount);
                var itemName = ItemDefinitionRepository.Default.GetName(item.Item._ItemId);
                logger.LogLine($"{itemName} {count}");
            }
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            _inventory ??= ChainsawPlayerInventory.FromData(randomizer.FileRepository, randomizer.Campaign);
            var inventory = _inventory;

            var itemRandomizer = randomizer.ItemRandomizer;
            if (!randomizer.GetConfigOption<bool>("random-inventory"))
            {
                itemRandomizer.MarkItemPlaced(ItemIds.SG09R);
                itemRandomizer.MarkItemPlaced(ItemIds.CombatKnife);
                return;
            }

            var itemData = ChainsawItemData.FromRandomizer(randomizer);
            var rng = randomizer.CreateRng();

            inventory.PTAS = rng.Next(0, 200) * 100;
            inventory.SpinelCount = rng.Next(0, 5);
            inventory.ClearItems();

            // Weapons
            var weapons = randomizer.ValuableDistributor.GetItems(ItemDiscovery.Start);
            var addedClasses = new System.Collections.Generic.HashSet<string>();
            foreach (var weapon in weapons)
            {
                var weaponClass = weapon.Definition.Class ?? string.Empty;
                // Skip if we've already added a weapon of this class
                if (addedClasses.Contains(weaponClass))
                    continue;

                addedClasses.Add(weaponClass);
                inventory.AddItem(new Item(weapon.Definition.Id));
                var ammo = itemRandomizer.GetRandomItemDefinition(rng, ItemKinds.Ammo, weapon.Definition.Class);
                if (ammo != null)
                {
                    inventory.AddItem(new Item(ammo.Id));
                }
            }

            AddRandomStuff(randomizer, inventory, rng);
            inventory.UpdateWeapons(itemData);
            inventory.AutoSort(itemData);
            inventory.AssignShortcuts();
            inventory.Save(randomizer.FileRepository);

            foreach (var item in inventory.PlayerData.InventoryData.InventoryItems)
            {
                var size = itemData.GetSize(item.Item._ItemId);
                var width = size.Width;
                var height = size.Height;
                if (item.CurrDirection != 0)
                {
                    (width, height) = (height, width);
                }
                logger.LogLine($"Add item {item.Item} ({item.STRUCT_SlotIndex_Column}, {item.STRUCT_SlotIndex_Row}) ({width}x{height}) Rotation = {item.CurrDirection}");
            }
        }

        private void AddRandomStuff(ChainsawRandomizer randomizer, ChainsawPlayerInventory inventory, Rng rng, int count = 10)
        {
            var randomKinds = new[] {
                ItemKinds.Fish,
                ItemKinds.Fish,
                ItemKinds.Viper,
                ItemKinds.Viper,
                ItemKinds.Health,
                ItemKinds.Health,
                ItemKinds.Egg,
                ItemKinds.Egg,
                ItemKinds.Grenade,
                ItemKinds.Grenade,
                ItemKinds.Knife,
                ItemKinds.Knife,
                ItemKinds.Gunpowder,
                ItemKinds.Gunpowder,
                ItemKinds.Resource,
                ItemKinds.Resource
            };

            var itemRandomizer = randomizer.ItemRandomizer;
            var bag = new EndlessBag<string>(rng, randomKinds);
            for (var i = 0; i < count; i++)
            {
                var kind = bag.Next();
                var randomItem = itemRandomizer.GetRandomItemDefinition(rng, kind);
                if (randomItem != null)
                    inventory.AddItem(new Item(randomItem.Id, -1));
            }
        }
    }
}
