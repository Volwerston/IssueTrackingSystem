using BTS.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using WebApplication7;

namespace IssueTrackingSystem
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        /*
        protected void Application_Error()
        {
            Exception ex = Server.GetLastError();
            HttpException httpex = ex as HttpException;

            RouteData data = new RouteData();

            data.Values.Add("controller", "Bts");

            if (httpex == null)
            {
                data.Values.Add("action", "Index");
            }
            else
            {
                switch (httpex.GetHttpCode())
                {
                    case 404:
                        data.Values.Add("action", "NotFoundErrorPage");
                        break;
                    default:
                        data.Values.Add("action", "GeneralErrorPage");
                        break;
                }
            }

            Server.ClearError();
            Response.TrySkipIisCustomErrors = true;
            IController controller = new BtsController();
            controller.Execute(new RequestContext(new HttpContextWrapper(Context), data));
        }
        */
    }
}
