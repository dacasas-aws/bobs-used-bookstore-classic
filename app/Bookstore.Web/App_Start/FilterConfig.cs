using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Bookstore.Web
{
    public class FilterConfig
    {
        // Called from Program.cs inside AddControllersWithViews(options => ...).
        public static void RegisterGlobalFilters(FilterCollection filters)
        {
            // System.Web.Mvc.HandleErrorAttribute has no ASP.NET Core equivalent; Program.cs configures
            // app.UseExceptionHandler("/Home/Error") (non-development) to render the shared Error view.
            filters.Add(new AuthorizeFilter());
        }
    }
}
