using System.Threading.Tasks;

namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public class BioRandService(
        DatabaseService db)
    {
        public async Task Initialize()
        {
            await db.SetAllRandoStatusToExpiredAsync();
        }
    }
}
