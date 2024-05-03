using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EmbedIO.WebApi;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Models;
using IntelOrca.Biohazard.BioRand.RE4R.Server.Services;
using Swan;

namespace IntelOrca.Biohazard.BioRand.RE4R.Server.Controllers
{
    internal class BaseController : WebApiController
    {
        private readonly DatabaseService _db;

        public BaseController(DatabaseService db)
        {
            _db = db;
        }

        protected string CreateUrl(string path)
        {
            var baseUrl = Request.Url.AbsoluteUri.TrimEnd('/');
            return $"{baseUrl}/${path}";
        }

        protected async Task<UserDbModel?> GetAuthorizedUserAsync(UserRoleKind minimumRole = UserRoleKind.EarlyAccess)
        {
            var authorization = HttpContext.Request.Headers["Authorization"];
            if (!string.IsNullOrEmpty(authorization))
            {
                var parts = authorization.Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    var type = parts[0];
                    var token = parts[1];
                    if (type == "Bearer")
                    {
                        var user = await _db.GetUserByToken(token);
                        if (user != null && user.Role < minimumRole)
                        {
                            return null;
                        }
                        return user;
                    }
                }
            }
            return null;
        }

        protected object EmptyResult()
        {
            return new object { };
        }

        protected object ErrorResult(HttpStatusCode code)
        {
            Response.StatusCode = (int)code;
            return EmptyResult();
        }

        protected object NotFoundResult() => ErrorResult(HttpStatusCode.NotFound);
        protected object UnauthorizedResult() => ErrorResult(HttpStatusCode.Unauthorized);

        protected object Failure(HttpStatusCode statusCode, string message)
        {
            Response.StatusCode = (int)statusCode;
            return new
            {
                success = false,
                message
            };
        }

        protected object GetUser(UserDbModel user)
        {
            return new
            {
                user.Id,
                user.Name,
                Created = user.Created.ToUnixEpochDate(),
                user.Email,
                user.Role,
                AvatarUrl = GetAvatarUrl(user.Email),
                user.ShareHistory
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
