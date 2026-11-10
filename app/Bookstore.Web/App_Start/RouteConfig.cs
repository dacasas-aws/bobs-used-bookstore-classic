using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Bookstore.Web
{
    public class RouteConfig
    {
        // Called from Program.cs after UseRouting()/UseAuthorization(), following AdminAreaRegistration.RegisterArea.
        public static void RegisterRoutes(IEndpointRouteBuilder routes)
        {
            // routes.IgnoreRoute("{resource}.axd/{*pathInfo}") is dropped: ASP.NET Core has no .axd handlers to ignore.

            // The MVC5 namespaces constraint ("Bookstore.Web.Controllers") is dropped: conventional routes
            // without an area value only match controllers that are not decorated with [Area].
            routes.MapControllerRoute(
                name: "Default",
                pattern: "{controller=Home}/{action=Index}/{id?}"
            );
        }
    }
}
