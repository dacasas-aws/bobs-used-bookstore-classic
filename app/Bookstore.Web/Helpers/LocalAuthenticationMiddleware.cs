using System;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Bookstore.Domain.Customers;

namespace Bookstore.Web.Helpers
{
    // Activated by AuthenticationConfig.UseBookstoreAuthentication (Program.cs) when Services:Authentication is not "aws".
    public class LocalAuthenticationMiddleware
    {
        private const string UserId = "FB6135C7-1464-4A72-B74E-4B63D343DD09";

        private readonly RequestDelegate _next;

        public LocalAuthenticationMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        // ICustomerService is resolved per request (method injection) so a scoped service is not captured by this singleton middleware.
        public async Task InvokeAsync(HttpContext context, ICustomerService customerService)
        {
            if (context.Request.Path.Value?.StartsWith("/Authentication/Login") == true)
            {
                var principal = CreateClaimsPrincipal(context);

                await SaveCustomerDetailsAsync(principal, customerService);

                context.Response.Cookies.Append("LocalAuthentication", string.Empty, new CookieOptions { Expires = DateTime.Now.AddDays(1) });

                context.Response.Redirect("/");
            }
            else if (context.Request.Cookies.ContainsKey("LocalAuthentication"))
            {
                var principal = CreateClaimsPrincipal(context);

                await SaveCustomerDetailsAsync(principal, customerService);

                await _next(context);
            }
            else
            {
                await _next(context);
            }
        }

        private ClaimsPrincipal CreateClaimsPrincipal(HttpContext context)
        {
            var identity = new ClaimsIdentity("Application");

            identity.AddClaim(new Claim(ClaimTypes.Name, "bookstoreuser"));
            identity.AddClaim(new Claim("nameidentifier", UserId));
            identity.AddClaim(new Claim("given_name", "Bookstore"));
            identity.AddClaim(new Claim("family_name", "User"));
            identity.AddClaim(new Claim(ClaimTypes.Role, "Administrators"));

            var principal = new ClaimsPrincipal(identity);

            context.User = principal;

            return principal;
        }

        private static async Task SaveCustomerDetailsAsync(ClaimsPrincipal principal, ICustomerService customerService)
        {
            var identity = (ClaimsIdentity)principal.Identity;

            var dto = new CreateOrUpdateCustomerDto(
                identity.FindFirst("nameidentifier").Value,
                identity.Name,
                identity.FindFirst("given_name").Value,
                identity.FindFirst("family_name").Value);

            await customerService.CreateOrUpdateCustomerAsync(dto);
        }
    }
}