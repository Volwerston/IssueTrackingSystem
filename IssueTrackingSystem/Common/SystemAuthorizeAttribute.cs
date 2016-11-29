using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace IssueTrackingSystem.Common
{
    public class SystemAuthorizeAttribute : AuthorizeAttribute
    {
        private bool hasRole(string userRole, string suitableRoles)
        {
            string[] rolesArr = suitableRoles.Split(',');
            return rolesArr.Any(x => x == userRole);
        }
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            bool accessAllowed = false;

            if (HttpContext.Current.Session["Username"] != null)
            {
                if (hasRole(HttpContext.Current.Session["Status"].ToString(), Roles) && Convert.ToBoolean(HttpContext.Current.Session["Confirmed"].ToString()))
                {
                    accessAllowed = true;
                }
                else
                {
                    HttpContext.Current.Session.Remove("Username");
                    HttpContext.Current.Session.Remove("Status");
                    HttpContext.Current.Session.Remove("Id");
                    HttpContext.Current.Session.Remove("Confirmed");
                }
            }

            if (!accessAllowed)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary(
                        new { controller = "Bts", action = "Index" }
                        )
                    );
            }
        }
    }
}