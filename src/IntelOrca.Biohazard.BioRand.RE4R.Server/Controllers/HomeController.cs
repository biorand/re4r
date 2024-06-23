using System;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Microsoft.AspNetCore.Mvc;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    [ApiController]
    [Route("home")]
    public class HomeController(DatabaseService db)
    {
        [HttpGet("news")]
        public object GetNewsAsync()
        {
            return new
            {
                Items = new[]
                {
                    new
                    {
                        Date = new DateTime(2024, 6, 20).ToString("d MMMM"),
                        Timestamp = new DateTime(2024, 6, 20).ToUnixTimeSeconds(),
                        Title = "Improved weapons",
                        Body = "<p>Weapons and exclusives have been improved.<ul><li>Random weapon exclusive values</li><li>Weapon shortcuts now automatically applied</li></ul></p>"
                    },
                    new
                    {
                        Date = new DateTime(2024, 6, 20).ToString("d MMMM"),
                        Timestamp = new DateTime(2024, 6, 18).ToUnixTimeSeconds(),
                        Title = "Another",
                        Body = "<p>Weapons and exclusives have been improved.<ul><li>Random weapon exclusive values</li><li>Weapon shortcuts now automatically applied</li></ul></p>"
                    }
                }
            };
        }

        [HttpGet("stats")]
        public async Task<object> GetStatsAsync()
        {
            var seeds = await db.GetSeedsDaily();
            var totalUsers = await db.GetTotalUsersDaily();
            return new
            {
                Seeds = seeds,
                TotalUsers = totalUsers
            };
        }
    }
}
