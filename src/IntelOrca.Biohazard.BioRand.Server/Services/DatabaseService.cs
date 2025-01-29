using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Extensions;
using IntelOrca.Biohazard.BioRand.Server.Models;
using SQLite;

namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public sealed class DatabaseService
    {
        private const int LatestVersion = 4;

        private readonly SQLiteAsyncConnection _conn;
        private int _originalVersion;

        public int SystemUserId => 1;

        public DatabaseService(BioRandServerConfiguration config)
        {
            var databaseConfig = config.Database;
            var databasePath = (databaseConfig?.Path) ?? throw new Exception();
            _conn = new SQLiteAsyncConnection(databasePath);
            Initialize().Wait();
        }

        private QueryBuilder<T> BuildQuery<T>(string q, params object[] args) where T : new()
        {
            return new QueryBuilder<T>(_conn, q, args);
        }

        private async Task Initialize()
        {
            await InitializeMetaTable();
            await ApplyPreMigrations();
            await _conn.CreateTableAsync<GameDbModel>();
            await _conn.CreateTableAsync<UserDbModel>();
            await _conn.CreateTableAsync<UserTagDbModel>();
            await _conn.CreateTableAsync<TokenDbModel>();
            await _conn.CreateTableAsync<ProfileDbModel>();
            await _conn.CreateTableAsync<RandoDbModel>();
            await _conn.CreateTableAsync<RandoConfigDbModel>();
            await _conn.CreateTableAsync<TwitchDbModel>();
            await _conn.CreateTableAsync<KofiDbModel>();
            await _conn.CreateTableAsync<NewsDbModel>();
            await _conn.ExecuteAsync(
                @"CREATE TABLE IF NOT EXISTS ""user_usertag"" (
                    ""UserTagId"" integer not null,
                    ""UserId"" integer not null,
                    PRIMARY KEY (""UserTagId"", ""UserId""))");
            await _conn.ExecuteAsync(
                @"CREATE TABLE IF NOT EXISTS ""profile_star"" (
                    ""ProfileId"" integer not null,
                    ""UserId"" integer not null,
                    PRIMARY KEY (""ProfileId"", ""UserId""))");
            await GetOrCreateSystemUser();
            await InsertGames();
            await InsertUserTags();
            await ApplyPostMigrations();
        }

        private async Task InitializeMetaTable()
        {
            await _conn.CreateTableAsync<MetaDbModel>();
            var meta = await _conn.FindAsync<MetaDbModel>(1);
            if (meta == null)
            {
                _originalVersion = 0;
                meta = new MetaDbModel()
                {
                    Id = 1,
                    Version = LatestVersion
                };
                await _conn.InsertAsync(meta);
            }
            else
            {
                _originalVersion = meta.Version;
                meta.Version = LatestVersion;
                await _conn.UpdateAsync(meta);
            }
        }

        private async Task ApplyPreMigrations()
        {
            if (_originalVersion < 1)
            {
                await _conn.ExecuteAsync("ALTER TABLE rando ADD COLUMN Status INTEGER NOT NULL DEFAULT 0");
            }
            if (_originalVersion < 2)
            {
                await _conn.ExecuteAsync("ALTER TABLE news ADD COLUMN GameId INTEGER NOT NULL DEFAULT 1");
                await _conn.ExecuteAsync("ALTER TABLE profile ADD COLUMN GameId INTEGER NOT NULL DEFAULT 1");
                await _conn.ExecuteAsync("ALTER TABLE rando ADD COLUMN GameId INTEGER NOT NULL DEFAULT 1");
            }
            if (_originalVersion < 3)
            {
                await _conn.ExecuteAsync("ALTER TABLE kofi ADD COLUMN GameId INTEGER NOT NULL DEFAULT 1");
            }
        }

        private async Task ApplyPostMigrations()
        {
            if (_originalVersion < 4)
            {
                await MapRoleToUserTag(0, "pending");
                await MapRoleToUserTag(6, "admin");
                await MapRoleToUserTag(7, "system");
                await _conn.ExecuteAsync("ALTER TABLE user DROP COLUMN Role");
            }

            async Task MapRoleToUserTag(int role, string tag)
            {
                var userTag = await GetUserTag(tag) ?? throw new Exception("User tag not found");
                var userTagId = userTag.Id;
                await _conn.ExecuteAsync(
                    """
                    INSERT INTO user_usertag (UserTagId, UserId)
                    SELECT ?, u.Id
                    FROM user u
                    WHERE u.Role = ?
                    AND NOT EXISTS (
                        SELECT 1
                        FROM user_usertag utt
                        WHERE utt.UserId = u.Id
                        AND utt.UserTagId = ?)
                    """, userTagId, role, userTagId);
            }
        }

        private async Task<UserDbModel> GetOrCreateSystemUser()
        {
            var systemUser = await GetUserById(1);
            if (systemUser != null)
                return systemUser;

            var newUser = new UserDbModel
            {
                Id = 1,
                Created = DateTime.UtcNow,
                Name = "System",
                NameLowerCase = "system",
                Email = ""
            };
            await _conn.InsertOrReplaceAsync(newUser);
            await UpdateUserTagsForUser(newUser.Id, ["system"]);
            return newUser;
        }

        private async Task InsertGames()
        {
            var game1 = await GetGameByIdAsync(1) ?? new GameDbModel();
            game1.Id = 1;
            game1.Name = "Resident Evil 4 (2024)";
            game1.Moniker = "re4r";
            var game2 = await GetGameByIdAsync(2) ?? new GameDbModel();
            game2.Id = 2;
            game2.Name = "Resident Evil 2 (2019)";
            game2.Moniker = "re2r";
            await _conn.InsertOrReplaceAsync(game1);
            await _conn.InsertOrReplaceAsync(game2);
        }

        private async Task InsertUserTags()
        {
            await AddTag("system", 0xFF99AABB, 0xFF233876);
            await AddTag("admin", 0xFF99AABB, 0xFF233876);
            await AddTag("pending", 0xFFFCE8F3, 0xFF751A3D);
            await AddTag("banned", 0xFFFF0000, 0xFFFF0000);
            await AddTagForEachGame("patron/long", 0xFFDEF7EC, 0xFF014737);
            await AddTagForEachGame("patron/manual", 0xFFDEF7EC, 0xFF014737);
            await AddTagForEachGame("patron/kofi", 0xFFDEF7EC, 0xFF014737);
            await AddTagForEachGame("patron/twitch", 0xFFDEF7EC, 0xFF014737);
            await AddTagForEachGame("curator", 0xFFFDF6B2, 0xFF633112);
            await AddTagForEachGame("tester", 0xFFFDF6B2, 0xFF633112);

            async Task AddTagForEachGame(string label, uint light, uint dark)
            {
                await AddTag($"re2r:{label}", light, dark);
                await AddTag($"re4r:{label}", light, dark);
            }

            async Task AddTag(string label, uint light, uint dark)
            {
                await AddOrReplaceUserTag(new UserTagDbModel()
                {
                    Label = label,
                    ColorLight = light,
                    ColorDark = dark
                });
            }
        }

        public async Task<GameDbModel[]> GetGamesAsync()
        {
            return await _conn.Table<GameDbModel>().ToArrayAsync();
        }

        public async Task<GameDbModel> GetGameByIdAsync(int id)
        {
            return await _conn.Table<GameDbModel>()
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();
        }

        public Task UpdateGameAsync(GameDbModel game)
        {
            return _conn.UpdateAsync(game, typeof(GameDbModel));
        }

        public async Task<ExtendedUserDbModel> GetUserById(int id)
        {
            return await _conn.FindWithQueryAsync<ExtendedUserDbModel>(
                """
                SELECT u.*, GROUP_CONCAT(ut.Label, ',') AS Tags
                FROM user AS u
                LEFT JOIN user_usertag AS uut ON u.Id = uut.UserId
                LEFT JOIN usertag AS ut ON ut.Id = uut.UserTagId
                WHERE u.Id = ?
                GROUP BY u.Id
                """, id);
        }

        public async Task<ExtendedUserDbModel> GetUserByEmail(string email)
        {
            var emailLower = email.ToLowerInvariant();
            return await _conn.FindWithQueryAsync<ExtendedUserDbModel>(
                """
                SELECT u.*, GROUP_CONCAT(ut.Label, ',') AS Tags
                FROM user AS u
                LEFT JOIN user_usertag AS uut ON u.Id = uut.UserId
                LEFT JOIN usertag AS ut ON ut.Id = uut.UserTagId
                WHERE u.Email = ?
                GROUP BY u.Id
                """, emailLower);
        }

        public async Task<ExtendedUserDbModel> GetUserByName(string name)
        {
            var nameLower = name.ToLowerInvariant();
            return await _conn.FindWithQueryAsync<ExtendedUserDbModel>(
                """
                SELECT u.*, GROUP_CONCAT(ut.Label, ',') AS Tags
                FROM user AS u
                LEFT JOIN user_usertag AS uut ON u.Id = uut.UserId
                LEFT JOIN usertag AS ut ON ut.Id = uut.UserTagId
                WHERE u.NameLowerCase = ?
                GROUP BY u.Id
                """, nameLower);
        }

        public async Task<ExtendedUserDbModel?> GetUserByToken(string token)
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
                Email = email.ToLowerInvariant()
            };
            await _conn.InsertAsync(user);
            await UpdateUserTagsForUser(user.Id, ["pending"]);
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

        public Task<LimitedResult<TokenUserDbViewModel>> GetTokensAsync(
            SortOptions? sortOptions = null,
            LimitOptions? limitOptions = null)
        {
            var q = BuildQuery<TokenUserDbViewModel>(@"
                SELECT t.*,
                       u.Name as UserName,
                       u.Email as UserEmail
                FROM token AS t
                LEFT JOIN user AS u ON t.UserId = u.Id");
            return q.ExecuteLimitedAsync(sortOptions, limitOptions);
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

        public async Task DeleteExpiredTokens()
        {
            // Delete tokens that were never used
            await _conn.ExecuteAsync(@"
                DELETE FROM token
                WHERE Id IN (
                    SELECT Id
                    FROM token
                    WHERE LastUsed IS NULL
                      AND datetime((Created - 621355968000000000) / 10000000, 'unixepoch') < date('now', '-1 days'))");

            // Delete tokens that haven't been used in ages
            await _conn.ExecuteAsync(@"
                DELETE FROM token
                WHERE Id IN (
                    SELECT Id
                    FROM (
                        SELECT Id,
                               datetime((LastUsed - 621355968000000000) / 10000000, 'unixepoch') AS 'LastUsed'
                        FROM token
                        WHERE LastUsed IS NOT NULL)
                    WHERE LastUsed < date('now', '-30 days'))");
        }

        public async Task AddOrReplaceUserTag(UserTagDbModel userTag)
        {
            var tag = await GetUserTag(userTag.Label);
            if (tag != null)
            {
                userTag.Id = tag.Id;
                await _conn.UpdateAsync(userTag);
            }
            else
            {
                await _conn.InsertAsync(userTag);
            }
        }

        public async Task<UserTagDbModel?> GetUserTag(string label)
        {
            return await _conn
                .Table<UserTagDbModel>()
                .FirstOrDefaultAsync(x => x.Label == label);
        }

        public async Task<UserTagDbModel[]> GetUserTags()
        {
            return await _conn
                .Table<UserTagDbModel>()
                .ToArrayAsync();
        }

        public async Task<UserTagDbModel[]> GetUserTagsForUser(int userId)
        {
            var q = @"
                SELECT * FROM usertag AS t
                INNER JOIN user_usertag AS ut ON ut.UserTagId = t.Id
                WHERE ut.UserId = 2";
            var result = await _conn.QueryAsync<UserTagDbModel>(q, userId);
            return [.. result];
        }

        public async Task UpdateUserTagsForUser(int userId, IEnumerable<UserTagDbModel> tags)
        {
            await _conn.RunInTransactionAsync(c =>
            {
                c.Execute("DELETE FROM user_usertag WHERE UserId = ?", userId);
                foreach (var tag in tags)
                {
                    c.Execute("INSERT OR IGNORE INTO user_usertag (UserId, UserTagId) VALUES (?, ?)",
                        userId,
                        tag.Id);
                }
            });
        }

        public async Task UpdateUserTagsForUser(int userId, IEnumerable<string> labels)
        {
            var tags = new List<UserTagDbModel>();
            foreach (var label in labels)
            {
                var tag = await GetUserTag(label) ?? throw new Exception("User tag not found");
                tags.Add(tag);
            }
            await UpdateUserTagsForUser(userId, tags);
        }

        public async Task EnsureUserTagForUserAsync(int userId, string label)
        {
            var tag = await GetUserTag(label) ?? throw new Exception("User tag not found");
            await _conn.ExecuteAsync("INSERT OR IGNORE INTO user_usertag (UserId, UserTagId) VALUES (?, ?)", userId, tag.Id);
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

        public async Task<ProfileDbModel?> GetDefaultProfile(int gameId)
        {
            return await _conn.Table<ProfileDbModel>()
                .Where(x => (x.Flags & 1) == 0)
                .Where(x => x.UserId == SystemUserId)
                .Where(x => x.Name == "Default")
                .Where(x => x.GameId == gameId)
                .FirstOrDefaultAsync();
        }

        public async Task<ExtendedProfileDbModel[]> GetProfilesForUserAsync(int userId, int gameId)
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
                WHERE GameId = ?
                  AND (p.UserId = ? OR IsStarred OR (p.Flags & 4))
                  AND NOT(p.Flags & 1)";
            var result = await _conn.QueryAsync<ExtendedProfileDbModel>(q,
                userId,
                gameId,
                userId);
            return [.. result];
        }

        public Task<LimitedResult<ExtendedProfileDbModel>> GetProfilesAsync(
            int starUserId,
            string? query,
            int? gameId,
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
            q.WhereIf("p.GameId = ?", gameId);
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
            RandomizerConfiguration config)
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

        public async Task SetProfileConfigAsync(int profileId, RandomizerConfiguration config)
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

        public Task<ExtendedRandoDbModel> GetRandoAsync(int id)
        {
            var q = @"
                SELECT r.*,
                       u.Name as UserName,
                       u.Email as UserEmail,
                       p.Id as ProfileId,
                       p.Name as ProfileName,
                       p.Description as ProfileDescription,
                       pu.Id as ProfileUserId,
                       pu.Name as ProfileUserName,
                       c.Data as Config
                FROM rando as r
                LEFT JOIN user as u ON r.UserId = u.Id
                LEFT JOIN randoconfig as c ON r.ConfigId = c.Id
                LEFT JOIN profile as p ON c.BasedOnProfileId = p.Id
                LEFT JOIN user as pu ON p.UserId = pu.Id
                WHERE r.Id = ?";
            return _conn.FindWithQueryAsync<ExtendedRandoDbModel>(q, id);
        }

        public Task UpdateRandoAsync(RandoDbModel rando)
        {
            return _conn.UpdateAsync(rando, typeof(RandoDbModel));
        }

        public Task SetRandoStatusAsync(int id, RandoStatus status)
        {
            return _conn.ExecuteAsync(@"UPDATE rando SET Status = ? WHERE Id = ?", status, id);
        }

        public Task SetAllRandoStatusToExpiredAsync()
        {
            return _conn.ExecuteAsync(@"UPDATE rando SET Status = ? WHERE Status <> ?", RandoStatus.Expired, RandoStatus.Failed);
        }

        public Task<LimitedResult<ExtendedRandoDbModel>> GetRandosAsync(
            int? filterGameId = null,
            int? filterUserId = null,
            int? viewerUserId = null,
            SortOptions? sortOptions = null,
            LimitOptions? limitOptions = null)
        {
            var q = BuildQuery<ExtendedRandoDbModel>(@"
                SELECT r.*,
                       u.Name as UserName,
                       u.Email as UserEmail,
                       COALESCE(GROUP_CONCAT(ut.Label, ','), '') AS UserTags,
                       p.Id as ProfileId,
                       p.Name as ProfileName,
                       pu.Id as ProfileUserId,
                       pu.Name as ProfileUserName,
                       c.Data as Config
                FROM rando as r
                LEFT JOIN user as u ON r.UserId = u.Id
                LEFT JOIN randoconfig as c ON r.ConfigId = c.Id
                LEFT JOIN profile as p ON c.BasedOnProfileId = p.Id
                LEFT JOIN user as pu ON p.UserId = pu.Id
                LEFT JOIN user_usertag AS uut ON r.UserId = uut.UserId
                LEFT JOIN usertag AS ut ON ut.Id = uut.UserTagId");
            q.WhereIf("r.GameId = ?", filterGameId);
            q.WhereIf("r.UserId = ?", filterUserId);
            if (viewerUserId != null)
            {
                q.Where("((u.Flags & 1) OR u.Id = ?)", viewerUserId.Value);
            }
            q.Append("GROUP BY r.Id");

            q.SetCountQuery("SELECT COUNT(*) FROM rando as r");

            return q.ExecuteLimitedAsync(sortOptions, limitOptions);
        }

        public Task<LimitedResult<ExtendedRandoDbModel>> GetRandosWithStatusAsync(RandoStatus status)
        {
            var q = BuildQuery<ExtendedRandoDbModel>(@"
                SELECT r.*,
                       u.Name as UserName,
                       u.Email as UserEmail,
                       COALESCE(GROUP_CONCAT(ut.Label, ','), '') AS UserTags,
                       p.Id as ProfileId,
                       p.Name as ProfileName,
                       p.Description as ProfileDescription,
                       pu.Id as ProfileUserId,
                       pu.Name as ProfileUserName,
                       c.Data as Config
                FROM rando as r
                LEFT JOIN user as u ON r.UserId = u.Id
                LEFT JOIN randoconfig as c ON r.ConfigId = c.Id
                LEFT JOIN profile as p ON c.BasedOnProfileId = p.Id
                LEFT JOIN user as pu ON p.UserId = pu.Id
                LEFT JOIN user_usertag AS uut ON r.UserId = uut.UserId
                LEFT JOIN usertag AS ut ON ut.Id = uut.UserTagId
                WHERE r.Status = ?
                GROUP BY r.Id", status);
            return q.ExecuteLimitedAsync();
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
            return await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM profile");
        }

        public async Task<int> CountUsers()
        {
            return await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM user");
        }

        public async Task<bool> AdminUserExistsAsync()
        {
            var tag = await GetUserTag("admin") ?? throw new Exception("User tag not found");
            var rows = await _conn.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM user_usertag WHERE UserTagId = ?", tag.Id);
            return rows != 0;
        }

        public Task UpdateUserAsync(UserDbModel user)
        {
            return _conn.UpdateAsync(user);
        }

        public Task<UserDbModel> GetUserAsync(string name)
        {
            var lowerName = name.ToLowerInvariant();
            return _conn.Table<UserDbModel>().FirstOrDefaultAsync(x => x.NameLowerCase == lowerName);
        }

        public Task<LimitedResult<ExtendedUserDbModel>> GetUsersAsync(SortOptions? sortOptions = null, LimitOptions? limitOptions = null)
        {
            var q = BuildQuery<ExtendedUserDbModel>(
                """
                SELECT u.*, COALESCE(GROUP_CONCAT(ut.Label, ','), '') AS Tags
                FROM user AS u
                LEFT JOIN user_usertag AS uut ON u.Id = uut.UserId
                LEFT JOIN usertag AS ut ON ut.Id = uut.UserTagId
                GROUP BY u.Id
                """);
            q.SetCountQuery("SELECT COUNT(*) FROM user");
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

        public async Task CreateKofiAsync(KofiDbModel kofi)
        {
            await _conn.InsertAsync(kofi);
        }

        public Task<KofiDbModel> GetKofiAsync(int id)
        {
            return _conn.FindAsync<KofiDbModel>(id);
        }

        public async Task UpdateKofiAsync(KofiDbModel kofi)
        {
            await _conn.UpdateAsync(kofi);
        }

        public Task<LimitedResult<KofiUserDbViewModel>> GetKofiAsync(
            int? gameId,
            string? userName,
            SortOptions? sortOptions,
            LimitOptions? limitOptions)
        {
            var q = BuildQuery<KofiUserDbViewModel>(@"
                SELECT kofi.*, user.Name as UserName
                FROM kofi
                LEFT JOIN user ON kofi.UserId = user.Id");
            q.WhereIf("kofi.GameId = ?", gameId);
            q.WhereIf("user.Name = ?", userName);
            return q.ExecuteLimitedAsync(sortOptions, limitOptions);
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

        public async Task<KofiDailyDbViewModel[]> GetKofiDaily(int gameId)
        {
            var result = await _conn.QueryAsync<KofiDailyDbViewModel>(@"
                SELECT strftime('%Y-%m-%d', datetime((timestamp - 621355968000000000) / 10000000, 'unixepoch')) AS day,
                       COUNT(*) AS Donations,
                       SUM(Price) AS Amount
                FROM kofi
                WHERE kofi.GameId = ?
                GROUP BY day", gameId);
            return [.. result];
        }

        public async Task<DailyDbViewModel[]> GetSeedsDaily(int gameId)
        {
            var result = await _conn.QueryAsync<DailyDbViewModel>(@"
                SELECT strftime('%Y-%m-%d', datetime((Created - 621355968000000000) / 10000000, 'unixepoch')) AS Day,
                       COUNT(*) AS Value
                FROM rando
                WHERE GameId = ?
                  AND Day >= date('now', '-30 days')
                  AND Day < date('now')
                GROUP BY Day", gameId);
            return [.. result];
        }

        public async Task<MonthlyDbViewModel[]> GetTotalUsersMonthly()
        {
            var result = await _conn.QueryAsync<MonthlyDbViewModel>(@"
                SELECT date(Month || '-01', '+1 month') AS Month, Value
                FROM (
                    SELECT Month, SUM(Value) OVER (ORDER BY Month) AS Value
                    FROM (
                        SELECT strftime('%Y-%m', datetime((Created - 621355968000000000) / 10000000, 'unixepoch')) AS Month,
                               COUNT(*) AS Value
                        FROM user
                        GROUP BY Month))
                WHERE Month >= date('now', '-12 months')");
            return [.. result];
        }

        public async Task<NewsDbModel?> GetNewsItem(int id)
        {
            return await _conn.FindAsync<NewsDbModel>(id);
        }

        public async Task CreateNewsItem(NewsDbModel newsItem)
        {
            await _conn.InsertAsync(newsItem);
        }

        public async Task UpdateNewsItem(NewsDbModel newsItem)
        {
            await _conn.UpdateAsync(newsItem);
        }

        public async Task DeleteNewsItem(int id)
        {
            await _conn.DeleteAsync<NewsDbModel>(id);
        }

        public async Task<NewsDbModel[]> GetNewsItems(int gameId)
        {
            return await _conn
                .Table<NewsDbModel>()
                .Where(x => x.GameId == gameId)
                .OrderByDescending(x => x.Timestamp)
                .ToArrayAsync();
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
            public string? UserTags { get; set; }
            public int ProfileId { get; set; }
            public string? ProfileName { get; set; }
            public string? ProfileDescription { get; set; }
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

        private string? _countQuery;
        private object[]? _countParameters;

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

        public void SetCountQuery(string query, params object[] parameters)
        {
            _countQuery = query;
            _countParameters = parameters;
        }

        public Task<int> CountAsync()
        {
            if (_countQuery == null)
            {
                var q = _sb.ToString();
                var fromIndex = q.IndexOf("FROM");
                if (fromIndex == -1)
                    throw new Exception("Unable to create count query.");
                q = $"SELECT COUNT(*) {q[fromIndex..]}";
                return _conn.ExecuteScalarAsync<int>(q, [.. _parameters]);
            }
            else
            {
                return _conn.ExecuteScalarAsync<int>(_countQuery, _countParameters ?? []);
            }
        }

        public async Task<LimitedResult<T>> ExecuteLimitedAsync(SortOptions? sortOptions = null, LimitOptions? limitOptions = null)
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
