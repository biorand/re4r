﻿using System.Collections.Generic;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class ItemRandomizer
    {
        private readonly HashSet<int> _placedItemIds = new HashSet<int>();
        private readonly bool _allowBonusItems;

        public int[] PlacedItemIds => _placedItemIds.ToArray();
        public ItemDefinition[] PlacedItems => _placedItemIds
            .Select(x => ItemDefinitionRepository.Default.Find(x)!)
            .ToArray();

        public ItemRandomizer(bool allowBonusItems)
        {
            _allowBonusItems = allowBonusItems;
            _placedItemIds.Add(ItemIds.RecipeHandgunAmmo);
            _placedItemIds.Add(ItemIds.RecipeShotgunAmmo);
            _placedItemIds.Add(ItemIds.RecipeHerbGG);
            _placedItemIds.Add(ItemIds.RecipeHerbGR);
            _placedItemIds.Add(ItemIds.RecipeHerbGY);
            _placedItemIds.Add(ItemIds.RecipeHerbRY);
            _placedItemIds.Add(ItemIds.RecipeHerbGGG);
            _placedItemIds.Add(ItemIds.RecipeHerbGGY1);
            _placedItemIds.Add(ItemIds.RecipeHerbGGY2);
            _placedItemIds.Add(ItemIds.RecipeHerbGRY1);
            _placedItemIds.Add(ItemIds.RecipeHerbGRY2);
            _placedItemIds.Add(ItemIds.RecipeHerbGRY3);
            _placedItemIds.Add(ItemIds.RecipeHerbGRY3);
            _placedItemIds.Add(ItemIds.Case7x10);
        }

        public ItemDefinition? GetRandomWeapon(Rng rng, string? classification = null, bool allowReoccurance = true)
        {
            var weaponKinds = new[] {
                ItemClasses.None, ItemClasses.Handgun, ItemClasses.Shotgun,
                ItemClasses.Rifle, ItemClasses.Smg, ItemClasses.Magnum };

            if (classification == ItemClasses.None)
                return null;

            if (classification == ItemClasses.Random)
                classification = rng.Next(weaponKinds);

            return GetRandomItem(rng, ItemKinds.Weapon, classification, allowReoccurance);
        }

        public ItemDefinition? GetRandomAttachment(Rng rng, string? classification = null, bool allowReoccurance = true)
        {
            return GetRandomItem(rng, ItemKinds.Attachment, classification, allowReoccurance);
        }

        public ItemDefinition? GetRandomItem(Rng rng, string kind, string? classification = null, bool allowReoccurance = true)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var poolEnumerable = itemRepo
                .GetAll(kind, classification)
                .Where(x => _allowBonusItems || !(x.Bonus ?? false));
            if (!allowReoccurance)
            {
                poolEnumerable = poolEnumerable
                    .Where(x => !_placedItemIds.Contains(x.Id));

            }
            var pool = poolEnumerable.ToArray();
            if (pool.Length == 0)
                return null;

            var chosen = rng.Next(pool);
            _placedItemIds.Add(chosen.Id);
            return chosen;
        }

        public void MarkItemPlaced(int id)
        {
            _placedItemIds.Add(id);
        }

        public bool IsItemPlaced(int id) => _placedItemIds.Contains(id);
    }
}
