using System.Collections.Generic;

namespace IntelOrca.Biohazard.BioRand.RE4R.Modifiers
{
    internal class InventoryModifier : Modifier
    {
        private ChainsawPlayerInventory? _inventory;

        public override void LogState(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            _inventory ??= ChainsawPlayerInventory.FromData(randomizer.FileRepository);
            var inventory = _inventory;

            logger.LogLine($"PTAS = {inventory.PTAS}");
            logger.LogLine($"Spinels = {inventory.SpinelCount}");
        }

        public override void Apply(ChainsawRandomizer randomizer, RandomizerLogger logger)
        {
            _inventory ??= ChainsawPlayerInventory.FromData(randomizer.FileRepository);
            var inventory = _inventory;

            var itemRandomizer = randomizer.ItemRandomizer;
            if (!randomizer.GetConfigOption<bool>("random-inventory"))
            {
                itemRandomizer.MarkItemPlaced(ItemIds.SG09R);
                itemRandomizer.MarkItemPlaced(ItemIds.CombatKnife);
                return;
            }

            var itemData = ChainsawItemData.FromData(randomizer.FileRepository);
            var rng = randomizer.CreateRng();

            inventory.PTAS = rng.Next(0, 200) * 100;
            inventory.SpinelCount = rng.Next(0, 5);
            inventory.ClearItems();

            // Weapons
            var primaryWeaponKind = randomizer.GetConfigOption("inventory-weapon-primary", "handgun")!;
            var secondaryWeaponKind = randomizer.GetConfigOption("inventory-weapon-secondary", "random")!;
            var knifeWeapon = itemRandomizer.GetRandomWeapon(rng, ItemClasses.Knife, allowReoccurance: false);
            var primaryWeapon = itemRandomizer.GetRandomWeapon(rng, primaryWeaponKind, allowReoccurance: false);
            var secondaryWeapon = itemRandomizer.GetRandomWeapon(rng, secondaryWeaponKind, allowReoccurance: false);
            if (knifeWeapon != null)
            {
                inventory.AddItem(new Item(knifeWeapon.Id));
            }
            if (primaryWeapon != null)
            {
                inventory.AddItem(new Item(primaryWeapon.Id));
                var ammo = itemRandomizer.GetRandomItem(rng, ItemKinds.Ammo, primaryWeapon.Class);
                if (ammo != null)
                    inventory.AddItem(new Item(ammo.Id));
            }
            if (secondaryWeapon != null)
            {
                inventory.AddItem(new Item(secondaryWeapon.Id));
                var ammo = itemRandomizer.GetRandomItem(rng, ItemKinds.Ammo, secondaryWeapon.Class);
                if (ammo != null)
                    inventory.AddItem(new Item(ammo.Id));
            }

            // Other stuff
            var randomKinds = new[] {
                ItemKinds.Fish,
                ItemKinds.Health,
                ItemKinds.Egg,
                ItemKinds.Grenade,
                ItemKinds.Knife,
                ItemKinds.Gunpowder,
                ItemKinds.Resource };

            var kinds = new List<string>();
            for (var i = 0; i < 10; i++)
            {
                kinds.Add(rng.Next(randomKinds));
            }

            foreach (var kind in kinds)
            {
                var randomItem = itemRandomizer.GetRandomItem(rng, kind);
                if (randomItem != null)
                    inventory.AddItem(new Item(randomItem.Id, -1));
            }

            inventory.UpdateWeapons(itemData);
            inventory.AutoSort(itemData);
            inventory.Save(randomizer.FileRepository);

            foreach (var item in inventory.Data[0].InventoryItems)
            {
                logger.LogLine($"Add item {item.Item} {item.Item.CurrentItemCount}");
            }
        }
    }
}
