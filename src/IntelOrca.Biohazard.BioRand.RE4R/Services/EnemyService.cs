using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Modifiers;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class EnemyService
    {
        private int _contextIdGroup;
        private int _contextIdIndex;
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

        public ContextId GetNextContextId()
        {
            return new ContextId(0, 0, _contextIdGroup, _contextIdIndex++);
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
        private ImmutableArray<string> _tags = [];

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
        public string Tags
        {
            get => string.Join(" ", _tags);
            set => _tags = value.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
        }
        public string Include { get; set; } = "";
        public string Exclude { get; set; } = "";

        public bool HasEmptyRotation => Yaw == 0 && Pitch == 0 && Roll == 0;
        public Vector3 Position => new Vector3(X, Y, Z);
        public EulerAngles Rotation => new EulerAngles(Yaw, Pitch, Roll);
        public bool IsExtra => Guid == default || Description.StartsWith("[EXTRA]");
        public Guid GuidOrAuto => Guid != default ? Guid : string.Concat(Campaign, Stage, X, Y, Z, Condition, SkipCondition).GetGuidHash();
        public bool HasTag(string tag) => _tags.Contains(tag);
        public ImmutableArray<string> TagsAsArray
        {
            get => _tags;
            set => _tags = value;
        }
        public ImmutableArray<string> IncludeAsArray => Include.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();
        public ImmutableArray<string> ExcludeAsArray => Exclude.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToImmutableArray();

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
        public const string Preserve = "preserve";
        public const string NoDuplicate = "noduplicate";
        public const string Aggroed = "aggroed";
        public const string Small = "small";
        public const string LockWeapon = "lockweapon";
        public const string Horde = "horde";
        public const string NoPlaga = "noplaga";
    }
}
