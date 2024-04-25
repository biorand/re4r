using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Services
{
    internal sealed class DatabaseService
    {
        private readonly SQLiteAsyncConnection _conn;

        private DatabaseService(SQLiteAsyncConnection conn)
        {
            _conn = conn;
        }

        public static async Task<DatabaseService> CreateDefault()
        {
            var config = Re4rConfiguration.GetDefault();
            var databaseConfig = config.Database;
            var databasePath = (databaseConfig?.Path) ?? throw new Exception();
            var db = new SQLiteAsyncConnection(databasePath);
            var instance = new DatabaseService(db);
            await instance.CreateTables();
            return instance;
        }

        public async Task CreateTables()
        {
            await _conn.CreateTableAsync<UserDbModel>();
            await _conn.CreateTableAsync<TokenDbModel>();
        }

        public async Task<UserDbModel> GetUserByEmail(string email)
        {
            var emailLower = email.ToLowerInvariant();
            return await _conn.Table<UserDbModel>()
                .Where(x => x.Email == emailLower)
                .FirstOrDefaultAsync();
        }

        public async Task<UserDbModel> GetUserByName(string name)
        {
            var nameLower = name.ToLowerInvariant();
            return await _conn.Table<UserDbModel>()
                .Where(x => x.NameLowerCase == nameLower)
                .FirstOrDefaultAsync();
        }

        public async Task<UserDbModel> CreateUserAsync(string email, string name)
        {
            var user = new UserDbModel()
            {
                Created = DateTime.UtcNow,
                Name = name,
                NameLowerCase = name.ToLowerInvariant(),
                Email = email.ToLowerInvariant(),
                Role = UserRoleKind.Pending,
            };
            await _conn.InsertAsync(user);
            return user;
        }

        public async Task<TokenDbModel> CreateTokenAsync(UserDbModel user)
        {
            var code = RandomNumberGenerator.GetInt32(1000000);
            var token = RandomNumberGenerator.GetHexString(32, lowercase: true);
            var result = new TokenDbModel()
            {
                Created = DateTime.UtcNow,
                UserId = user.Id,
                Code = code,
                Token = token
            };
            await _conn.InsertAsync(result);
            return result;
        }

        public async Task<TokenDbModel?> GetTokenAsync(UserDbModel user, int code)
        {
            return await _conn.Table<TokenDbModel>()
                .Where(x => x.UserId == user.Id && x.Code == code)
                .FirstOrDefaultAsync();
        }

        public async Task UseTokenAsync(string token)
        {
            await _conn.ExecuteAsync("UPDATE token SET LastUsed = ? WHERE token = ?",
                DateTime.UtcNow,
                token);
        }
    }
}
