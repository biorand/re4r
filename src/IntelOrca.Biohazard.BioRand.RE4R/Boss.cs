using System;
using System.Collections.Immutable;
using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class Boss(string key, string name, Guid guid)
    {
        public string Key => key;
        public string Name => name;
        public Guid Guid => guid;
    }

    public static class Bosses
    {
        public static ImmutableArray<Boss> Leon { get; } = [
            new Boss("dellago", "Del Lago", new Guid("ae0f964e-c6c1-456c-8eaa-a2e3ee6dac42")),
            new Boss("elgigante-1", "El Gigante (Village)", new Guid("484ec7c2-ded4-49d7-819b-23f6e23a208a")),
            new Boss("mendez-1", "Mendez (Phase 1)", new Guid("a61c62f3-52e7-4d78-a167-c99b84fcada9")),
            new Boss("mendez-2", "Mendez (Phase 2)", new Guid("aa862ced-d677-42bd-8542-fbbbe038a16f")),
            new Boss("verdugo", "Verdugo", new Guid("902fe2d5-0816-4d48-845b-52efd751dec1")),
            new Boss("elgigante-2", "El Gigante (Lava A)", new Guid("222771c0-628e-4aab-b8a7-dd7d1cff9c6d")),
            new Boss("elgigante-3", "El Gigante (Lava B)", new Guid("55d16590-73c2-4335-8a9b-3b4970ab1103")),
            new Boss("krauser-1", "Krauser (Knife)", new Guid("6c68dd30-5387-4fba-b05e-e4846570025a")),
            new Boss("salazar-1", "Salazar", new Guid("0c88b4af-61c0-4024-a996-8d0ee41514a0")),
            new Boss("krauser-2", "Krauser Fight 1", new Guid("d26180c8-7b97-4d75-aea0-c497a60fb779")),
            new Boss("krauser-3", "Krauser Fight 2", new Guid("e510bf60-81a9-46be-b285-cc168cbb3a45")),
            new Boss("krauser-4", "Krauser Fight 3", new Guid("a241d88c-1eea-48c2-8261-3f31716e4dfa")),
            new Boss("sadler", "Sadler", new Guid("cacdb0fa-b472-4567-9dbd-8cb1cafa9c57")),
        ];

        public static ImmutableArray<Boss> Ada { get; } = [
            new Boss("pesanta-1", "Pesanta (Ch. 1)", new Guid("33bda65f-fa3a-4385-a2c7-92786d5f0b6f")),
            new Boss("pesanta-2", "Pesanta (Ch. 2)", new Guid("2b7268ca-f11a-4143-9969-cb500b4b7301")),
            new Boss("elgigante-4", "El Gigante (Farm)", new Guid("afac38f0-0c3c-4e19-8058-fed0d7105fd3")),
            new Boss("u3-1", "U3 (Phase 1)", new Guid("56ca7658-5434-4d3b-8622-9a9891e1daa1")),
            new Boss("u3-2", "U3 (Phase 2)", new Guid("aa862ced-d677-42bd-8542-fbbbe038a16f")),
            new Boss("sadler-2", "Sadler (Human)", new Guid("71c38980-cf1a-4040-aa93-708a290529ee")),
        ];

        public static ImmutableArray<Boss> All { get; } = [.. Leon, .. Ada];

        public static bool IsBoss(Guid guid) => GetBoss(guid) != null;
        public static Boss? GetBoss(Guid guid) => All.FirstOrDefault(x => x.Guid == guid);
    }
}
