using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SW.Frontend.Controllers
{
    public class SharedController : Controller
    {

        [ChildActionOnly]      
        public ActionResult HeaderView()
        {
            return PartialView("_HeaderView");
        }

        [ChildActionOnly]
        public ActionResult RegisterPartial()
        {
            return PartialView("_RegisterPartial");
        }
    }
}