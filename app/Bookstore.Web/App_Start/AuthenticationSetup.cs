using System.Security.Claims;
using System.Threading.Tasks;
using BobsBookstoreClassic.Data;
using Bookstore.Domain.Customers;
using Bookstore.Web.Helpers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace Bookstore.Web
{
    // Program.cs calls builder.Services.AddBookstoreAuthentication() before Build() and app.UseBookstoreAuthentication()
    // between UseRouting() and UseAuthorization().
    public static class AuthenticationConfig
    {
        private static bool IsCognitoAuthentication()
        {
            return BookstoreConfiguration.GetSetting("Services/Authentication") == "aws";
        }

        public static IServiceCollection AddBookstoreAuthentication(this IServiceCollection services)
        {
            if (IsCognitoAuthentication())
            {
                ConfigureCognitoAuthentication(services);
            }
            else
            {
                // Local mode: the legacy OWIN pipeline registered no authentication handler; the LocalAuthenticationMiddleware sets the user.
                // A cookie scheme is registered as default only so that ASP.NET Core challenge/forbid calls ([Authorize]) have a handler.
                services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                    .AddCookie();
            }

            return services;
        }

        public static IApplicationBuilder UseBookstoreAuthentication(this IApplicationBuilder app)
        {
            app.UseAuthentication();

            if (!IsCognitoAuthentication())
            {
                ConfigureLocalAuthentication(app);
            }

            return app;
        }

        private static void ConfigureLocalAuthentication(IApplicationBuilder app)
        {
            // ICustomerService is resolved per request from HttpContext.RequestServices via InvokeAsync method injection.
            app.UseMiddleware<LocalAuthenticationMiddleware>();
        }

        private static void ConfigureCognitoAuthentication(IServiceCollection services)
        {
            services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
                {
                    options.ClientId = BookstoreConfiguration.GetSetting("Authentication/Cognito/LocalClientId");
                    options.MetadataAddress = BookstoreConfiguration.GetSetting("Authentication/Cognito/MetadataAddress");
                    // ASP.NET Core redeems the authorization code automatically when ResponseType is "code" (legacy RedeemCode = true).
                    options.ResponseType = OpenIdConnectResponseType.Code;
                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.UseTokenLifetime = false;
                    options.SaveTokens = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        NameClaimType = "cognito:username",
                        RoleClaimType = "cognito:groups"
                    };
                    options.Events = new OpenIdConnectEvents
                    {
                        OnRedirectToIdentityProvider = x =>
                        {
                            // Legacy also set Options.RedirectUri; ASP.NET Core derives it from CallbackPath ("/signin-oidc"), so only the protocol message is overridden.
                            x.ProtocolMessage.RedirectUri = x.Request.GetReturnUrl();

                            return Task.CompletedTask;
                        },
                        OnAuthorizationCodeReceived = x =>
                        {
                            if (x.TokenEndpointRequest != null)
                            {
                                x.TokenEndpointRequest.RedirectUri = x.Request.GetReturnUrl();
                            }

                            return Task.CompletedTask;
                        },
                        OnTokenValidated = async x =>
                        {
                            var service = x.HttpContext.RequestServices.GetRequiredService<ICustomerService>();

                            var identity = (ClaimsIdentity)x.Principal.Identity;

                            var dto = new CreateOrUpdateCustomerDto(
                                identity.GetSub(),
                                identity.Name,
                                identity.FindFirst(y => y.Type.Contains("givenname")).Value,
                                identity.FindFirst(y => y.Type.Contains("surname")).Value);

                            await service.CreateOrUpdateCustomerAsync(dto);
                        }
                    };
                });
        }
    }
}