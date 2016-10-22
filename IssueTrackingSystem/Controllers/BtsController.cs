using BTS.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace BTS.Controllers
{
    public class BtsController : Controller
    {

        private DBModel db = new DBModel();
        // GET: Bts
        public ActionResult Index()
        {
            User user = new User();
            return View(user);
        }


        public ActionResult LogIn(User u)
        {

            // change logic
            if (ModelState.IsValid)
            {
                return View();
            }

            return RedirectToAction("Index");
        }

        public ActionResult SignUp()
        {
            User user = new User();
            return View(user);
        }

        [HttpPost]
        public ActionResult SignUp(User u, HttpPostedFileBase ava)
        {

            if (ModelState.IsValid)
            {
               if(ava != null)
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
                    string password = db.getPassword(email);

                    bool letterSent = false;

                    if (password != "")
                    {
                        letterSent = db.SendPassword(email, password);
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