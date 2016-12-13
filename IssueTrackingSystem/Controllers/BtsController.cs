using BTS.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using IssueTrackingSystem.Common;
using System.Text;
using System.ServiceModel;
using ServiceClasses;
using System.Data;

namespace BTS.Controllers
{
    [LogError]
    public class BtsController : Controller
    {
        private static Uri tcpUri = new Uri("http://localhost:1050/TestService");
        private static EndpointAddress address = new EndpointAddress(tcpUri);
        private static BasicHttpBinding binding = new BasicHttpBinding();
        

        private static ChannelFactory<IService> factory = new ChannelFactory<IService>(binding, address);
        private IService service = factory.CreateChannel();
        // GET: Bts

        public ActionResult Index(string message, string messageType)
        {
            binding.MaxReceivedMessageSize = Int32.MaxValue;

            if (Session["Username"] != null && Session["Status"] != null)
            {
                return RedirectToAction("Main", "Bts", new { message = message, messageType = messageType });
            }
            else
            {
                if (Request.Cookies["username"] != null && Request.Cookies["password"] != null)
                {
                        User u = service.getAuthenticatedUser(Request.Cookies["username"].Value, Request.Cookies["password"].Value);

                        if (u.Id != -1)
                        {
                            Session["Username"] = Request.Cookies["username"].Value;
                            Session["Status"] = u.Status;
                            Session["Confirmed"] = u.Confirmed.ToString();
                            Session["Id"] = u.Id;

                            return RedirectToAction("Main");
                        }
                    }

                if(message != null && messageType != null)
                {
                    TempData["message"] = message;
                    TempData["status"] = messageType;
                }

                return View();
            }
        }

        [HttpPost]
        public ActionResult ConfirmUser(int userId)
        {
            bool toReturn = false;

                toReturn = service.ConfirmUser(userId);


            return RedirectToAction("ExternalAccountPage", "Bts", new { id = userId });
        }

        public ActionResult LogIn(string Login, string Password, bool rememberMe)
        {

            bool authenticated = false;

                User u = service.getAuthenticatedUser(Login, Password);
                if (u.Id != -1)
                {
                    Session["Username"] = Login;
                    Session["Status"] = u.Status;
                    Session["Id"] = u.Id;
                    Session["Confirmed"] = u.Confirmed.ToString();

                    if (rememberMe)
                    {
                        HttpCookie nick = new HttpCookie("username");
                        nick.Expires = DateTime.Now.AddDays(1d);
                        nick.Value = Login;
                        Response.SetCookie(nick);

                        HttpCookie pass = new HttpCookie("password");
                        pass.Expires = DateTime.Now.AddDays(1d);
                        pass.Value = Password;
                        Response.SetCookie(pass);
                    }

                    authenticated = true;
                }


            if (!authenticated)
            {
                TempData["status"] = "Warning";
                TempData["message"] = "User name and/or password are incorrect";
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Main");
            }
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult SignOut()
        {
            Session.Remove("Username");
            Session.Remove("Status");
            Session.Remove("Id");
            Session.Remove("Confirmed");

            HttpCookie nick = new HttpCookie("username");
            nick.Expires = DateTime.Now.AddDays(-1d);
            Response.SetCookie(nick);

            HttpCookie pass = new HttpCookie("password");
            pass.Expires = DateTime.Now.AddDays(-1d);
            Response.SetCookie(pass);

            return RedirectToAction("Index");
        }

        public ActionResult SignUp()
        {
            User user = new User();
            return View(user);
        }

        [HttpPost]
        public ActionResult SignUp(User u, HttpPostedFileBase userAvatar, string confirmPassword, string birthDate)
        {

            if (birthDate != "")
            {
                u.BirthDate = DateTime.ParseExact(birthDate, "dd.MM.yyyy",
                                       System.Globalization.CultureInfo.InvariantCulture);
            }

            if (ModelState.IsValid)
            {
                if (u.Password == confirmPassword)
                {
                    if (userAvatar != null)
                    {
                        u.Avatar = new byte[userAvatar.ContentLength];
                        userAvatar.InputStream.Read(u.Avatar, 0, userAvatar.ContentLength);
                    }

                        string addResult = service.AddAccount(u);

                        if (addResult == "Success")
                        {

                            int insertedId = service.getUsers().Where(x => x.Nickname == u.Nickname).ToList().First().Id;

                            string text = "Please confirm the personality of new user <a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { @id = insertedId }) + "\">" + u.Nickname + "</a>";

                            List<User> admins = service.getUsers().Where(x => x.Status == "Admin").ToList();

                            foreach (var admin in admins)
                            {
                                service.WriteMessage(admin.Nickname, u.Nickname, text);
                                service.InformAboutNotification(admin);
                            }
                        }
                        else if (addResult == "Fail")
                        {
                            TempData["status"] = "Error";
                            TempData["message"] = "Internal error. Try again. The problem may be with your birth date";
                        }
                        else
                        {
                            TempData["status"] = "Warning";
                            TempData["message"] = "User with the same nickname or e-mail has already been registered";
                        }

                        if (addResult == "Success")
                        {
                            return RedirectToAction("Index", "Bts",
                                new { messageType = "Success", message = "Account has been successfully registered" });
                        }
                    else
                    {
                        TempData["status"] = "Error";
                        TempData["message"] = "Internal error. Try again";
                    }
                }
                else
                {
                    TempData["status"] = "Warning";
                    TempData["message"] = "Passwords don't match";
                }
            }

            return View();
        }

        public ActionResult PasswordUpdate()
        {
            return View();
        }

        [HttpPost]
        public ActionResult PasswordUpdate(string email)
        {
            if (email == "")
            {
                TempData["status"] = "Notification";
                TempData["message"] = "Please fill in the input form";
            }
            else
            {
                    bool letterSent = false;

                    if (email != "")
                    {
                        letterSent = service.isEmailSent(email);
                    }

                    if(letterSent)
                    {
                        return RedirectToAction("Index", "Bts",
                            new { messageType = "Success", message = "Letter was successfully sent" });
                    }
                    else
                    {
                        TempData["message"] = "Error";
                        TempData["message"] = "Letter was not sent. Try again";
                    }
            }
            return View();
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult Main(string message, string messageType)
        {
            List<Category> categories = new List<Category>();

            categories = service.getCategories();

            if(messageType != null && message != null)
            {
                TempData["message"] = message;
                TempData["status"] = messageType;
            }

            return View(categories);
        }

        [HttpPost]
        public string GetProjectsByName(string[] prName)
        {
            List<Project> toReturn = null;

                toReturn = service.GetProjectsByName(prName[0]);


            List<string> imageUrls = new List<string>();

            if (toReturn != null && toReturn.Count() != 0)
            {
                foreach (Project proj in toReturn)
                {
                    var base64 = Convert.ToBase64String(proj.Logo);
                    string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                    imageUrls.Add(toAdd);
                    proj.Logo = null;
                }
            }

            string returnStringProjects = JsonConvert.SerializeObject(toReturn);

            if (toReturn != null && toReturn.Count() != 0)
            {
                string returnStringImgs = JsonConvert.SerializeObject(imageUrls);

                returnStringProjects = returnStringProjects.Substring(0, returnStringProjects.Count() - 1);
                returnStringProjects += ",";
                returnStringImgs = returnStringImgs.Substring(1);

                returnStringProjects += returnStringImgs;
            }

            return returnStringProjects;
        }

        [HttpPost]
        public string GetProjectsByCategories(int[] categories)
        {
            List<Project> toReturn = null;

            toReturn = service.GetProjectsByCategories(categories);


            List<string> imageUrls = new List<string>();

            if (toReturn != null && toReturn.Count() != 0)
            {
                foreach (Project proj in toReturn)
                {
                    var base64 = Convert.ToBase64String(proj.Logo);
                    string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                    imageUrls.Add(toAdd);
                    proj.Logo = null;
                }
            }

            string returnStringProjects = JsonConvert.SerializeObject(toReturn);

            if (toReturn != null && toReturn.Count() != 0)
            {
                string returnStringImgs = JsonConvert.SerializeObject(imageUrls);

                returnStringProjects = returnStringProjects.Substring(0, returnStringProjects.Count() - 1);
                returnStringProjects += ",";
                returnStringImgs = returnStringImgs.Substring(1);

                returnStringProjects += returnStringImgs;
            }

            return returnStringProjects;
        }

        [SystemAuthorize(Roles="Project Manager,Admin")]
        public ActionResult AddProject()
        {
            Project proj = new Project();

                List<Category> categories = service.getCategories();


                if (categories != null)
                { 

                    List<SelectListItem> categoryItems = new List<SelectListItem>();

                    foreach(Category category in categories)
                    {
                        SelectListItem item = new SelectListItem()
                        {
                            Text = category.Title,
                            Value = category.Title
                        };

                        categoryItems.Add(item);
                    }

                    ViewBag.Categories = categoryItems;

                    return View(proj);
                }

            return View("PageNotFound", "Error");
        }

        [HttpPost]
        public ActionResult AddProject(Project proj, HttpPostedFileBase aLogo, FormCollection collection)
        {

            List<Category> categories = null;

            if (ModelState.IsValid)
            {
                proj.PmId = int.Parse(Session["Id"].ToString());

                if (collection["projectCategories"] != null)
                {
                    List<string> items = collection["projectCategories"].ToString().Split(',').ToList();

                    categories = service.getCategories();
                        
                    if (categories != null)
                    {

                        List<int> categoryIds = new List<int>();

                        foreach (string categoryName in items)
                        {
                            int categoryId = categories.Where(x => x.Title == categoryName).Single().Id;

                            categoryIds.Add(categoryId);
                        }

                        if (aLogo != null)
                        {
                            byte[] img = new byte[aLogo.ContentLength];
                            aLogo.InputStream.Read(img, 0, aLogo.ContentLength);

                            proj.Logo = img;
                        }

                        bool success = false;

                            success = service.AddProject(proj, categoryIds);


                        if (success)
                        {
                            return RedirectToAction("InternalAccountPage", "Bts", 
                                new { messageType = "Success", message = "New project has been successfully added" });
                        }
                    }
                }
            }

                categories = service.getCategories();

                if (categories != null)
                {
                    List<SelectListItem> categoryItems = new List<SelectListItem>();

                    foreach (Category category in categories)
                    {
                        SelectListItem item = new SelectListItem()
                        {
                            Text = category.Title,
                            Value = category.Title
                        };

                        categoryItems.Add(item);
                    }

                    ViewBag.Categories = categoryItems;
                    return View();
                }

                return View("PageNotFound", "Error");
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult ShowProject(string name, string message, string messageType)
        {
            bool projectFound = false;

            Project project = new Project();

            List<Bug> projectBugs = null;

                List<Project> chosenProjects = service.GetProjectsByName(name);

                if (chosenProjects.Count > 0)
                {
                    projectFound = true;
                    project = chosenProjects[0];
                }

                if (project != null)
                {
                    List<User> invitedDevs = null;
                    List<User> developers = service.GetDevelopersOfProject(name, out invitedDevs);
                    List<SelectListItem> developerList = null;

                    ViewBag.InvitedDevs = invitedDevs;
                    ViewBag.ApprovedDevelopers = developers;
                    

                    if (developers != null)
                    {
                        developerList = new List<SelectListItem>();

                        foreach (User dev in developers)
                        {
                            SelectListItem item = new SelectListItem() { Text = dev.Name + " " + dev.Surname, Value = dev.Id.ToString() };
                            developerList.Add(item);
                        }
                    }

                    ViewBag.Developers = developerList;
                }

                projectBugs = service.GetProjectBugs(project);


            if (!projectFound)
            {
                return RedirectToAction("PageNotFound", "Error");
            }

            if (projectBugs != null)
            {
                ViewBag.ProjectBugs = projectBugs;
            }
            else
            {
                ViewBag.ProjectBugs = new List<Bug>();
            }

            ViewBag.Pm = project.PmId;

            if(messageType != null && message != null)
            {
                TempData["message"] = message;
                TempData["status"] = messageType;
            }

            return View(project);
        }

        public ActionResult JoinProject(int id, string projectName)
        {
            service.ApproveDeveloperForProject(projectName, id);

            return RedirectToAction("ShowProject", "Bts", new { name = projectName });
        }

        public ActionResult ChangePassword()
        {
                if (!service.IsPasswordResetLinkValid(Request.QueryString["uid"]))
                {
                    TempData["status"] = "Error";
                    TempData["message"] = "Password Reset link has expired or is invalid";
                }

            return View();
        }

        [HttpPost]
        public string FindDevelopersForProject(int id, string[] names, string projectName)
        {
            List<User> toReturn = new List<User>();
            List<string> imgPaths = null;
            User anonymous = null;
            List<string> userNames = names.Distinct().ToList();
            userNames.Remove("");

                anonymous = service.getUsers().Where(x => x.Nickname == "Anonymous").ToList()[0];

                List<User> invitedDevs = null;
                List<User> currDevelopers = service.GetDevelopersOfProject(projectName, out invitedDevs);

                if(invitedDevs.Count() > 0)
                {
                    if(currDevelopers == null)
                    {
                        currDevelopers = new List<User>();
                    }

                    currDevelopers.AddRange(invitedDevs);
                }

                while (toReturn.Count() < 5)
                {
                    int prevCount = toReturn.Count();
                    List<User> buf = null;

                    buf = service.searchForUsers(id, userNames, new List<string> { "Developer" });

                    if(buf != null)
                    {
                        id = buf.Last().Id;

                        buf = buf.Where(x => x.Confirmed).ToList();
                    }

                    if(buf == null)
                    {
                        break;
                    }
                    else
                    {
                        foreach(User newDeveloper in buf)
                        {
                            bool elFound = false;

                            if (currDevelopers != null)
                            {
                                foreach (User existingDeveloper in currDevelopers)
                                {
                                    if (existingDeveloper.Id == newDeveloper.Id)
                                    {
                                        elFound = true;
                                        break;
                                    }
                                }
                            }

                            if(!elFound)
                            {
                                toReturn.Add(newDeveloper);

                                if(toReturn.Count() == 5)
                                {
                                    break;
                                }
                            }
                        }
                    }
                }

            if (toReturn != null)
            {
                int size = toReturn.Count();

                imgPaths = new List<string>();

                for (int i = 0; i < size; ++i)
                {
                    if (toReturn[i].Avatar != null)
                    {
                        var base64 = Convert.ToBase64String(toReturn[i].Avatar);
                        string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                        imgPaths.Add(toAdd);
                        toReturn[i].Avatar = null;
                    }
                    else
                    {
                        User u = new User();
                        var base64 = Convert.ToBase64String(anonymous.Avatar);
                        string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                        imgPaths.Add(toAdd);
                    }
                }
            }

            if(toReturn.Count() == 0)
            {
                toReturn = null;
            }

            string toReturnString = JsonConvert.SerializeObject(toReturn);

            if (toReturn != null)
            {
                toReturnString = toReturnString.Substring(0, toReturnString.Count() - 1);
                toReturnString += ",";

                string toReturnStringImgs = JsonConvert.SerializeObject(imgPaths);
                toReturnStringImgs = toReturnStringImgs.Substring(1);

                toReturnString += toReturnStringImgs;
            }

            return toReturnString;
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult InviteDevelopers(string projectName)
        {
            if (projectName != null)
            {
                ViewBag.ProjectName = projectName;

                return View();
            }

            return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        public string GetUserNotifications(string receiver)
        {
            List<Notification> toReturn = null;

                toReturn = service.GetNotificationsOfUser(receiver);

            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public string InviteDevelopers(int devId, string projectName)
        {
            bool toReturn = false;

                toReturn = service.InviteDeveloperToProject(projectName, devId);

                if(toReturn)
                {
                    Project proj = service.GetProjectsByName(projectName).ToList().SingleOrDefault();

                    User dev = service.getUsers().Where(x => x.Id == devId).ToList().SingleOrDefault();

                    User admin = service.getUsers().Where(x => x.Status == "Admin").ToList()[0];

                    string text = "You were invited to the <a href=\"" + Url.Action("ShowProject", "Bts", new { name = proj.Name }) + "\">" + projectName + "</a> project. Please go to the project page to accept invitation";

                    service.WriteMessage(dev.Nickname, admin.Nickname, text);
                    service.InformAboutNotification(dev);
                }

   
            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public ActionResult ChangePassword(string password, string confirmPassword)
        {
            if (password == confirmPassword)
            {
                    bool passwordChanged = service.ChangeUserPassword(Request.QueryString["uid"], password);

                    if(!passwordChanged)
                    {
                        TempData["status"] = "Error";
                        TempData["message"] = "Password Reset link has expired or is invalid";
                    }

                    service.deleteExpiredRecords();

                    if(passwordChanged)
                    {
                        return RedirectToAction("Index", "Bts", 
                            new { messageType = "Success", message = "Password successfully changed" });
                    }
            }
            else
            {
                TempData["status"] = "Warning";
                TempData["message"] = "Passwords don't match";
            }

            return View();
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult ReportBug(int projectId, string projectName)
        {
            TempData["projId"] = projectId;
            TempData["projname"] = projectName;
            Bug bug = new Bug();
            return View(bug);
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult FindUsers()
        {
            List<string> statuses = service.getUsers().Select(x => x.Status).ToList().Distinct().ToList();

            ViewBag.StatusList = statuses;
            return View();
        }

        [HttpPost]
        public string RemoveDevelopersFromProject(string projName, int[] toErase)
        {
            bool toReturn = false;

            if (toErase != null)
            {
                    toReturn = service.RemoveDevsFromProject(projName, toErase.ToList());
            }

           if(toReturn && toErase != null)
            {
                    Project proj = service.GetProjectsByName(projName).Single();
                    User pm = service.getUsers().Where(x => x.Id == proj.PmId).Single();

                    foreach (int devId in toErase)
                    {
                        User user = service.getUsers().Where(x => x.Id == devId).Single();


                        string text1 = "Project manager dismissed you from project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projName }) + "\">"
                            + projName + "</a> . Do not despair and move on!";
                        service.WriteMessage(user.Nickname, pm.Nickname, text1);
                    }

            }

            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public string FindUsers(int id, string[]  names, string[] statusItems)
        {
            List<User> toReturn = null;
            List<string> userStatuses = null;
            List<string> userNames = names.Distinct().ToList();
            List<string> imgPaths = null;
            User anonymous = null;
            userNames.Remove("");

            if (statusItems != null)
            {
                userStatuses = statusItems.ToList();
            }

                toReturn = service.searchForUsers(id, userNames, userStatuses);
                anonymous = service.getUsers().Where(x => x.Nickname == "Anonymous").ToList()[0];


            if (toReturn != null)
            {
                int size = toReturn.Count();

                imgPaths = new List<string>();

                for (int i = 0; i < size; ++i)
                {
                    if (toReturn[i].Avatar != null)
                    {
                        var base64 = Convert.ToBase64String(toReturn[i].Avatar);
                        string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                        imgPaths.Add(toAdd);
                        toReturn[i].Avatar = null;
                    }
                    else
                    {
                        User u = new User();
                        var base64 = Convert.ToBase64String(anonymous.Avatar);
                        string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                        imgPaths.Add(toAdd);
                    }
                }
            }

            string toReturnString = JsonConvert.SerializeObject(toReturn);

            if (toReturn != null)
            {
                toReturnString = toReturnString.Substring(0, toReturnString.Count() - 1);
                toReturnString += ",";

                string toReturnStringImgs = JsonConvert.SerializeObject(imgPaths);
                toReturnStringImgs = toReturnStringImgs.Substring(1);

                toReturnString += toReturnStringImgs;
            }

            return toReturnString;
        }

        [HttpPost]
        public string RemoveNotification(int id)
        {
            bool toReturn = false;

            toReturn = service.RemoveNotification(id);


            return JsonConvert.SerializeObject(toReturn);
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult InternalAccountPage(string message, string messageType)
        {
            User user = new User();


                user = service.getUsers().Where(x => x.Nickname == Session["Username"].ToString()).Single();
                List<User> users = service.getUsers();

                if (user.Avatar == null)
                {
                    User anonymous = users.Single(x => x.Name == "Anonymous");
                    var base64 = Convert.ToBase64String(anonymous.Avatar);
                    ViewBag.ImgPath = string.Format("data:image/jpg;base64, {0}", base64);
                }
                else
                {
                    var base64 = Convert.ToBase64String(user.Avatar);
                    ViewBag.ImgPath = string.Format("data:image/jpg;base64, {0}", base64);
                }
            
            
            if(user == null)
            {
                return RedirectToAction("PageNotFound", "Error");
            }

            if(messageType != null && message != null)
            {
                TempData["message"] = message;
                TempData["status"] = messageType;
            }

            return View(user);
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult ExternalAccountPage(int id)
        {
            User u = null;
            List<User> users = null;

                users = service.getUsers();
                u = users.SingleOrDefault(x => x.Id == id);


            if(u == null)
            {
                return RedirectToAction("PageNotFound", "Error");
            }
            else if(u.Nickname == Session["Username"].ToString())
            {
                return RedirectToAction("InternalAccountPage");
            }

            if (u.Avatar == null)
            {
                User anonymous = users.Single(x => x.Name == "Anonymous");
                var base64 = Convert.ToBase64String(anonymous.Avatar);
                ViewBag.ImgPath = string.Format("data:image/jpg;base64, {0}", base64);
            }
            else
            {
                var base64 = Convert.ToBase64String(u.Avatar);
                ViewBag.ImgPath = string.Format("data:image/jpg;base64, {0}", base64);
            }

            return View(u);
        }

        [HttpPost]
        public string EditEmail(int id,string email)
        {
            bool toReturn = false;

                toReturn = service.EditUserEmail(id, email);

            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public string EditBirthDate(int id, string birthdate)
        {
            bool toReturn = false;

                toReturn = service.EditUserBirthDate(id, birthdate);


            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public string EditAvatar(int id, HttpPostedFileBase avatar)
        {
            User u = new User();
            bool success = false;
            byte[] toPost = new byte[avatar.ContentLength];

                avatar.InputStream.Read(toPost, 0, avatar.ContentLength);

                success = service.EditUserAvatar(id, toPost);


            string toReturn = null;

            if (success)
            {
                var base64 = Convert.ToBase64String(toPost);
                toReturn = string.Format("data:image/jpg;base64, {0}", base64);
            }
            

            return toReturn;
        }

    [ValidateInput(false)]
        [HttpPost]
        public ActionResult ReportBug(Bug b)
        {

            bool bugReported = false;
            bool attachmentsAdded = true;
            
            if (TempData["projId"] != null)
            { 
            b.ProjectId = Convert.ToInt32(TempData["projId"].ToString());
            b.Status = "Pending";
            b.TopicStarter = Session["Username"].ToString();

                if (ModelState.IsValid)
                {
                    string encodedDescripton = HttpUtility.HtmlEncode(b.Description);
                    string encodedSubject = HttpUtility.HtmlEncode(b.Subject);

                    b.Subject = encodedSubject;
                    b.Description = encodedDescripton;

                    Uri tcpUri = new Uri("http://localhost:1050/TestService");
                    EndpointAddress address = new EndpointAddress(tcpUri);
                    BasicHttpBinding binding = new BasicHttpBinding();
                    ChannelFactory<IService> factory = new ChannelFactory<IService>(binding, address);
                    IService service = factory.CreateChannel();

                    bugReported = service.ReportBug(b);
                }

                TempData["projId"] = b.ProjectId;
            }

            if (bugReported && attachmentsAdded)
            {
                
                return RedirectToAction("ShowProject", "Bts",
                    new { name=TempData["projName"].ToString(), messageType = "Success", message = "Bug successfully reported. Wait for further messages" });
            }
            else
            {
                TempData["status"] = "Error";
                TempData["message"] = "Something went wrong. Probably the bug has already been noticed or files are too heavy";
            }

            return View();
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult BugDescriptionPage(string projName, int id)
        {
            ViewBag.ProjectName = projName;

                Project proj = service.GetProjectsByName(projName)[0];

                Bug bug = null;

                if (proj != null)
                {
                    bug = service.GetProjectBugs(proj).Where(x => x.Id == id).ToList()[0];
                }

                if (bug != null)
                {
                    List<User> invitedDevs = null;
                    List<User> developers = service.GetDevelopersOfProject(projName, out invitedDevs);
                    List<SelectListItem> developerList = null;

                    if (developers != null)
                    {
                        developerList = new List<SelectListItem>();

                        foreach (User dev in developers)
                        {
                            SelectListItem item = new SelectListItem() { Text = dev.Nickname, Value = dev.Nickname };
                            developerList.Add(item);
                        }
                    }

                    ViewBag.Developers = developerList;

                    if (bug.DeveloperId != 0)
                    {
                        ViewBag.DeveloperInDuty = service.getUsers().Where(x => x.Id == bug.DeveloperId).ToList().SingleOrDefault();
                    }
                    else
                    {
                        ViewBag.DeveloperInDuty = null;
                    }

                    if (proj.PmId != 0)
                    {
                        ViewBag.ProjectManager = service.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();
                    }
                    else
                    {
                        ViewBag.ProjectManager = null;
                    }

                     

                    return View(bug);
                }

            return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        public string ChangeDeveloper(int bugId, string projectName, string devNickname)
        {

            bool toReturn = false;

                User u = service.getUsers().Where(x => x.Nickname == devNickname).ToList().SingleOrDefault();

                Project proj = service.GetProjectsByName(projectName).SingleOrDefault();

                Bug toChange = service.GetProjectBugs(proj).Where(x => x.Id == bugId).ToList().SingleOrDefault();

                User prevDeveloper = service.getUsers().Where(x => x.Id == toChange.DeveloperId).ToList().SingleOrDefault();
                User pm = service.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();
                User newDeveloper = service.getUsers().Where(x => x.Nickname == devNickname).Single();

                if (prevDeveloper != null)
                {
                    if (prevDeveloper.Name != devNickname)
                    {
                        string text1 = "Project manager made <a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { id = newDeveloper.Id })
                            + "\">" + devNickname + "</a> responsible for <a href=\"" + Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId
                            + "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + "</a> instead of you. Do not despair and move on!";
                        service.WriteMessage(prevDeveloper.Nickname, pm.Nickname, text1);
                        service.InformAboutNotification(prevDeveloper);

                        string text2 = "You were made responsible for <a href=\"" + 
                            Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId +
                            "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + 
                            "</a>. Have a luck!";

                        service.WriteMessage(devNickname, pm.Nickname, text2);
                        service.InformAboutNotification(newDeveloper);
                }
                else
                {

                    string text2 = "You were made responsible for <a href=\"" +
                        Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId +
                        "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName +
                        "</a>. Have a luck!";

                    service.WriteMessage(devNickname, pm.Nickname, text2);
                }      
            }


            if (u != null)
            {
                toReturn = service.SetDevIdForBug(bugId, u.Id);
                service.SetBugStatus(bugId, "Assigned");
            }

            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public ActionResult RestartBug(string projName, int id)
        {

                service.RestartBug(id);

                Project proj = service.GetProjectsByName(projName).ToList().SingleOrDefault();
                Bug bug = service.GetProjectBugs(proj).Where(x => x.Id == id).ToList().SingleOrDefault();

                User bugDeveloper = service.getUsers().Where(x => x.Id == bug.DeveloperId).ToList().SingleOrDefault();
                User pm = service.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();
                User topicStarter = service.getUsers().Where(x => x.Nickname == bug.TopicStarter).Single();

                string message = "<a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { id = topicStarter.Id })
                + "\">" + bug.TopicStarter + "</a> restarted discusion of <a href=\"" + Url.Action("BugDescriptionPage", "Bts", new { id = id, projName = projName }) + "\">" + "bug #" + id
                + "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projName }) + "\">" + projName + "</a>";

                service.WriteMessage(bugDeveloper.Nickname, bug.TopicStarter, message);
                service.InformAboutNotification(bugDeveloper);

                service.WriteMessage(pm.Nickname, bug.TopicStarter, message);
                service.InformAboutNotification(pm);

                 

                return RedirectToAction("BugDescriptionPage", "Bts", new { projName = projName, id = id });

        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult BugWorkflowPage(string projName, int id)
        {
            ViewBag.ProjectName = projName;

                Project proj = service.GetProjectsByName(projName)[0];

                Bug bug = null;

                if (proj != null)
                {
                    bug = service.GetProjectBugs(proj).Where(x => x.Id == id).ToList()[0];
                }

                if (bug != null)
                {
                    List<Message> messages = service.GetMessageLog(id);

                    int correct = -1;

                    if (messages != null)
                    {
                        messages = messages.OrderByDescending(x => x.Id).ToList();

                        foreach (Message message in messages)
                        {
                            if (message.Correct)
                            {
                                correct = message.Id;
                            }
                        }
                    }

                    if(correct != -1)
                    {
                        ViewBag.CorrectItem = correct;
                    }
                    else
                    {
                        ViewBag.CorectItem = 0;
                    }

                    ViewBag.Messages = messages;

                    return View(bug);
            }

            return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        public ActionResult BugWorkflowPage(int id, string messageToAdd, string projName, int recipientId, string Recipient)
        {
            ViewBag.ProjectName = projName;

            string[] urlSearch = messageToAdd.Split(' ');

            for(int i = 0; i < urlSearch.Count(); ++i)
            {
                if(urlSearch[i].Length > 10 && urlSearch[i].Substring(0, 7) == "http://")
                {
                    urlSearch[i] = "<a href=\"" + urlSearch[i] + "\">" + urlSearch[i] + "</a>";
                }
            }

            StringBuilder builder = new StringBuilder();
            builder.Append("<b>");

            for(int i = 0; i < urlSearch.Count(); ++i)
            {
                builder.Append(urlSearch[i]);
                builder.Append(" ");
            }

            builder.Append("</b>");

            messageToAdd = builder.ToString();

            messageToAdd.Replace(' ', '_');

            messageToAdd.Replace("\r\n", "<br/>");

                Bug bug = null;

                Message message = new Message();
                message.SenderNick = Session["Username"].ToString();
                message.UserToReply = Recipient;
                message.MessageToReplyId = recipientId;
                message.MessageText = messageToAdd;

                service.AddMessageToWorkflow(id, message);

                Project proj = service.GetProjectsByName(projName).ToList().SingleOrDefault();


                if (proj != null)
                {
                    bug = service.GetProjectBugs(proj).Where(x => x.Id == id).ToList().SingleOrDefault();
                }

                if (bug != null)
                {
                    User developer = service.getUsers().Where(x => x.Id == bug.DeveloperId).ToList().SingleOrDefault();

                    if (developer != null)
                    {
                        if (Session["Username"].ToString() == bug.TopicStarter)
                        {
                            string messageText = "<a href =\"" + Url.Action("ExternalAccountPage", "Bts", new { id = developer.Id })
                                  + "\">" + bug.TopicStarter + "</a>  added new message to discussion of <a href=\""
                                  + Url.Action("BugDescriptionPage", "Bts", new { id = bug.Id, projName = projName }) + "\"> bug # " + id + "</a>";

                            service.WriteMessage(developer.Nickname, bug.TopicStarter, messageText);
                            service.InformAboutNotification(developer);
                        }
                        else
                        {
                            string messageText = "<a href =\"" + Url.Action("ExternalAccountPage", "Bts", new { id = developer.Id })
                            + "\">" + developer.Nickname + "</a>  added new message to discussion of <a href=\""
                            + Url.Action("BugDescriptionPage", "Bts", new { id = bug.Id, projName = projName }) + "\"> bug # " + id + "</a>";

                            User topicStarter = service.getUsers().Where(x => x.Nickname == bug.TopicStarter).Single();

                            service.WriteMessage(bug.TopicStarter, developer.Nickname, messageText);
                            service.InformAboutNotification(topicStarter);
                        }
                    }

                    List<Message> messages = service.GetMessageLog(id);

                    if (messages != null)
                    {
                        messages = messages.OrderByDescending(x => x.Id).ToList();
                    }

                    ViewBag.Messages = messages;
                }

                 

                return RedirectToAction("BugDescriptionPage", "Bts", new { id = id, projName = projName });
        }

        [HttpPost]
        public string MarkRightAnswer(int selectedItemId, int bugId, int estimate, string finalComment, string projectName, string userToReply)
        {
            bool toReturn = false;

                Message aMessage = new Message();
                aMessage.MessageText = finalComment;
                aMessage.SenderNick = Session["Username"].ToString();
                aMessage.MessageToReplyId = selectedItemId;
                aMessage.UserToReply = userToReply;

                service.AddMessageToWorkflow(bugId, aMessage);

                toReturn = service.MarkRightIssueAnswer(bugId, selectedItemId, estimate);

                if(toReturn)
                {
                    Project proj = service.GetProjectsByName(projectName).ToList().SingleOrDefault();
                    Bug bug = service.GetProjectBugs(proj).Where(x => x.Id == bugId).ToList().SingleOrDefault();

                    User bugDeveloper = service.getUsers().Where(x => x.Id == bug.DeveloperId).ToList().SingleOrDefault();
                    User pm = service.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();
                    User topicStarter = service.getUsers().Where(x => x.Nickname == bug.TopicStarter).Single();

                    string message ="<a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { id = topicStarter.Id })
                    + "\">" + bug.TopicStarter + "</a> closed discusion of <a href=\"" + Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId
                    + "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + "</a>";

                    service.WriteMessage(bugDeveloper.Nickname, bug.TopicStarter, message);
                    service.InformAboutNotification(bugDeveloper);

                    service.WriteMessage(pm.Nickname, bug.TopicStarter, message);
                    service.InformAboutNotification(pm);
                }

            return JsonConvert.SerializeObject(toReturn);
        }

        [SystemAuthorize(Roles= "Admin,Tester,Developer,Project Manager")]
        public ActionResult BugSolutionPage(int id, string projName)
        {

            ViewBag.ProjectName = projName;

                Project proj = service.GetProjectsByName(projName)[0];

                Bug bug = null;

                if (proj != null)
                {
                    bug = service.GetProjectBugs(proj).Where(x => x.Id == id).ToList()[0];
                }

                if (bug != null)
                {
                    ViewBag.PmId = proj.PmId;

                    return View(bug);
                }

            return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        [ValidateInput(false)]
        public string BugSolution(int bugId, string solution, string projectName)
        {
            bool toReturn = true;

                toReturn = service.AddSolutionOfBug(bugId, solution);

                if(toReturn)
                {
                    Project proj = service.GetProjectsByName(projectName).ToList().FirstOrDefault();

                    Bug bug = service.GetProjectBugs(proj).Where(x => x.Id == bugId).ToList().FirstOrDefault();

                    User pm = service.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();

                    User topicStarter = service.getUsers().Where(x => x.Nickname == bug.TopicStarter).Single();

                    string message = "<a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { id = pm.Id })
                    + "\">" + pm.Nickname + "</a> documented solution of <a href=\"" + Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId
                    + "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + "</a>";

                    service.WriteMessage(bug.TopicStarter, pm.Nickname, message);
                    service.InformAboutNotification(topicStarter);
                }


            return JsonConvert.SerializeObject(toReturn);
        }
    }
}