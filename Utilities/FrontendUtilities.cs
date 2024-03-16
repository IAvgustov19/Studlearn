using SW.Core.DataLayer.Account;
using SW.Frontend.App_Start;
using SW.Shared.Models.Account;
using SW.Workflow.Coordinator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.Practices.Unity;
using System.Web.Mvc;
using System.IO;
using System.Net;
using System.Net.Http;

namespace SW.Frontend.Utilities
{
    /*
     * Don't use static classes or singleton pattern because they are not unique per request
     * If you want to make changes in this class, read it before http://stackoverflow.com/questions/194999/are-static-class-instances-unique-to-a-request-or-a-server-in-asp-net
     */

    /// <summary>
    /// Global available instance, used like a helper to access user or other data
    /// </summary>
    public class FrontendUtilities
    {
        private FrontendUtilities()
        {

        }

        public static FrontendUtilities Instance
        {
            get
            {
                var items = System.Web.HttpContext.Current.Items;
                if (items != null && !items.Contains("DashboardUtilitiesInstance"))
                {
                    items["DashboardUtilitiesInstance"] = new FrontendUtilities();
                }
                return items["DashboardUtilitiesInstance"] as FrontendUtilities;
            }
        }

        private CurrentUserPreview _currentUser;

        public CurrentUserPreview CurrentUser
        {
            get
            {
                if (_currentUser == null)
                {
                    var userId = System.Threading.Thread.CurrentPrincipal.Identity.GetUserId();
                    var accountUOW = UnityConfig.GetConfiguredContainer().Resolve<IAccountUOW>();
                    var coreUser = accountUOW.UsersRepository.GetByID(userId);
                    _currentUser = MapperManager.Map<SW.Core.DataLayer.AspNetUser, CurrentUserPreview>(coreUser);
                }
                return _currentUser;
            }
        }

        public string RenderViewToString(ControllerContext context,
                                    string viewPath,
                                    object model = null,
                                    bool partial = false)
        {
            // first find the ViewEngine for this view
            ViewEngineResult viewEngineResult = null;
            if (partial)
                viewEngineResult = ViewEngines.Engines.FindPartialView(context, viewPath);
            else
                viewEngineResult = ViewEngines.Engines.FindView(context, viewPath, null);

            if (viewEngineResult == null)
                throw new FileNotFoundException("View cannot be found.");

            // get the view and attach the model to view data
            var view = viewEngineResult.View;
            context.Controller.ViewData.Model = model;

            string result = null;

            using (var sw = new StringWriter())
            {
                var ctx = new ViewContext(context, view,
                                            context.Controller.ViewData,
                                            context.Controller.TempData,
                                            sw);
                view.Render(ctx, sw);
                result = sw.ToString();
            }

            return result;
        }

        public static bool IsTopMenuItemActive(string controller, string action, string koPart = null)
        {
            string c = HttpContext.Current.Request.RequestContext.RouteData.Values["controller"].ToString();
            string a = HttpContext.Current.Request.RequestContext.RouteData.Values["action"].ToString();
            var parts = HttpContext.Current.Request.RawUrl.Split('?');
            var kp = parts.Count() > 1 ? parts[1] : String.Empty;
            return koPart == null
                ? String.Equals(c, controller, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(a, action, StringComparison.OrdinalIgnoreCase)
                : String.Equals(c, controller, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(a, action, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(kp, koPart, StringComparison.OrdinalIgnoreCase);
        }

        public static IPAddress GetCurrentIp(HttpRequestMessage request)
        {
            string ipRaw;
            if (request.Properties.ContainsKey("MS_HttpContext"))
            {
                ipRaw = ((HttpContextWrapper)request.Properties["MS_HttpContext"]).Request.UserHostAddress;
            }
            else if (request.Properties.ContainsKey(System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name))
            {
                System.ServiceModel.Channels.RemoteEndpointMessageProperty prop = (System.ServiceModel.Channels.RemoteEndpointMessageProperty)
                    request.Properties[System.ServiceModel.Channels.RemoteEndpointMessageProperty.Name];
                ipRaw = prop.Address;
            }
            else if (HttpContext.Current != null)
            {
                ipRaw = HttpContext.Current.Request.UserHostAddress;
            }
            else
            {
                ipRaw = null;
            }
            IPAddress ip;
            return IPAddress.TryParse(ipRaw, out ip) ? ip : null;
        }
    }
}