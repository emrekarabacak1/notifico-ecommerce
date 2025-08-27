using Notifico.Data;
using Notifico.Models;

namespace Notifico.Helpers
{
    public static class SessionHelper
    {
        public static User GetCurrentUser(this IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            var userName = httpContextAccessor.HttpContext.Session.GetString("UserName");
            if(string.IsNullOrEmpty(userName))
            {
                return null;
            }

            return context.Users.FirstOrDefault(u => u.UserName == userName);
        }

        public static string GetCurrentUserRole(this IHttpContextAccessor httpContextAccessor, ApplicationDbContext context)
        {
            var user = GetCurrentUser(httpContextAccessor, context);
            return user?.Role;
        }
    }
}
