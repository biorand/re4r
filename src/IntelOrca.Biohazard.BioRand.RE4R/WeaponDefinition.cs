namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class WeaponDefinition(string name, int id)
    {
        public string Name { get; } = name;
        public int Id { get; } = id;

        public override string ToString() => Name;
    }
}
