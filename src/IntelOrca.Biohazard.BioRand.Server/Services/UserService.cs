﻿using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using IntelOrca.Biohazard.BioRand.Server.Extensions;
using IntelOrca.Biohazard.BioRand.Server.Models;

namespace IntelOrca.Biohazard.BioRand.Server.Services
{
    public class UserService
    {
        public object GetUser(UserDbModel user, UserTagDbModel[]? tags = null, TwitchDbModel? twitchModel = null)
        {
            return new
            {
                user.Id,
                user.Name,
                Created = user.Created.ToUnixTimeSeconds(),
                user.Email,
                user.Role,
                Tags = tags?.Select(x => x.Label),
                AvatarUrl = twitchModel == null ? GetAvatarUrl(user.Email) : twitchModel.TwitchProfileImageUrl,
                user.ShareHistory,
                user.KofiEmail,
                KofiEmailVerified = user.KofiEmailVerification == null,
                user.KofiMember,
                twitch = twitchModel == null ? null : new
                {
                    DisplayName = twitchModel.TwitchDisplayName,
                    ProfileImageUrl = twitchModel.TwitchProfileImageUrl,
                    IsSubscribed = twitchModel.IsSubscribed,
                }
            };
        }

        protected static string GetAvatarUrl(string email)
        {
            var inputBytes = Encoding.ASCII.GetBytes(email.ToLower());
            var hashBytes = SHA256.HashData(inputBytes);
            var hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
            return $"https://www.gravatar.com/avatar/{hashString}";
        }
    }
}
