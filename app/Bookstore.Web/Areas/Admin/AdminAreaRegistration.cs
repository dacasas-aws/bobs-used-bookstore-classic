using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Bookstore.Web.Areas
{
    // ASP.NET Core has no AreaRegistration base class: Admin controllers carry [Area("Admin")] (AdminAreaControllerBase)
    // and the area route is registered on the endpoint route builder from Program.cs, before the default route.
    public class AdminAreaRegistration
    {
        public string AreaName 
        {
            get 
            {
                return "Admin";
            }
        }

        public static void RegisterArea(IEndpointRouteBuilder routes) 
        {
            routes.MapControllerRoute(
                name: "Admin_default",
                pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
            );
        }
    }
}
