namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class EnemyKindDefinition(string key, string componentName, string prefab, bool closed, bool noItemDrop)
    {
        public string Key => key;
        public string ComponentName => componentName;
        public string Prefab => prefab;
        public bool Closed => closed;
        public bool NoItemDrop => noItemDrop;

        public override string ToString() => Key;
    }
}
