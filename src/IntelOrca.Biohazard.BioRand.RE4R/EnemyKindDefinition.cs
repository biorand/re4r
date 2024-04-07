namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class EnemyKindDefinition(string name, string componentName)
    {
        public string Name { get; set; } = name;
        public string ComponentName { get; } = componentName;

        public override string ToString() => Name;
    }
}
