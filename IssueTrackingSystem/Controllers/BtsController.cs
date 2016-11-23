﻿using BTS.Models;
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

                            return RedirectToAction("Main");
                        }
                    }
                }

                return View();
            }
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

                    if(rememberMe)
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

        [SystemAuthorize(Roles = "Admin")]
        public ActionResult SignOut()
        {
            Session.Remove("Username");
            Session.Remove("Status");

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

                        db.Close();

                        if (addResult == "Success")
                        {
                            TempData["status"] = "Success";
                            TempData["message"] = "Account has been successfully registered";
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

        [SystemAuthorize(Roles = "Admin")]
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

        [SystemAuthorize(Roles = "Admin")]
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

                projectBugs = db.GetProjectBugs(project);
                db.Close();
            }

            if (!projectFound)
            {
                return RedirectToAction("PageNotFound", "Error");
            }

                ViewBag.ProjectBugs = projectBugs;

                return View(project);
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

        [SystemAuthorize(Roles = "Admin")]
        public ActionResult ReportBug(int projectId)
        {
            TempData["projId"] = projectId;
            Bug bug = new Bug();
            return View(bug);
        }

        [SystemAuthorize(Roles = "Admin")]
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
        public string FindUsers(int id, string[]  names, string[] statusItems)
        {
            List<User> toReturn = null;
            List<string> userStatuses = null;
            List<string> userNames = names.Distinct().ToList();
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

           // List<string> filePaths = new List<string>();
            string imgCookieString = "";

            foreach (var user in toReturn)
            {
                if (user.Avatar != null)
                {
                    var base64 = Convert.ToBase64String(user.Avatar);
                    string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                    //filePaths.Add(toAdd);
                    imgCookieString += toAdd;
                    imgCookieString += ' ';
                    user.Avatar = null;
                }
                else
                {
                    var base64 = Convert.ToBase64String(anonymous.Avatar);
                    string toAdd = string.Format("data:image/jpg;base64, {0}", base64);
                    //filePaths.Add(toAdd);
                    imgCookieString += toAdd;
                    imgCookieString += ' ';
                }
            }

            HttpCookie imgs = new HttpCookie("Images");
            imgs.Expires = DateTime.Now.AddHours(1d);
            imgs.Value = imgCookieString;
            Response.SetCookie(imgs);

            string toReturnString = JsonConvert.SerializeObject(toReturn);
            return toReturnString;
        }

        [SystemAuthorize(Roles = "Admin")]
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

        [SystemAuthorize(Roles = "Admin")]
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
            b.TopicStarter = User.Identity.Name;

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
    }
}