using BTS.Models;
using IssueTrackingSystem.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.ServiceModel;
using System.Web.Security;

namespace ITSWebService
{
    class Program
    {
        static void Main()
        {
            using (System.ServiceModel.ServiceHost host = new
                                                     System.ServiceModel.ServiceHost(typeof(ServiceClasses.WebService)))
            {
                host.Open();
                Console.WriteLine("Host started @ " + DateTime.Now.ToString());
                Console.ReadLine();
            }
        }
    }
}
