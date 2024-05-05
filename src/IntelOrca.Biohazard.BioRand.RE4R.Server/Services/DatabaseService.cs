using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Services
{
    internal sealed class DatabaseService
    {
        private readonly SQLiteAsyncConnection _conn;

        public int SystemUserId => 1;

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
            await _conn.CreateTableAsync<ProfileDbModel>();
            await _conn.CreateTableAsync<RandoDbModel>();
            await _conn.CreateTableAsync<RandoConfigDbModel>();
            await _conn.ExecuteAsync(
                @"CREATE TABLE IF NOT EXISTS ""profile_star"" (
                    ""ProfileId"" integer not null,
                    ""UserId"" integer not null,
                    PRIMARY KEY (""ProfileId"", ""UserId""))");
            await GetOrCreateSystemUser();
        }

        private async Task<UserDbModel> GetOrCreateSystemUser()
        {
            var systemUser = await GetUserById(1);
            if (systemUser == null)
            {
                systemUser = new UserDbModel
                {
                    Id = 1,
                    Created = DateTime.UtcNow,
                    Name = "System",
                    NameLowerCase = "system",
                    Email = "",
                    Role = UserRoleKind.System
                };
                await _conn.InsertOrReplaceAsync(systemUser);
            }
            return systemUser;
        }

        public async Task<UserDbModel> GetUserById(int id)
        {
            return await _conn.Table<UserDbModel>()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();
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

        public async Task<UserDbModel?> GetUserByToken(string token)
        {
            var tokenModel = await _conn.Table<TokenDbModel>()
                .Where(x => x.Token == token)
                .FirstOrDefaultAsync();
            if (tokenModel == null)
                return null;

            return await GetUserById(tokenModel.UserId);
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

        public async Task<ExtendedProfileDbModel?> GetProfileAsync(int id, int userId)
        {
            var q = @"
                SELECT p.*,
                       u.Name AS UserName,
                       IIF(ps.ProfileId, 1, 0) AS IsStarred,
                       c.Data
                  FROM profile AS p
                LEFT JOIN profile_star AS ps ON p.Id = ps.ProfileId AND ps.UserId = ?
                LEFT JOIN randoconfig AS c ON p.ConfigId = c.Id
                LEFT JOIN user AS u ON p.UserId = u.Id
                WHERE p.Id = ?
                  AND NOT(p.Flags & 1)";
            var result = await _conn.FindWithQueryAsync<ExtendedProfileDbModel>(q, userId, id);
            return result;
        }

        public async Task<ProfileDbModel?> GetDefaultProfile()
        {
            return await _conn.Table<ProfileDbModel>()
                .Where(x => (x.Flags & 1) == 0)
                .Where(x => x.UserId == SystemUserId)
                .Where(x => x.Name == "Default")
                .FirstOrDefaultAsync();
        }

        public async Task<ExtendedProfileDbModel[]> GetProfilesForUserAsync(int userId)
        {
            var q = @"
                SELECT p.*,
                       u.Name AS UserName,
                       IIF(ps.ProfileId, 1, 0) AS IsStarred,
                       c.Data
                  FROM profile AS p
                LEFT JOIN profile_star AS ps ON p.Id = ps.ProfileId AND ps.UserId = ?
                LEFT JOIN randoconfig AS c ON p.ConfigId = c.Id
                LEFT JOIN user AS u ON p.UserId = u.Id
                WHERE (p.UserId = ?
                    OR p.UserId = ?
                    OR ps.ProfileId IS NOT NULL)
                  AND ((p.Flags & 2) OR p.UserId = ?)
                  AND NOT(p.Flags & 1)";
            var result = await _conn.QueryAsync<ExtendedProfileDbModel>(q,
                userId,
                userId,
                SystemUserId,
                userId);
            return [.. result];
        }

        public async Task<ExtendedProfileDbModel[]> GetProfilesAsync(string? query, string? user, int page = 1)
        {
            var pageSize = 25;
            var skip = (page - 1) * pageSize;
            var parameters = new List<object>();
            var q = @"
                SELECT p.*, u.Name as UserName, IIF(ps.ProfileId, 1, 0) AS IsStarred
                FROM profile AS p
                LEFT JOIN user AS u ON p.UserId = u.Id
                LEFT JOIN profile_star AS ps ON p.Id = ps.ProfileId
                WHERE (p.Name LIKE ? OR p.Description LIKE ?)
                  AND p.UserId != ?
                  AND p.Flags & 2
                  AND NOT(p.Flags & 1)";
            parameters.Add($"%{query}%");
            parameters.Add($"%{query}%");
            parameters.Add(SystemUserId);

            if (!string.IsNullOrEmpty(user))
            {
                q += " AND u.Name = ?";
                parameters.Add(user);
            }

            q += " ORDER BY StarCount DESC LIMIT ? OFFSET ?";
            parameters.Add(pageSize);
            parameters.Add(skip);

            var result = await _conn.QueryAsync<ExtendedProfileDbModel>(q, [.. parameters]);
            return [.. result];
        }

        public async Task StarProfileAsync(int profileId, int userId, bool value)
        {
            if (value)
            {
                await _conn.ExecuteAsync("INSERT OR IGNORE INTO profile_star (ProfileId, UserId) VALUES (?, ?)",
                    profileId,
                    userId);
            }
            else
            {
                await _conn.ExecuteAsync("DELETE FROM profile_star WHERE ProfileId = ? AND UserId = ?",
                    profileId,
                    userId);
            }
            await _conn.ExecuteAsync(
                @"UPDATE profile
                     SET StarCount = (SELECT COUNT(*) FROM profile_star WHERE ProfileId = ? AND UserId = ?)
                   WHERE Id = ?",
                profileId,
                userId,
                profileId);
        }

        public async Task<ProfileDbModel> CreateProfileAsync(
            ProfileDbModel profile,
            Dictionary<string, object> config)
        {
            await _conn.InsertAsync(profile);
            await SetProfileConfigAsync(profile.Id, config);
            return profile;
        }

        public async Task UpdateProfileAsync(ProfileDbModel profile)
        {
            await _conn.UpdateAsync(profile, typeof(ProfileDbModel));
        }

        public async Task DeleteProfileAsync(int profileId)
        {
            await _conn.ExecuteAsync("UPDATE profile SET Flags = Flags | 1 WHERE Id = ?", profileId);
        }

        public async Task SetProfileConfigAsync(int profileId, Dictionary<string, object> config)
        {
            var newConfigData = config.ToJson(indented: false);
            var existingConfigId = await _conn.ExecuteScalarAsync<int>("SELECT ConfigId FROM profile WHERE Id = ?", profileId);
            var existingConfigData = await _conn.ExecuteScalarAsync<string>("SELECT Data FROM randoconfig WHERE Id = ?", existingConfigId);
            if (existingConfigData == newConfigData)
                return;

            var randoConfig = await GetOrCreateRandoConfig(profileId, newConfigData);
            await _conn.ExecuteAsync("UPDATE profile SET ConfigId = ? WHERE Id = ?", randoConfig.Id, profileId);
            await CleanRandoConfig(existingConfigId);
        }

        public async Task SetUserConfigAsync(int userId, int profileId, Dictionary<string, object> config)
        {
            var newConfigData = config.ToJson(indented: false);
            var existingConfigId = await _conn.ExecuteScalarAsync<int>("SELECT ConfigId FROM user WHERE Id = ?", userId);
            var existingConfigData = await _conn.ExecuteScalarAsync<string>("SELECT Data FROM randoconfig WHERE Id = ?", existingConfigId);
            if (existingConfigData == newConfigData)
                return;

            var randoConfig = await GetOrCreateRandoConfig(profileId, newConfigData);
            await _conn.ExecuteAsync("UPDATE user SET ConfigId = ? WHERE Id = ?", randoConfig.Id, userId);
            await CleanRandoConfig(existingConfigId);
        }

        public async Task<RandoConfigDbModel> GetOrCreateRandoConfig(int profileId, string data)
        {
            var result = await _conn.Table<RandoConfigDbModel>()
                .Where(x => x.BasedOnProfileId == profileId && x.Data == data)
                .FirstOrDefaultAsync();
            if (result == null)
            {
                result = new RandoConfigDbModel()
                {
                    Created = DateTime.UtcNow,
                    BasedOnProfileId = profileId,
                    Data = data
                };
                await _conn.InsertAsync(result);
            }
            return result;
        }

        public async Task CleanRandoConfig(int randoConfigId)
        {
            var a = await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM profile WHERE ConfigId = ?", randoConfigId);
            var b = await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM rando WHERE ConfigId = ?", randoConfigId);
            var total = a + b;
            if (total == 0)
                await _conn.ExecuteAsync("DELETE FROM randoconfig WHERE Id = ?", randoConfigId);
        }

        public async Task<RandoDbModel> CreateRando(RandoDbModel rando)
        {
            await _conn.InsertAsync(rando);
            return rando;
        }

        public async Task UpdateSeedCount(int profileId)
        {
            await _conn.ExecuteAsync(
                @"UPDATE profile
                     SET SeedCount = (
                        SELECT COUNT(*) FROM rando
                        INNER JOIN randoconfig ON ConfigId = randoconfig.Id
                        WHERE BasedOnProfileId = ?)
                   WHERE Id = ?",
                profileId,
                profileId);
        }

        public async Task<int> CountRandos()
        {
            return await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM rando");
        }

        public async Task<int> CountProfiles()
        {
            return await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM rando");
        }

        public async Task<int> CountUsers()
        {
            return await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM rando");
        }

        public async Task<bool> AdminUserExistsAsync()
        {
            var rows = await _conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM user WHERE Role = ?",
                UserRoleKind.Administrator);
            return rows != 0;
        }

        public Task UpdateUserAsync(UserDbModel user)
        {
            return _conn.UpdateAsync(user);
        }

        public Task<UserDbModel> GetUserAsync(int id)
        {
            return _conn.Table<UserDbModel>().FirstOrDefaultAsync(x => x.Id == id);
        }

        public Task<UserDbModel> GetUserAsync(string name)
        {
            var lowerName = name.ToLowerInvariant();
            return _conn.Table<UserDbModel>().FirstOrDefaultAsync(x => x.NameLowerCase == lowerName);
        }

        public async Task<UserDbModel[]> GetUsersAsync(string sort, bool descending, int skip, int count)
        {
            var q = _conn.Table<UserDbModel>();
            if (sort != null)
            {
                if (descending)
                {
                    q = sort.ToLowerInvariant() switch
                    {
                        "name" => q.OrderByDescending(x => x.Name),
                        "created" => q.OrderByDescending(x => x.Created),
                        "role" => q.OrderByDescending(x => x.Role),
                        _ => q
                    };
                }
                else
                {
                    q = sort.ToLowerInvariant() switch
                    {
                        "name" => q.OrderBy(x => x.Name),
                        "created" => q.OrderBy(x => x.Created),
                        "role" => q.OrderBy(x => x.Role),
                        _ => q
                    };
                }
            }
            return await q
                .Skip(skip)
                .Take(count)
                .ToArrayAsync();
        }

        public class ExtendedProfileDbModel : ProfileDbModel
        {
            public string UserName { get; set; } = "";
            public string Data { get; set; } = "";
            public bool IsStarred { get; set; }
        }
    }
}
