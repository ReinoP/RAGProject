namespace AIproject.Data
{
    using System.Security.Claims;
    using AIproject.Interfaces;
    using Microsoft.AspNetCore.Http;

    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserContext(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }


        public string? GetUserId()
        {
         var userId = _httpContextAccessor.HttpContext?
                .User?
                .FindFirstValue(ClaimTypes.NameIdentifier);

            if (String.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User ID is not set in the context. Cannot process document without user association.");
            }

            return userId;
        }

        public string? GetUserName()
        {
            var usernName = _httpContextAccessor.HttpContext?
                   .User?
                   .FindFirstValue(ClaimTypes.Name);

            if (String.IsNullOrEmpty(usernName))
            {
                throw new UnauthorizedAccessException("User ID is not set in the context. Cannot process document without user association.");
            }

            return usernName;
        }
    }
}
