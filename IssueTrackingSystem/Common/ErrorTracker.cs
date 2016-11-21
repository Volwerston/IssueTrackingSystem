using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.Globalization;
using System.Text;

namespace IssueTrackingSystem.Common
{
    public class ErrorTracker
    {
        public void LogError(string errorData)
        {
            
            string year = DateTime.Now.Year.ToString();
            string month = DateTime.Now.ToString("MMMM", CultureInfo.InvariantCulture);

            if (!Directory.Exists("D:/IssueTrackingSystem/Error log/" + year + "/" + month))
            {
                Directory.CreateDirectory("D:/IssueTrackingSystem/Error log/" + year + "/" + month);
            }

            string filePath = "D:/IssueTrackingSystem/Error log/" + year + "/" + month + "/ErrorLog.txt";

            FileInfo errorLog = new FileInfo(filePath);

            if (!errorLog.Exists)
            {
                errorLog.Create();
            }

            List<string> linesToAppend = new List<string>();
            linesToAppend.Add("Controller: " + HttpContext.Current.Request.RequestContext.RouteData.Values["controller"].ToString());
            linesToAppend.Add("Action: " + HttpContext.Current.Request.RequestContext.RouteData.Values["action"].ToString());
            linesToAppend.Add("Error: " + errorData + "\n");
            linesToAppend.Add("Date: " + DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));
            linesToAppend.Add("Time: " + DateTime.Now.ToString("hh:mm:ss zz tt", CultureInfo.InvariantCulture));
            linesToAppend.Add("--------------------- ");

            File.AppendAllLines(filePath, linesToAppend);
        }
    }
}