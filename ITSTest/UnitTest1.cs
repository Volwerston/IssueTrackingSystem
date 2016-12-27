using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Web.Mvc;
using System.Web;

namespace ITSTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void Test_LogIn()
        {
            // Arrange
            BTS.Controllers.BtsController controller = new BTS.Controllers.BtsController();

            // Act
            ActionResult result = controller.LogIn("FreZZz", "somepassw", true);

            // Assert

            Assert.IsNotNull(result);
        }

        [TestMethod]
        public void Test_SignUp()
        {
            // Arrange
            BTS.Controllers.BtsController controller = new BTS.Controllers.BtsController();

            // Act
            ActionResult result = controller.SignUp();

            // Assert

            Assert.IsNotNull(result);
        }


        [TestMethod]

        public void Test_BugDescriptionPage()
        {
            // Arrange
            BTS.Controllers.BtsController controller = new BTS.Controllers.BtsController();

            // Act
            controller.Session["Username"] = "FreZZz";
            controller.Session["Status"] = "Admin";
            controller.Session["Id"] = 1;
            controller.Session["Confirmed"] = true.ToString();

            HttpContext.Current.Session["Username"] = "FreZZz";
            HttpContext.Current.Session["Status"] = "Admin";
            HttpContext.Current.Session["Id"] = 1;
            HttpContext.Current.Session["Confirmed"] = true.ToString();

            ActionResult result = controller.BugDescriptionPage("Facebook", 2007);

            // Assert

            Assert.IsNotNull(result);
        }
    }
}
