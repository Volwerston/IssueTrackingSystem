using BTS.Models;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using IssueTrackingSystem.Common;
using System.Web.Security;
using System.IO;

namespace BTS.Controllers
{
    [LogError]
    public class BtsController : Controller
    {

        private DBModel db = new DBModel();
        // GET: Bts

        public ActionResult Index()
        {
            if (Session["Username"] != null && Session["Status"] != null)
            {
                return RedirectToAction("Main");
            }
            else
            {
                if (Request.Cookies["username"] != null && Request.Cookies["password"] != null)
                {
                    if (db.Open())
                    {
                        User u = db.getAuthenticatedUser(Request.Cookies["username"].Value, Request.Cookies["password"].Value);

                        db.Close();

                        if (u.Id != -1)
                        {
                            Session["Username"] = Request.Cookies["username"].Value;
                            Session["Status"] = u.Status;
                            Session["Confirmed"] = u.Confirmed.ToString();
                            Session["Id"] = u.Id;

                            return RedirectToAction("Main");
                        }
                    }
                }

                return View();
            }
        }

        [HttpPost]
        public ActionResult ConfirmUser(int userId)
        {
            bool toReturn = false;

            if(db.Open())
            {
                toReturn = db.ConfirmUser(userId);
                db.Close();
            }

            return RedirectToAction("ExternalAccountPage", "Bts", new { id = userId });
        }

        public ActionResult LogIn(string Login, string Password, bool rememberMe)
        {

            bool authenticated = false;

            if (db.Open())
            {
                User u = db.getAuthenticatedUser(Login, Password);
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

                db.Close();
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

                    if (db.Open())
                    {

                        string addResult = db.AddAccount(u);

                        if (addResult == "Success")
                        {
                            TempData["status"] = "Success";
                            TempData["message"] = "Account has been successfully registered";

                            int insertedId = db.getUsers().Where(x => x.Nickname == u.Nickname).ToList().First().Id;

                            string text = "Please confirm the personality of new user <a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { @id = insertedId }) + "\">" + u.Nickname + "</a>";

                            List<User> admins = db.getUsers().Where(x => x.Status == "Admin").ToList();

                            foreach (var admin in admins)
                            {
                                db.WriteMessage(admin.Nickname, u.Nickname, text);
                                db.InformAboutNotification(admin);
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

                        db.Close();
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
                if (db.Open())
                {
                    bool letterSent = false;

                    if (email != "")
                    {
                        letterSent = db.isEmailSent(email);
                    }

                    db.Close();

                    TempData["status"] = "Success";
                    TempData["message"] = "Please check your e-mail";

                }
            }
            return View();
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult Main()
        {
            List<Category> categories = new List<Category>();

            if (db.Open())
            {
                categories = db.getCategories();
                db.Close();
            }

            return View(categories);
        }

        [HttpPost]
        public string GetProjectsByName(string[] prName)
        {
            List<Project> toReturn = null;

            if (db.Open())
            {
                toReturn = db.GetProjectsByName(prName[0]);
                db.Close();
            }

            string returnString = JsonConvert.SerializeObject(toReturn);
            return returnString;
        }

        [HttpPost]
        public string GetProjectsByCategories(int[] categories)
        {
            List<Project> toReturn = null;

            if (db.Open())
            {
                toReturn = db.GetProjectsByCategories(categories);
                db.Close();
            }

            string returnString = JsonConvert.SerializeObject(toReturn);
            return returnString;
        }

        [SystemAuthorize(Roles="Project Manager,Admin")]
        public ActionResult AddProject()
        {
            Project proj = new Project();
            return View(proj);
        }

        [HttpPost]
        public ActionResult AddProject(Project proj, HttpPostedFileBase aLogo)
        {
            if(ModelState.IsValid)
            {
                proj.PmId = int.Parse(Session["Id"].ToString());

                byte[] img = new byte[aLogo.ContentLength];
                aLogo.InputStream.Read(img, 0, aLogo.ContentLength);

                proj.Logo = img;

                if(db.Open())
                {
                    bool success = db.AddProject(proj);

                    db.Close();
                }

                return RedirectToAction("InternalAccountPage", "Bts");
            }

            return View();
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult ShowProject(string name)
        {
            bool projectFound = false;

            Project project = new Project();

            List<Bug> projectBugs = null;

            if (db.Open())
            {
                List<Project> chosenProjects = db.GetProjectsByName(name);

                if (chosenProjects.Count > 0)
                {
                    projectFound = true;
                    project = chosenProjects[0];
                }

                if (project != null)
                {
                    List<User> invitedDevs = null;
                    List<User> developers = db.GetDevelopersOfProject(name, out invitedDevs);
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

                projectBugs = db.GetProjectBugs(project);
                db.Close();
            }

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

            return View(project);
        }

        public ActionResult JoinProject(int id, string projectName)
        {
            if(db.Open())
            {
                db.ApproveDeveloperForProject(projectName, id);
            }

            return RedirectToAction("ShowProject", "Bts", new { name = projectName });
        }

        public ActionResult ChangePassword()
        {
            if (db.Open())
            {
                if (!db.IsPasswordResetLinkValid(Request.QueryString["uid"]))
                {
                    TempData["status"] = "Error";
                    TempData["message"] = "Password Reset link has expired or is invalid";
                }

                db.Close();
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

            if (db.Open())
            {
                anonymous = db.getUsers().Where(x => x.Nickname == "Anonymous").ToList()[0];

                List<User> invitedDevs = null;
                List<User> currDevelopers = db.GetDevelopersOfProject(projectName, out invitedDevs);

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

                    buf = db.searchForUsers(id, userNames, new List<string> { "Developer" });

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

            if(db.Open())
            {
                toReturn = db.GetNotificationsOfUser(receiver);

                db.Close();
            }


            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public string InviteDevelopers(int devId, string projectName)
        {
            bool toReturn = false;

            if (db.Open())
            {
                toReturn = db.InviteDeveloperToProject(projectName, devId);

                if(toReturn)
                {
                    Project proj = db.GetProjectsByName(projectName).ToList().SingleOrDefault();

                    User dev = db.getUsers().Where(x => x.Id == devId).ToList().SingleOrDefault();

                    User admin = db.getUsers().Where(x => x.Status == "Admin").ToList()[0];

                    string text = "You were invited to the <a href=\"" + Url.Action("ShowProject", "Bts", new { name = proj.Name }) + "\">" + projectName + "</a> project. Please go to the project page to accept invitation";

                    db.WriteMessage(dev.Nickname, admin.Nickname, text);
                    db.InformAboutNotification(dev);
                }

                db.Close();
            }
   
            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public ActionResult ChangePassword(string password, string confirmPassword)
        {
            if (password == confirmPassword)
            {

                if (db.Open())
                {
                    if (db.ChangeUserPassword(Request.QueryString["uid"], password))
                    {
                        TempData["status"] = "Success";
                        TempData["message"] = "Password changed successfully";
                    }
                    else
                    {
                        TempData["status"] = "Error";
                        TempData["message"] = "Password Reset link has expired or is invalid";
                    }

                    db.deleteExpiredRecords();

                    db.Close();
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
        public ActionResult ReportBug(int projectId)
        {
            TempData["projId"] = projectId;
            Bug bug = new Bug();
            return View(bug);
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult FindUsers()
        {
            if (db.Open())
            {
            List<string> statuses = db.getUsers().Select(x => x.Status).ToList().Distinct().ToList();

            ViewBag.StatusList = statuses;

            db.Close();

            return View();
            }
            else return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        public string RemoveDevelopersFromProject(string projName, int[] toErase)
        {
            bool toReturn = false;

            if (toErase != null)
            {
                if (db.Open())
                {
                    toReturn = db.RemoveDevsFromProject(projName, toErase.ToList());
                    db.Close();
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

            if(db.Open())
            {
                toReturn = db.searchForUsers(id, userNames, userStatuses);
                anonymous = db.getUsers().Where(x => x.Nickname == "Anonymous").ToList()[0];
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

            if(db.Open())
            {
                toReturn = db.RemoveNotification(id);

                db.Close();
            }

            return JsonConvert.SerializeObject(toReturn);
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult InternalAccountPage()
        {
            User user = new User();

            if (db.Open())
            {
                user = db.getAuthenticatedUser(Session["Username"].ToString());
                List<User> users = db.getUsers();

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
            }
            
            if(user == null)
            {
                return RedirectToAction("PageNotFound", "Error");
            }

            return View(user);
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult ExternalAccountPage(int id)
        {
            User u = null;
            List<User> users = null;

            if (db.Open())
            {
                users = db.getUsers();
                u = users.SingleOrDefault(x => x.Id == id);

                db.Close();
            }

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

            if(db.Open())
            {
                toReturn = db.EditUserEmail(id, email);

                db.Close();
            }

            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public string EditBirthDate(int id, string birthdate)
        {
            bool toReturn = false;

            if (db.Open())
            {
                toReturn = db.EditUserBirthDate(id, birthdate);

                db.Close();
            }

            return JsonConvert.SerializeObject(toReturn);
        }

        [HttpPost]
        public string EditAvatar(int id, HttpPostedFileBase avatar)
        {
            User u = new User();
            bool success = false;
            byte[] toPost = new byte[avatar.ContentLength];

            if (db.Open())
            {
                avatar.InputStream.Read(toPost, 0, avatar.ContentLength);

                success = db.EditUserAvatar(id, toPost);

                db.Close();
            }

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
            bool attachmentsAdded = false;
            
            if (TempData["projId"] != null)
            { 
            b.ProjectId = Convert.ToInt32(TempData["projId"].ToString());
            b.Status = "Unconfirmed";
            b.TopicStarter = Session["Username"].ToString();

                if (ModelState.IsValid)
                {
                    string encodedDescripton = HttpUtility.HtmlEncode(b.Description);
                    string encodedSubject = HttpUtility.HtmlEncode(b.Subject);

                    b.Subject = encodedSubject;
                    b.Description = encodedDescripton;

                    if (db.Open())
                    {
                        bugReported = db.ReportBug(b);

                        if (bugReported)
                        {
                            int bugId = db.GetBugId(b.Subject);
                            attachmentsAdded = db.AddAttachments(b.Attachments.ToList(), bugId);
                        }

                        db.Close();
                    }
                }

                TempData["projId"] = b.ProjectId;
            }

            if (bugReported && attachmentsAdded)
            {
                TempData["status"] = "Success";
                TempData["message"] = "Bug successfully added. Wait for admin's confirmation";
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

            if (db.Open())
            {
                Project proj = db.GetProjectsByName(projName)[0];

                Bug bug = null;

                if (proj != null)
                {
                    bug = db.GetProjectBugs(proj).Where(x => x.Id == id).ToList()[0];
                }

                if (bug != null)
                {
                    List<User> invitedDevs = null;
                    List<User> developers = db.GetDevelopersOfProject(projName, out invitedDevs);
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
                        ViewBag.DeveloperInDuty = db.getUsers().Where(x => x.Id == bug.DeveloperId).ToList().SingleOrDefault();
                    }
                    else
                    {
                        ViewBag.DeveloperInDuty = null;
                    }

                    if (bug.PmId != 0)
                    {
                        ViewBag.ProjectManager = db.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();
                    }
                    else
                    {
                        ViewBag.ProjectManager = null;
                    }

                    db.Close();

                    return View(bug);
                }

            }

            return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        public string ChangeDeveloper(int bugId, string projectName, string devNickname)
        {

            bool toReturn = false;

            if(db.Open())
            {
                User u = db.getUsers().Where(x => x.Nickname == devNickname).ToList().SingleOrDefault();

                Project proj = db.GetProjectsByName(projectName).SingleOrDefault();

                Bug toChange = db.GetProjectBugs(proj).Where(x => x.Id == bugId).ToList().SingleOrDefault();

                User prevDeveloper = db.getUsers().Where(x => x.Id == toChange.DeveloperId).ToList().SingleOrDefault();
                User pm = db.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();
                User newDeveloper = db.getUsers().Where(x => x.Nickname == devNickname).Single();

                if (prevDeveloper != null)
                {
                    if (prevDeveloper.Name != devNickname)
                    {
                        string text1 = "Project manager made <a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { id = newDeveloper.Id })
                            + "\">" + devNickname + "</a> responsible for <a href=\"" + Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId
                            + "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + "</a> instead of you. Do not despair and move on!";
                        db.WriteMessage(prevDeveloper.Nickname, pm.Nickname, text1);
                        db.InformAboutNotification(prevDeveloper);

                        string text2 = "You were made responsible for <a href=\"" + 
                            Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId +
                            "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + 
                            "</a>. Have a luck!";

                        db.WriteMessage(devNickname, pm.Nickname, text2);
                        db.InformAboutNotification(newDeveloper);
                    }
                }
                else
                {

                    string text2 = "You were made responsible for <a href=\"" +
                        Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId +
                        "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName +
                        "</a>. Have a luck!";

                    db.WriteMessage(devNickname, pm.Nickname, text2);
                }

                if (u != null)
                {
                    toReturn = db.SetDevIdForBug(bugId, u.Id);
                }

                db.Close();
            }

            return JsonConvert.SerializeObject(toReturn);
        }

        [SystemAuthorize(Roles = "Admin,Tester,Developer,Project Manager")]
        public ActionResult BugWorkflowPage(string projName, int id)
        {
            ViewBag.ProjectName = projName;

            if (db.Open())
            {
                Project proj = db.GetProjectsByName(projName)[0];

                Bug bug = null;

                if (proj != null)
                {
                    bug = db.GetProjectBugs(proj).Where(x => x.Id == id).ToList()[0];
                }

                if (bug != null)
                {
                    List<Message> messages = db.GetMessageLog(id);

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
            }

            return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        public ActionResult BugWorkflowPage(int id, string messageToAdd, string projName, int recipientId, string Recipient)
        {
            ViewBag.ProjectName = projName;

            if (db.Open())
            {
                Bug bug = null;

                Message message = new Message();
                message.SenderNick = Session["Username"].ToString();
                message.UserToReply = Recipient;
                message.MessageToReplyId = recipientId;
                message.MessageText = messageToAdd;

                if (id == 0)
                {
                    message.MessageToReplyId = null;
                }

                if (Recipient == "")
                {
                    message.UserToReply = null;
                }

                db.AddMessageToWorkflow(id, message);

                Project proj = db.GetProjectsByName(projName).ToList().SingleOrDefault();


                if (proj != null)
                {
                    bug = db.GetProjectBugs(proj).Where(x => x.Id == id).ToList().SingleOrDefault();
                }

                if (bug != null)
                {
                    User developer = db.getUsers().Where(x => x.Id == bug.DeveloperId).ToList().SingleOrDefault();

                    if (developer != null)
                    {
                        if (Session["Username"].ToString() == bug.TopicStarter)
                        {
                            string messageText = "<a href =\"" + Url.Action("ExternalAccountPage", "Bts", new { id = developer.Id })
                                  + "\">" + bug.TopicStarter + "</a>  added new message to discussion of <a href=\""
                                  + Url.Action("BugDescriptionPage", "Bts", new { id = bug.Id, projName = projName }) + "\"> bug # " + id + "</a>";

                            db.WriteMessage(developer.Nickname, bug.TopicStarter, messageText);
                            db.InformAboutNotification(developer);
                        }
                        else
                        {
                            string messageText = "<a href =\"" + Url.Action("ExternalAccountPage", "Bts", new { id = developer.Id })
                            + "\">" + developer.Nickname + "</a>  added new message to discussion of <a href=\""
                            + Url.Action("BugDescriptionPage", "Bts", new { id = bug.Id, projName = projName }) + "\"> bug # " + id + "</a>";

                            User topicStarter = db.getUsers().Where(x => x.Nickname == bug.TopicStarter).Single();

                            db.WriteMessage(bug.TopicStarter, developer.Nickname, messageText);
                            db.InformAboutNotification(topicStarter);
                        }
                    }

                    List<Message> messages = db.GetMessageLog(id);

                    if (messages != null)
                    {
                        messages = messages.OrderByDescending(x => x.Id).ToList();
                    }

                    ViewBag.Messages = messages;
                }

                db.Close();

                return RedirectToAction("BugDescriptionPage", "Bts", new { id = id, projName = projName });
            }


            return RedirectToAction("PageNotFound", "Bts");
        }

        [HttpPost]
        public string MarkRightAnswer(int selectedItemId, int bugId, int estimate, string finalComment, string projectName)
        {
            bool toReturn = false;

            if(db.Open())
            {
                Message aMessage = new Message();
                aMessage.MessageText = finalComment;
                aMessage.SenderNick = Session["Username"].ToString();

                db.AddMessageToWorkflow(bugId, aMessage);

                toReturn = db.MarkRightIssueAnswer(bugId, selectedItemId, estimate);

                if(toReturn)
                {
                    Project proj = db.GetProjectsByName(projectName).ToList().SingleOrDefault();
                    Bug bug = db.GetProjectBugs(proj).Where(x => x.Id == bugId).ToList().SingleOrDefault();

                    User bugDeveloper = db.getUsers().Where(x => x.Id == bug.DeveloperId).ToList().SingleOrDefault();
                    User pm = db.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();
                    User topicStarter = db.getUsers().Where(x => x.Nickname == bug.TopicStarter).Single();

                    string message ="<a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { id = topicStarter.Id })
                    + "\">" + bug.TopicStarter + "</a> closed discusion of <a href=\"" + Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId
                    + "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + "</a>";

                    db.WriteMessage(bugDeveloper.Nickname, bug.TopicStarter, message);
                    db.InformAboutNotification(bugDeveloper);

                    db.WriteMessage(pm.Nickname, bug.TopicStarter, message);
                    db.InformAboutNotification(pm);
                }

                db.Close();
            }

            return JsonConvert.SerializeObject(toReturn);
        }

        [SystemAuthorize(Roles= "Admin,Tester,Developer,Project Manager")]
        public ActionResult BugSolutionPage(int id, string projName)
        {

            ViewBag.ProjectName = projName;

            if (db.Open())
            {
                Project proj = db.GetProjectsByName(projName)[0];

                Bug bug = null;

                if (proj != null)
                {
                    bug = db.GetProjectBugs(proj).Where(x => x.Id == id).ToList()[0];
                }

                if (bug != null)
                {
                    return View(bug);
                }
            }

            return RedirectToAction("PageNotFound", "Error");
        }

        [HttpPost]
        [ValidateInput(false)]
        public string BugSolution(int bugId, string solution, string projectName)
        {
            bool toReturn = true;

            if(db.Open())
            {
                toReturn = db.AddSolutionOfBug(bugId, solution);

                if(toReturn)
                {
                    Project proj = db.GetProjectsByName(projectName).ToList().FirstOrDefault();

                    Bug bug = db.GetProjectBugs(proj).Where(x => x.Id == bugId).ToList().FirstOrDefault();

                    User pm = db.getUsers().Where(x => x.Id == proj.PmId).ToList().SingleOrDefault();

                    User topicStarter = db.getUsers().Where(x => x.Nickname == bug.TopicStarter).Single();

                    string message = "<a href=\"" + Url.Action("ExternalAccountPage", "Bts", new { id = pm.Id })
                    + "\">" + pm.Nickname + "</a> documented solution of <a href=\"" + Url.Action("BugDescriptionPage", "Bts", new { id = bugId, projName = projectName }) + "\">" + "bug #" + bugId
                    + "</a> in project <a href=\"" + Url.Action("ShowProject", "Bts", new { name = projectName }) + "\">" + projectName + "</a>";

                    db.WriteMessage(bug.TopicStarter, pm.Nickname, message);
                    db.InformAboutNotification(topicStarter);
                }

                db.Close();
            }
            return JsonConvert.SerializeObject(toReturn);
        }
    }
}