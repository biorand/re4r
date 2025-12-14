using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Modifiers;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class EnemyService
    {
        private Dictionary<Guid, EnemyPlacement> _guidToEnemyPlacements;

        public List<EnemyPlacement> EnemyPlacements { get; private set; }

        public EnemyService(DynamicData dynamicData)
        {
            var data = dynamicData.GetData(DynamicDataName.Enemies) ?? throw new Exception("Unable to get enemy data");
            EnemyPlacements = Csv.Deserialize<EnemyPlacement>(data)
                .Where(x => x.Chapter != 0)
                .ToList();

            _guidToEnemyPlacements = EnemyPlacements.ToDictionary(x => x.GuidOrAuto);
        }

        public void Remove(IEnumerable<EnemyPlacement> placements)
        {
            EnemyPlacements = EnemyPlacements.Except(placements).ToList();
            _guidToEnemyPlacements = EnemyPlacements.ToDictionary(x => x.GuidOrAuto);
        }

        public EnemyPlacement? Find(Guid guid)
        {
            _guidToEnemyPlacements.TryGetValue(guid, out var result);
            return result;
        }

        public EnemyPlacement Duplicate(EnemyPlacement enemyPlacement, Guid guid)
        {
            var result = enemyPlacement.Duplicate(guid);
            EnemyPlacements.Add(result);
            _guidToEnemyPlacements.Add(result.Guid, enemyPlacement);
            return result;
        }
    }

    [DebuggerDisplay("{GuidOrAuto}")]
    internal class EnemyPlacement
    {
        // Set from CSV:
        [Key]
        public int Row { get; set; }
        public Campaign Campaign { get; set; }
        public Guid Guid { get; set; }
        public int Chapter { get; set; }
        public string Description { get; set; } = "";
        public int Stage { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }
        public string Condition { get; set; } = "";
        public string SkipCondition { get; set; } = "";
        public string MiniBoss { get; set; } = "";
        public string Battle { get; set; } = "";
        public ImmutableArray<string> Tags { get; set; } = [];
        public ImmutableArray<string> Include { get; set; } = [];
        public ImmutableArray<string> Exclude { get; set; } = [];

        // Not set from CSV:
        public Guid DeathFlag { get; set; }

        public bool HasEmptyPosition => X == 0 && Y == 0 && Z == 0;
        public bool HasEmptyRotation => Yaw == 0 && Pitch == 0 && Roll == 0;
        public Vector3 Position => new Vector3(X, Y, Z);
        public EulerAngles Rotation => new EulerAngles(Yaw, Pitch, Roll);
        public bool IsExtra => Guid == default || Description.StartsWith("[EXTRA]");
        public Guid GuidOrAuto => Guid != default ? Guid : $"Enemy_{Row}".GetGuidHash();
        public bool HasTag(string tag) => Tags.Contains(tag);

        public int Location => Stage / 1000;

        public EnemyPlacement Duplicate(Guid guid)
        {
            return new EnemyPlacement()
            {
                Campaign = Campaign,
                Guid = guid,
                Chapter = Chapter,
                Description = Description,
                Stage = Stage,
                X = X,
                Y = Y,
                Z = Z,
                Yaw = Yaw,
                Pitch = Pitch,
                Roll = Roll,
                Condition = Condition,
                SkipCondition = SkipCondition,
                MiniBoss = MiniBoss,
                Tags = Tags,
                Include = Include,
                Exclude = Exclude
            };
        }
    }

    internal static class EnemyTags
    {
        /// <summary>
        /// Keep enemy the same kind.
        /// </summary>
        public const string Preserve = "preserve";

        /// <summary>
        /// Do not multiply the enemy.
        /// </summary>
        public const string NoDuplicate = "noduplicate";

        /// <summary>
        /// Do not create a wave for this enemy.
        /// </summary>
        public const string NoWave = "nowave";

        /// <summary>
        /// Make the enemy find the player immediately.
        /// </summary>
        public const string Aggroed = "aggroed";

        /// <summary>
        /// If the enemy kind is the same, keep the weapon the same.
        /// </summary>
        public const string LockWeapon = "lockweapon";

        /// <summary>
        /// Prevent enemy carrying important items.
        /// </summary>
        public const string Horde = "horde";

        /// <summary>
        /// Prevent the enemy from plaga-ing.
        /// </summary>
        public const string NoPlaga = "noplaga";

        /// <summary>
        /// Make enemy non-chapter specific. Active for all chapters.
        /// </summary>
        public const string AnyChapter = "anychapter";

        /// <summary>
        /// Prevent enemy from being a toxic enemy when prevent toxic Mendez hill is enabled.
        /// </summary>
        public const string NoToxic = "notoxic";

        /// <summary>
        /// Try to make enemy one that attacks from a distance. E.g. crossbow, dynamite, JJ, RPG etc.
        /// </summary>
        public const string Ranged = "ranged";

        /// <summary>
        /// Try to make enemy one that is unlikely to easily kill Ashley.
        /// </summary>
        public const string AshleySafe = "ashleysafe";

        /// <summary>
        /// Killing the enemy is required to progress.
        /// </summary>
        public const string Guardian = "guardian";
    }
}
