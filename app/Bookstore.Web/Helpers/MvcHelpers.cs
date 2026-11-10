using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bookstore.Web.Helpers
{
    public static class MvcHelpers
    {
        // Extends IHtmlHelper (the type of @Html in ASP.NET Core views) so @Html.GetSelectListForEnum<T>() keeps resolving.
        public static IEnumerable<SelectListItem> GetSelectListForEnum<T>(this IHtmlHelper html, string emptyItem = null)
            where T : Enum
        {
            if (!string.IsNullOrEmpty(emptyItem))
            {
                yield return new SelectListItem()
                {
                    Text = emptyItem
                };
            }
            foreach (var val in Enum.GetValues(typeof(T)))
            {
                yield return new SelectListItem()
                {
                    Text = Enum.GetName(typeof(T), val)
                };
            }

        }
    }
}