using Microsoft.AspNetCore.Http;
using System;
using System.Net;

namespace Bookstore.Web.Helpers
{
    public static class HttpContextExtensions
    {
        public static string GetShoppingCartCorrelationId(this HttpContext context)
        {
            var CookieKey = "ShoppingCartId";

            var cookieOptions = new CookieOptions
            {
                Expires = DateTime.Now.AddYears(1),
                Path = "/"
            };

            string shoppingCartClientId = context.Request.Cookies[CookieKey];

            //var shoppingCartClientId = context.Request.Cookies[CookieKey].Value;

            if (string.IsNullOrWhiteSpace(shoppingCartClientId))
            {
                shoppingCartClientId = context.User.Identity.IsAuthenticated ? context.User.GetSub() : Guid.NewGuid().ToString();
            }

            // The legacy code built cookieOptions but wrote the cookie without them (a session cookie);
            // that behavior is preserved here.
            context.Response.Cookies.Append(CookieKey, shoppingCartClientId);

            return shoppingCartClientId;
        }
    }
}