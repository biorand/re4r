namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public class WeaponDefinition(string key, int id, bool ranged)
    {
        public string Key { get; } = key;
        public int Id { get; } = id;
        public bool Ranged { get; } = ranged;

        public override string ToString() => Key;
    }
}
