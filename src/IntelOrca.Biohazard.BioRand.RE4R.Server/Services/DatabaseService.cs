﻿using System;
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
            await _conn.CreateTableAsync<RandoConfigDbModel>();
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

        public async Task<ExtendedProfileDbModel[]> GetProfilesAsync(int userId)
        {
            var result = await _conn.QueryAsync<ExtendedProfileDbModel>(
                "SELECT profile.*, user.Name as UserName, Data " +
                "FROM profile " +
                "LEFT JOIN user ON profile.UserId = user.Id " +
                "LEFT JOIN randoconfig ON profile.ConfigId = randoconfig.Id");
            return result.ToArray();
        }

        public async Task<ExtendedProfileDbModel[]> GetProfilesAsync(string? query, string? user, int page = 1)
        {
            var pageSize = 25;
            var skip = (page - 1) * pageSize;
            var parameters = new List<object>();
            var q = @"
                SELECT p.*, u.Name as UserName
                FROM profile AS p
                LEFT JOIN user AS u ON p.UserId = u.Id
                WHERE (p.Name LIKE ? OR p.Description LIKE ?)";
            parameters.Add($"%{query}%");
            parameters.Add($"%{query}%");

            if (!string.IsNullOrEmpty(user))
            {
                q += " AND u.Name = ?";
                parameters.Add(user);
            }

            q += " ORDER BY StarCount LIMIT ? OFFSET ?";
            parameters.Add(pageSize);
            parameters.Add(skip);

            var result = await _conn.QueryAsync<ExtendedProfileDbModel>(q, [.. parameters]);
            return [.. result];
        }

        public async Task<ProfileDbModel> CreateProfileAsync(
            int userId,
            string name,
            string description,
            Dictionary<string, object> config)
        {
            var result = new ProfileDbModel()
            {
                Created = DateTime.UtcNow,
                UserId = userId,
                Name = name,
                Description = description
            };
            await _conn.InsertAsync(result);
            await SetProfileConfigAsync(result.Id, config);
            return result;
        }

        public async Task SetProfileConfigAsync(int profileId, Dictionary<string, object> config)
        {
            var newConfigData = config.ToJson(indented: false);
            var existingConfigId = await _conn.ExecuteScalarAsync<int>("SELECT ConfigId FROM profile WHERE Id = ?", profileId);
            var existingConfigData = await _conn.ExecuteScalarAsync<string>("SELECT Data FROM randoconfig WHERE Id = ?", existingConfigId);
            if (existingConfigData == newConfigData)
                return;

            var randoConfig = await CreateRandoConfig(profileId, newConfigData);
            await _conn.ExecuteAsync("UPDATE profile SET ConfigId = ? WHERE Id = ?", randoConfig.Id, profileId);
            await CleanRandoConfig(existingConfigId);
        }

        public async Task<RandoConfigDbModel> CreateRandoConfig(int profileId, string data)
        {
            var result = new RandoConfigDbModel()
            {
                Created = DateTime.UtcNow,
                BasedOnProfileId = profileId,
                Data = data
            };
            await _conn.InsertAsync(result);
            return result;
        }

        public async Task CleanRandoConfig(int randoConfigId)
        {
            var a = await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM profile WHERE ConfigId = ?", randoConfigId);
            var b = await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM user WHERE ConfigId = ?", randoConfigId);
            // await _conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM rando WHERE ConfigId = ?", randoConfigId);
            var total = a + b;
            if (total == 0)
                await _conn.ExecuteAsync("DELETE FROM randoconfig WHERE Id = ?", randoConfigId);
        }

        public class ExtendedProfileDbModel : ProfileDbModel
        {
            public string UserName { get; set; } = "";
            public string Data { get; set; } = "";
        }
    }
}
