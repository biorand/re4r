using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IntelOrca.Biohazard.BioRand.Server.Models;
using IntelOrca.Biohazard.BioRand.Server.Services;
using Microsoft.Extensions.Logging;

namespace IntelOrca.Biohazard.BioRand.Server
{
    internal class UserTagModifier
    {
        private readonly DatabaseService _db;
        private readonly Dictionary<string, UserTagDbModel> _cache;
        private readonly UserTagDbModel[] _orig;
        private readonly List<UserTagDbModel> _curr;
        private readonly ILogger _logger;

        public UserDbModel User { get; }
        public int UserId => User.Id;

        public static async Task<UserTagModifier> CreateAsync(DatabaseService db, UserDbModel user, ILogger logger)
        {
            var cache = (await db.GetUserTags()).ToDictionary(x => x.Label);
            var curr = await db.GetUserTagsForUser(user.Id);
            return new UserTagModifier(db, user, cache, curr, logger);
        }

        private UserTagModifier(
            DatabaseService db,
            UserDbModel user,
            Dictionary<string, UserTagDbModel> cache,
            IEnumerable<UserTagDbModel> curr,
            ILogger logger)
        {
            _db = db;
            User = user;
            _cache = cache;
            _orig = [.. curr];
            _curr = [.. curr];
            _logger = logger;
        }

        public async Task ApplyAsync()
        {
            var oldIds = _orig.Select(x => x.Id).Order().ToArray();
            var newIds = _curr.Select(x => x.Id).Order().ToArray();
            if (!oldIds.SequenceEqual(newIds))
            {
                await _db.UpdateUserTagsForUser(UserId, _curr);

                var oldTags = string.Join(",", _orig.Select(x => x.Label).Order());
                var newTags = string.Join(",", _curr.Select(x => x.Label).Order());
                _logger.LogInformation("Updated user {UserId}[{UserName}] tags from {OldTags} to {NewTags}", User.Id, User.Name, oldTags, newTags);
            }
        }

        public bool IsValid(string label)
        {
            return GetTag(label) != null;
        }

        public bool Contains(string label)
        {
            var tag = GetTag(label);
            if (tag == null)
                return false;

            return _curr.Any(x => x.Id == tag.Id);
        }

        public void Set(params string[] labels)
        {
            _curr.Clear();
            foreach (var label in labels)
            {
                Add(label);
            }
        }

        public void Add(string label)
        {
            var tag = GetTag(label);
            if (tag != null)
            {
                if (!_curr.Any(x => x.Id == tag.Id))
                {
                    _curr.Add(tag);
                }
            }
        }

        public void Remove(string label)
        {
            var tag = GetTag(label);
            if (tag != null)
            {
                _curr.RemoveAll(x => x.Id == tag.Id);
            }
        }

        private UserTagDbModel? GetTag(string label)
        {
            _cache.TryGetValue(label, out var result);
            return result;
        }
    }
}
