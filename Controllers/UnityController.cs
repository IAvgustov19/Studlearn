using Microsoft.Practices.Unity;
using SW.Frontend.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SW.Frontend.Controllers
{
    public class UnityController : Controller
    {
        public IUnityContainer Unity { get; private set; }

        public UnityController()
        {
            Unity = UnityConfig.GetConfiguredContainer();
        }
    }
}