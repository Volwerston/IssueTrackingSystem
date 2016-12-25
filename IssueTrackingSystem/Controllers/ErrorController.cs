using IssueTrackingSystem.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace IssueTrackingSystem.Controllers
{
    public class ErrorController : Controller
    {
        // GET: Error
        public ActionResult PageNotFound()
        {
            ErrorTracker.LogError("Not found error", "404 - Page Not Found");

            return View();
        }

    }
}
