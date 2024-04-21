using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class WeaponDistributor(ChainsawRandomizer randomizer)
    {
        private readonly Dictionary<int, List<ItemDefinition>> _weapons = [];
        private readonly HashSet<ItemDefinition> _findable = [];

        public ImmutableArray<int> Chapters => [.. _weapons.Keys.Order()];

        public void Setup(ItemRandomizer itemRandomizer, Rng rng, RandomizerLogger logger)
        {
            var itemRepo = ItemDefinitionRepository.Default;
            var allWeapons = itemRepo.GetAll(ItemKinds.Weapon);

            // Start
            var primaryWeaponKind = randomizer.GetConfigOption("inventory-weapon-primary", "handgun")!;
            var secondaryWeaponKind = randomizer.GetConfigOption("inventory-weapon-secondary", "random")!;
            var primaryWeaponDefinition = itemRandomizer.GetRandomWeapon(rng, primaryWeaponKind, allowReoccurance: false);
            var secondaryWeaponDefinition = itemRandomizer.GetRandomWeapon(rng, secondaryWeaponKind, allowReoccurance: false);
            AddWeapon(0, primaryWeaponDefinition);
            AddWeapon(0, secondaryWeaponDefinition);
            AddWeapon(0, itemRandomizer.GetRandomWeapon(rng, ItemClasses.Knife, allowReoccurance: false));
            foreach (var weapon in new ItemDefinition?[] { primaryWeaponDefinition, secondaryWeaponDefinition })
            {
                if (weapon != null && rng.NextProbability(50))
                {
                    var attachment = itemRandomizer.GetRandomAttachment(rng, weapon, allowReoccurance: false);
                    if (attachment != null)
                    {
                        AddWeapon(0, attachment);
                        break;
                    }
                }
            }

            // Chapters
            var endlessBag = new EndlessBag<int>(rng, Enumerable.Range(1, 13));
            while (true)
            {
                var weapon = itemRandomizer.GetRandomWeapon(rng, allowReoccurance: false);
                if (weapon == null)
                    break;

                var chapter = endlessBag.Next();
                AddWeapon(chapter, weapon);
            }
            while (true)
            {
                var attachment = itemRandomizer.GetRandomAttachment(rng, allowReoccurance: false);
                if (attachment == null)
                    break;

                var chapter = attachment.Id == ItemIds.BiosensorScope
                    ? rng.Next(1, 5) // Ensure we get biosensor scope before cabin
                    : endlessBag.Next();
                AddWeapon(chapter, attachment);
            }

            var placedWeapons = _weapons
                .Where(x => x.Key != 0)
                .SelectMany(x => x.Value)
                .ToArray();
            var findable = placedWeapons
                .Shuffle(rng)
                .Take(placedWeapons.Length / 2)
                .ToArray();
            foreach (var weapon in findable)
                _findable.Add(weapon);

            LogDistribution(logger);
        }

        public ImmutableArray<ItemDefinition> GetStartingWeapons()
        {
            _weapons.TryGetValue(0, out var list);
            return list?.ToImmutableArray() ?? [];
        }

        public ImmutableArray<ItemDefinition> GetWeaponsForDrop(int chapter)
        {
            _weapons.TryGetValue(chapter, out var list);
            return list?
                .Where(_findable.Contains)
                .ToImmutableArray() ?? [];
        }

        public ImmutableDictionary<int, ImmutableArray<ItemDefinition>> GetWeaponsForShop()
        {
            var result = ImmutableDictionary.CreateBuilder<int, ImmutableArray<ItemDefinition>>();
            foreach (var chapter in Chapters)
            {
                result[chapter] = GetWeaponsForShop(chapter);
            }
            return result.ToImmutable();
        }

        public ImmutableArray<ItemDefinition> GetWeaponsForShop(int chapter)
        {
            var builder = ImmutableArray.CreateBuilder<ItemDefinition>();

            // Add non-findable weapons
            if (_weapons.TryGetValue(chapter, out var list))
            {
                builder.AddRange(list.Where(x => !_findable.Contains(x)));
            }

            // Add weapons that were findable last chapter
            if (chapter > 1)
            {
                if (_weapons.TryGetValue(chapter - 1, out list))
                {
                    builder.AddRange(list.Where(_findable.Contains));
                }
            }

            return builder.ToImmutable();
        }

        private void LogDistribution(RandomizerLogger logger)
        {
            logger.Push("Weapon Distribution");
            var chapters = _weapons.Keys.Order().ToArray();
            foreach (var chapter in chapters)
            {
                logger.Push($"Chapter {chapter}");
                var list = _weapons[chapter];
                foreach (var weapon in list)
                {
                    var findable = _findable.Contains(weapon) ? ", findable" : "";
                    logger.LogLine($"{weapon}{findable}");
                }
                logger.Pop();
            }
            logger.Pop();
        }

        private void AddWeapon(int chapter, ItemDefinition? definition)
        {
            if (definition == null)
                return;

            if (!_weapons.TryGetValue(chapter, out var list))
                _weapons[chapter] = list = new();

            list.Add(definition);
        }

        private static readonly string[] _weaponClasses = [
            ItemClasses.None,
            ItemClasses.Handgun,
            ItemClasses.Shotgun,
            ItemClasses.Rifle,
            ItemClasses.Smg,
            ItemClasses.Magnum ];
    }
}
