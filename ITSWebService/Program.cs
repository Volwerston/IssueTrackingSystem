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
            ServiceHost host = new ServiceHost(typeof(ServiceClasses.WebService), new Uri("http://localhost:1050/TestService"));
            host.AddServiceEndpoint(typeof(ServiceClasses.IService), new BasicHttpBinding(), "");
            host.Open();
            Console.WriteLine("Server started at {0} \n", DateTime.Now);
            Console.ReadLine();

            host.Close();
        }
    }
}
