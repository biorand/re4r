using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.RE4R.Extensions;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Services
{
    public sealed class DatabaseService
    {
        private readonly SQLiteAsyncConnection _conn;

        public int SystemUserId => 1;

        public DatabaseService(Re4rConfiguration config)
        {
            var databaseConfig = config.Database;
            var databasePath = (databaseConfig?.Path) ?? throw new Exception();
            _conn = new SQLiteAsyncConnection(databasePath);
            Initialize().Wait();
        }

        private async Task Initialize()
        {
            await _conn.CreateTablesAsync();
        }

        private QueryBuilder<T> BuildQuery<T>(string q, params object[] args) where T : new()
        {
            return new QueryBuilder<T>(_conn, q, args);
        }

        public async Task CreateTables()
        {
            await _conn.CreateTableAsync<UserDbModel>();
            await _conn.CreateTableAsync<TokenDbModel>();
            await _conn.CreateTableAsync<ProfileDbModel>();
            await _conn.CreateTableAsync<RandoDbModel>();
            await _conn.CreateTableAsync<RandoConfigDbModel>();
            await _conn.CreateTableAsync<TwitchDbModel>();
            await _conn.CreateTableAsync<KofiDbModel>();
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
            var now = DateTime.UtcNow - TimeSpan.FromDays(30);
            var tokenModel = await _conn.Table<TokenDbModel>()
                .Where(x => x.Token == token && x.LastUsed > now)
                .FirstOrDefaultAsync();
            if (tokenModel == null)
                return null;

            return await GetUserById(tokenModel.UserId);
        }

        public async Task<UserDbModel?> GetUserByKofiEmailToken(string? token)
        {
            if (token == null)
                return null;

            return await _conn.Table<UserDbModel>()
                .Where(x => x.KofiEmailVerification == token)
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

        public async Task<TokenDbModel?> GetTokenAsync(string token)
        {
            return await _conn.Table<TokenDbModel>()
                .Where(x => x.Token == token)
                .FirstOrDefaultAsync();
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

        public async Task DeleteTokenAsync(int tokenId)
        {
            await _conn.Table<TokenDbModel>()
                .Where(x => x.Id == tokenId)
                .DeleteAsync();
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

        public Task<LimitedResult<ExtendedProfileDbModel>> GetProfilesAsync(
            int starUserId,
            string? query,
            string? user,
            SortOptions? sortOptions,
            LimitOptions? limitOptions)
        {
            var q = BuildQuery<ExtendedProfileDbModel>(@"
                SELECT p.*, u.Name as UserName, IIF(ps.ProfileId, 1, 0) AS IsStarred
                FROM profile AS p
                LEFT JOIN user AS u ON p.UserId = u.Id
                LEFT JOIN profile_star AS ps ON p.Id = ps.ProfileId AND ps.UserId = ?",
                starUserId);

            q.Where("p.Name LIKE ? OR p.Description LIKE ?", $"%{query}%", $"%{query}%");
            q.Where("p.UserId != ?", SystemUserId);
            q.Where("p.Flags & 2");
            q.Where("NOT(p.Flags & 1)");
            q.WhereIf("u.Name = ?", user);

            return q.ExecuteLimitedAsync(sortOptions, limitOptions);
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
                     SET StarCount = (SELECT COUNT(*) FROM profile_star WHERE ProfileId = ?)
                   WHERE Id = ?",
                profileId,
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

        public Task<LimitedResult<ExtendedRandoDbModel>> GetRandosAsync(
            int? filterUserId = null,
            int? viewerUserId = null,
            SortOptions? sortOptions = null,
            LimitOptions? limitOptions = null)
        {
            var q = BuildQuery<ExtendedRandoDbModel>(@"
                SELECT r.*,
	                   u.Name as UserName,
	                   u.Email as UserEmail,
	                   p.Id as ProfileId,
	                   p.Name as ProfileName,
	                   pu.Id as ProfileUserId,
	                   pu.Name as ProfileUserName,
	                   c.Data as Config
                FROM rando as r
                LEFT JOIN user as u ON r.UserId = u.Id
                LEFT JOIN randoconfig as c ON r.ConfigId = c.Id
                LEFT JOIN profile as p ON c.BasedOnProfileId = p.Id
                LEFT JOIN user as pu ON p.UserId = pu.Id");
            q.WhereIf("r.UserId = ?", filterUserId);
            if (viewerUserId != null)
            {
                q.Where("((u.Flags & 1) OR u.Id = ?)", viewerUserId.Value);
            }
            return q.ExecuteLimitedAsync(sortOptions, limitOptions);
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

        public Task<LimitedResult<UserDbModel>> GetUsersAsync(SortOptions? sortOptions = null, LimitOptions? limitOptions = null)
        {
            var q = BuildQuery<UserDbModel>("SELECT * FROM user");
            return q.ExecuteLimitedAsync(sortOptions, limitOptions);
        }

        public async Task<TwitchDbModel> AddOrUpdateUserTwitchAsync(int userId, TwitchDbModel twitch)
        {
            var user = await GetUserById(userId);
            if (user.TwitchId is int twitchId)
            {
                twitch.Id = twitchId;
                await _conn.UpdateAsync(twitch);
            }
            else
            {
                await _conn.InsertAsync(twitch);
                user.TwitchId = twitch.Id;
                await UpdateUserAsync(user);
            }
            return twitch;
        }

        public async Task<TwitchDbModel?> GetUserTwitchAsync(int userId)
        {
            var user = await GetUserById(userId);
            if (user.TwitchId is int twitchId)
            {
                return await _conn.GetAsync<TwitchDbModel>(twitchId);
            }
            return null;
        }

        public async Task DeleteUserTwitchAsync(int userId)
        {
            var user = await GetUserById(userId);
            if (user.TwitchId is int twitchId)
            {
                user.TwitchId = null;
                await UpdateUserAsync(user);
                await _conn.DeleteAsync(new TwitchDbModel() { Id = twitchId });
            }
        }

        public async Task InsertKofiAsync(KofiDbModel kofi)
        {
            await _conn.InsertAsync(kofi);
        }

        public async Task<KofiDbModel[]> GetKofiByUserAsync(int userId)
        {
            return await _conn.Table<KofiDbModel>()
                .Where(x => x.UserId == userId)
                .ToArrayAsync();
        }

        public async Task<int?> FindKofiMatchAsync(string email)
        {
            var result = await _conn.QueryScalarsAsync<int>(@"
                SELECT Id FROM user
                 WHERE Email = ?
                    OR (KofiEmail = ? AND KofiEmailVerification IS NULL)
                 LIMIT 1", email, email);
            if (result.Count == 0)
                return null;
            return result.First();
        }

        public async Task<int?> UpdateKofiMatchesAsync(string email)
        {
            var result = await _conn.QueryAsync<int>(@"
                SELECT Id FROM user
                 WHERE Email = ?
                    OR (KofiEmail = ? AND KofiEmailVerification IS NULL)
                 LIMIT 1", email, email);
            if (result.Count == 0)
                return null;
            return result.First();
        }

        public async Task UpdateAllUnmatchedKofiMatchesAsync()
        {
            await _conn.ExecuteAsync(@"
                UPDATE kofi
                SET UserId = t.UserId
                FROM (
	                SELECT kofi.Id AS Id, user.Id AS UserId
	                FROM kofi
	                JOIN user ON user.Email = kofi.Email OR (user.KofiEmail = kofi.Email AND KofiEmailVerification IS NULL)
	                WHERE kofi.UserId IS NULL
                ) t
                WHERE kofi.Id = t.Id");
        }

        private static async Task<LimitedResult<T>> ExecuteLimitedResult<T>(
            AsyncTableQuery<T> query,
            LimitOptions? options) where T : new()
        {
            if (options is LimitOptions o)
            {
                var total = await query.CountAsync();
                var results = await query
                    .Skip(o.Skip)
                    .Take(o.Limit)
                    .ToArrayAsync();
                return new LimitedResult<T>(total, o.Skip, results);
            }
            else
            {
                var results = await query.ToArrayAsync();
                return new LimitedResult<T>(results.Length, 0, results);
            }
        }

        public class ExtendedProfileDbModel : ProfileDbModel
        {
            public string UserName { get; set; } = "";
            public string Data { get; set; } = "";
            public bool IsStarred { get; set; }
        }

        public class ExtendedRandoDbModel : RandoDbModel
        {
            public string? UserName { get; set; }
            public string? UserEmail { get; set; }
            public int ProfileId { get; set; }
            public string? ProfileName { get; set; }
            public int ProfileUserId { get; set; }
            public string? ProfileUserName { get; set; }
            public string? Config { get; set; }
        }
    }

    public readonly struct SortOptions(string field, bool descending)
    {
        public string Field => field;
        public bool Descending => descending;

        public static SortOptions? FromQuery(string? sort, string? order, params string[] allowed)
        {
            if (sort == null) return null;
            var field = allowed.FirstOrDefault(x => x.Equals(sort, StringComparison.OrdinalIgnoreCase));
            if (field == null) return null;
            var desc = "desc".Equals(order, StringComparison.OrdinalIgnoreCase);
            return new SortOptions(sort, desc);
        }
    }

    public readonly struct LimitOptions(int skip, int limit)
    {
        public int Skip => skip;
        public int Limit => limit;

        public static LimitOptions FromPage(int page, int itemsPerPage) => new((page - 1) * itemsPerPage, itemsPerPage);
    }


    public sealed class LimitedResult<T>(int total, int offset, T[] results)
    {
        public int Total => total;
        public int Offset => offset;
        public T[] Results => results;
        public int From => offset + 1;
        public int To => offset + results.Length;
    }

    public class QueryBuilder<T> where T : new()
    {
        private readonly ISQLiteAsyncConnection _conn;
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly List<object> _parameters = new List<object>();

        public QueryBuilder(ISQLiteAsyncConnection conn, string query = "", params object[] parameters)
        {
            _conn = conn;
            Append(query, parameters);
        }

        public void Append(string query, params object[] parameters)
        {
            if (_sb.Length > 0 && _sb[^1] != '\n')
            {
                _sb.Append('\n');
            }
            _sb.AppendLine(query);
            _parameters.AddRange(parameters);
        }

        public void Where(string query, params object[] parameters)
        {
            if (!_sb.ToString().Contains("WHERE"))
                Append($"WHERE ({query})", parameters);
            else
                Append($"AND ({query})", parameters);
        }

        public void WhereIf(string query, object? parameter)
        {
            if (parameter != null)
            {
                if (!_sb.ToString().Contains("WHERE"))
                    Append($"WHERE {query}", parameter);
                else
                    Append($"AND {query}", parameter);
            }
        }

        public Task<int> CountAsync()
        {
            var q = _sb.ToString();
            var fromIndex = q.IndexOf("FROM");
            if (fromIndex == -1)
                throw new Exception("Unable to create count query.");
            q = $"SELECT COUNT(*) {q[fromIndex..]}";
            return _conn.ExecuteScalarAsync<int>(q, [.. _parameters]);
        }

        public async Task<LimitedResult<T>> ExecuteLimitedAsync(SortOptions? sortOptions, LimitOptions? limitOptions)
        {
            var total = await CountAsync();
            if (sortOptions is SortOptions so)
            {
                var kind = so.Descending ? "DESC" : "ASC";
                Append($"ORDER BY {so.Field} {kind}");
            }
            if (limitOptions is LimitOptions lo)
            {
                Append("LIMIT ?", lo.Limit);
                Append("OFFSET ?", lo.Skip);
            }
            var results = await _conn.QueryAsync<T>(_sb.ToString(), [.. _parameters]);
            return new LimitedResult<T>(total, limitOptions?.Skip ?? 0, [.. results]);
        }
    }
}
