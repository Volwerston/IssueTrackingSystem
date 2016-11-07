using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HiddenTryOut.Models
{
    public class HomeController : Controller
    {
        int userCount = 0;
        List<User> userList = new List<User>();
        // GET: Homw
        public ActionResult Add()
        {
            User u = new User();
            u.Id = ++userCount;

            return View(u);
        }

        public ActionResult Index()
        {
            return View(userList);
        }

        [HttpPost]

        public ActionResult Add(User u)
        {
            User usr = new User()
            {
                Id = u.Id,
            Email = u.Email,
            Username = u.Username
            };

            userList.Add(usr);
            return RedirectToAction("Index");
        }

        public ActionResult Edit(int id)
        {
            User u = userList.Single(x => x.Id == id);
            return View(u);
        }

        [HttpPost]
        public ActionResult Edit(User u)
        {
            userList.RemoveAll(x => x.Id == u.Id);
            userList.Add(u);
            return RedirectToAction("Index");
        }

    }
}
