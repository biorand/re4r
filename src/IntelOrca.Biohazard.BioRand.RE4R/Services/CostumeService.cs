using System.Linq;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
    internal class CostumeService(ChainsawRandomizer randomizer)
    {
        public void Add(string key, string fieldName, object additional)
        {
            var enemyClassFactory = randomizer.GetService<EnemyClassFactory>();
            var enemyClass = enemyClassFactory.Classes.First(x => x.Key == key);
            var enemyFieldIndex = enemyClass.Fields.FindIndex(x => x.Name == fieldName);
            if (enemyFieldIndex != -1)
            {
                var original = enemyClass.Fields[enemyFieldIndex];
                enemyClass.Fields[enemyFieldIndex] = new EnemyFieldDefinition(fieldName, original.Values.Add(additional));
            }
            else
            {
                enemyClass.Fields.Add(new EnemyFieldDefinition(fieldName, [additional]));
            }
        }
    }
}
