using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class ItemDefinitionRepository
    {
        private static ItemDefinitionRepository? _default;

        public ItemDefinition[] Items { get; set; } = new ItemDefinition[0];

        public ImmutableArray<string> Kinds { get; private set; }
        public ImmutableDictionary<string, ImmutableArray<ItemDefinition>> KindToItemMap { get; private set; } =
            ImmutableDictionary<string, ImmutableArray<ItemDefinition>>.Empty;
        public ImmutableDictionary<int, ItemDefinition> IdToItemMap { get; private set; } =
            ImmutableDictionary<int, ItemDefinition>.Empty;

        public static ItemDefinitionRepository Default
        {
            get
            {
                if (_default == null)
                {
                    _default ??= Resources.items.DeserializeJson<ItemDefinitionRepository>();
                    _default.Initialize();
                }
                return _default;
            }
        }

        private void Initialize()
        {
            var releventItems = Items
                .Where(x => !string.IsNullOrEmpty(x.Kind))
                .Where(x => string.IsNullOrEmpty(x.Mode))
                .ToArray();

            Kinds = releventItems
                .Select(x => x.Kind!)
                .Distinct()
                .ToImmutableArray();
            KindToItemMap = releventItems
                .GroupBy(x => x.Kind!)
                .ToImmutableDictionary(x => x.Key, x => x.ToImmutableArray());
            IdToItemMap = Items.ToImmutableDictionary(x => x.Id);
        }

        public ItemDefinition? Find(int id)
        {
            IdToItemMap.TryGetValue(id, out var item);
            return item;
        }
    }

    public static class ItemKinds
    {
        public const string Ammo = "ammo";
        public const string Fish = "fish";
        public const string Health = "health";
        public const string Treasure = "treasure";
        public const string Attachment = "attachment";
        public const string Gunpowder = "gunpowder";
        public const string Weapon = "weapon";
        public const string Knife = "weapon";
        public const string Key = "key";
        public const string Token = "token";
        public const string Special = "special";
        public const string Money = "money";
        public const string Armor = "armor";
        public const string Map = "map";
        public const string CaseSize = "case-size";
        public const string CasePerk = "case-perk";
        public const string Recipe = "recipe";
        public const string Charm = "charm";
        public const string Grenade = "grenade";
    }
}
