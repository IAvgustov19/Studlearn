using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Practices.Unity;
using SW.Frontend.App_Start;
using log4net;

namespace SW.Frontend.API
{
    public abstract class ApiUnityController : ApiController
    {
        protected static ILog _logger = LogManager.GetLogger(typeof(ApiUnityController));

        public IUnityContainer Unity { get; private set; }

        public ApiUnityController()
        {
            Unity = UnityConfig.GetConfiguredContainer(); 
        }
    }
}
