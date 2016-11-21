using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;

namespace IssueTrackingSystem.Common
{
    public class LogErrorAttribute :  HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            base.OnException(filterContext);

            ErrorTracker tracker = new ErrorTracker();
            tracker.LogError(filterContext.Exception.Message);
        }
    }
}