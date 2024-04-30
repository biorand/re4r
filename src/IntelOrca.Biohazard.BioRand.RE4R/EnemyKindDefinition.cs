namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class EnemyKindDefinition(string key, string componentName, string prefab, bool closed)
    {
        public string Key { get; set; } = key;
        public string ComponentName { get; } = componentName;
        public string Prefab { get; } = prefab;
        public bool Closed { get; } = closed;

        public override string ToString() => Key;
    }
}
