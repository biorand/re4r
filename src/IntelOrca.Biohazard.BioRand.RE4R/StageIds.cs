using System.Collections.Immutable;
using System.Linq;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public static class StageIds
    {
        private static ImmutableArray<StageDefinition> _stages;

        public static ImmutableArray<int> All { get; } = [
            40100,
            40101,
            40102,
            40110,
            40200,
            40210,
            40211,
            40212,
            40500,
            40504,
            40505,
            40510,
            43300,
            43301,
            43302,
            43303,
            43310,
            43311,
            43400,
            43410,
            43411,
            44100,
            44101,
            44110,
            44201,
            44210,
            44300,
            44400,
            45200,
            45210,
            45300,
            45301,
            45302,
            45400,
            45401,
            46100,
            46200,
            47101,
            47102,
            47200,
            47201,
            47300,
            47301,
            47401,
            47410,
            47900,
            50100,
            50201,
            50202,
            50300,
            50301,
            50400,
            50401,
            50503,
            50601,
            51101,
            51200,
            51201,
            51402,
            51403,
            51404,
            51502,
            51503,
            51504,
            51600,
            51610,
            51611,
            51612,
            52101,
            52200,
            52202,
            52303,
            53200,
            53302,
            53303,
            54101,
            54102,
            54200,
            54203,
            54300,
            54400,
            54404,
            55102,
            55103,
            55200,
            55202,
            55300,
            55301,
            55302,
            56100,
            56101,
            56102,
            56103,
            56104,
            56200,
            56300,
            56301,
            59102,
            59103,
            59200,
            60102,
            60103,
            60104,
            60105,
            60110,
            60200,
            61102,
            61104,
            61106,
            61201,
            61203,
            61204,
            61302,
            61305,
            61402,
            61500,
            61502,
            61503,
            61504,
            61505,
            62000,
            62100,
            62200,
            63100,
            63102,
            63103,
            63106,
            63107,
            63108,
            63109,
            64103,
            64104,
            64200,
            65100,
            66101,
            66102,
            66103,
            66105,
            66200,
            67101,
            67102,
            67104,
            67200,
            67301,
            67400,
            67500,
            67501,
            68000,
            68203,
            68206,
            68300,
            69100,
            69900,
            78900,
        ];

        public static ImmutableArray<StageDefinition> Stages
        {
            get
            {
                if (_stages != null)
                    return _stages;

                var def = Resources.stages.DeserializeJson<StagesDefinition>();
                _stages = def.Stages.ToImmutableArray();
                return _stages;
            }
        }

        public static StageDefinition? FromId(int id)
        {
            return Stages.FirstOrDefault(x => x.Stage == id);
        }

        public class StagesDefinition
        {
            public StageDefinition[] Stages { get; set; } = [];
        }

        public class StageDefinition
        {
            public int Chapter { get; set; }
            public int Stage { get; set; }
            public string Name { get; set; } = "";
        }
    }
}
