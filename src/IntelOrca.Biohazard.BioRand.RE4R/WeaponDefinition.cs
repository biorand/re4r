namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class WeaponDefinition(string key, int id)
    {
        public string Key { get; } = key;
        public int Id { get; } = id;

        public override string ToString() => Key;
    }
}
