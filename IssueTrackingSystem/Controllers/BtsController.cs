﻿using BTS.Models;
using IssueTrackingSystem.Models;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;

namespace BTS.Controllers
{
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

            if (projectFound)
            {
                ViewBag.ProjectBugs = projectBugs;

                return View(project);
            }
            else
            {
                return RedirectToAction("NotFoundErrorPage");
            }
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

        public ActionResult NotFoundErrorPage()
        {
            return View();
        }

        public ActionResult GeneralErrorPage()
        {
            return View();
        }

        // just to see that image is properly saved in db
        public ActionResult Show()
        {
            List<User> users = new List<User>();

            if (db.Open())
            {
                users = db.getUsers();
                db.Close();
            }

            return View(users);
        }
    }
}