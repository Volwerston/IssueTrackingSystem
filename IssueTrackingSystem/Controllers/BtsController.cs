using BTS.Models;
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
            return View();
        }



        public ActionResult LogIn(string Login, string Password, bool rememberMe)
        {

            bool authenticated = false;

            if (db.Open())
            {
                if (db.isAuthenticated(Login, Password))
                {
                    FormsAuthentication.SetAuthCookie(Login, rememberMe);
                    authenticated = true;
                }

                db.Close();
            }

            if (!authenticated)
            {
                TempData["message"] = "User name and/or password are incorrect";
                return RedirectToAction("Index");
            }
            else
            {
                return RedirectToAction("Main");
            }
        }

        [Authorize]
        public ActionResult SignOut()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Index");
        }

        public ActionResult SignUp()
        {
            User user = new User();
            return View(user);
        }

        [HttpPost]
        public ActionResult SignUp(User u, HttpPostedFileBase ava, string confirmPassword)
        {
            if (ModelState.IsValid)
            {

                if (u.Password == confirmPassword)
                {
                    if (ava != null)
                    {
                        u.Avatar = new byte[ava.ContentLength];
                        ava.InputStream.Read(u.Avatar, 0, ava.ContentLength);
                    }

                    if (db.Open())
                    {

                        string addResult = db.AddAccount(u);

                        db.Close();

                        if (addResult == "Success")
                        {
                            TempData["message"] = "Account has been successfully registered";
                        }
                        else if (addResult == "Fail")
                        {
                            TempData["message"] = "Internal error. Try again. The problem may be with your birth date";
                        }
                        else
                        {
                            TempData["message"] = "User with the same nickname or e-mail has already been registered";
                        }
                    }
                }
                else
                {
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

                    if (letterSent)
                    {
                        TempData["message"] = "Please check your e-mail";
                    }
                    else
                    {
                        TempData["message"] = "Letter has not been sent. Please check the accuracy of your input";
                    }
                }
            }
            return View();
        }

        [Authorize]
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

        [Authorize]
        public ActionResult ShowProject(string name)
        {
            Project project = new Project();
            List<Bug> projectBugs = null;

            if (db.Open())
            {
                project = db.GetProjectsByName(name)[0];
                projectBugs = db.GetProjectBugs(project);
                db.Close();
            }

            ProjectBugs prBugs = new ProjectBugs()
            {
                proj = project,
                bugs = projectBugs
            };

            return View(prBugs);
        }
        
        public ActionResult ChangePassword()
        {
            if (db.Open())
            {
                if (!db.IsPasswordResetLinkValid(Request.QueryString["uid"]))
                {
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
                        TempData["message"] = "Password changed successfully";
                    }
                    else
                    {
                        TempData["message"] = "Password Reset link has expired or is invalid";
                    }

                    db.Close();
                }
            }
            else
            {
                TempData["message"] = "Passwords don't match";
            }

            return View();
        }

       [Authorize]
        public ActionResult AddBug(int projectId)
        {
            TempData["projId"] = projectId;
            Bug bug = new Bug();
            return View(bug);
        }

        [HttpPost]
        public ActionResult AddBug(Bug b)
        {

            bool bugAdded = false;
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
                        bugAdded = db.AddBug(b);

                        if (bugAdded)
                        {
                            int bugId = db.GetBugId(b.Subject);
                            attachmentsAdded = db.AddAttachments(b.Attachments.ToList(), bugId);
                        }

                        db.Close();
                    }
                }
            }

            if (bugAdded && attachmentsAdded)
            {
                TempData["message"] = "Bug successfully added. Wait for admin's confirmation";
            }
            else
            {
                TempData["message"] = "Something went wrong. Probably the bug has already been noticed or files are too heavy";
            }

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