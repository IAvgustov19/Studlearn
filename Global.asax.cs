using SW.Core.DataLayer.HellGates;
using SW.Frontend.App_Start;
using SW.Workflow.Coordinator;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Microsoft.Practices.Unity;
using log4net;
using System.IO;
using System.Web.Optimization;
using SW.Frontend.Optimization;

namespace SW.Frontend
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            // optimization
            ViewEngines.Engines.Clear();
            var razor = new RazorViewEngine();
            razor.ViewLocationCache = new TwoLevelViewCache(razor.ViewLocationCache);
            ViewEngines.Engines.Add(new RazorViewEngine());
            // initialization
            GlobalConfiguration.Configure(WebApiConfig.Register);
            AreaRegistration.RegisterAllAreas();
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            MapperManager.Configure();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            FilterConfig.RegisterWebApiFilters(GlobalConfiguration.Configuration.Filters);
            log4net.Config.XmlConfigurator.Configure(new FileInfo(Server.MapPath("~/Web.config")));
            //Scripts.DefaultTagFormat = "<script src=\"{0}\" async></script>"; // moved from Layout
        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            HttpContext context = HttpContext.Current;

            // https only in the production environment
#if !DEVELOPMENT
            if (!context.Request.IsSecureConnection && !context.Request.IsLocal)
            {
                Response.Redirect(context.Request.Url.ToString().Replace("http:", "https:"));
            }
#endif
            // http://www.validatethis.co.uk/news/fix-bad-value-x-ua-compatible-once-and-for-all/
            if (Request.UserAgent != null && Request.UserAgent.IndexOf("MSIE") > -1)
                Response.AddHeader("X-UA-Compatible", "IE=edge,chrome=1");

            #region non_www -> www with 301
            // non_www -> www with 301
            // http://stackoverflow.com/questions/3197319/asp-net-mvc-how-to-redirect-a-non-www-to-www-and-vice-versa
            // https://www.chriswiegman.com/2011/09/how-to-properly-redirect-your-domain-with-or-without-www/
            //if (!Request.Url.Host.StartsWith("www") && !Request.Url.IsLoopback)
            //{
            //    UriBuilder builder = new UriBuilder(Request.Url);
            //    builder.Host = "www." + Request.Url.Host;
            //    Response.StatusCode = 301;
            //    Response.AddHeader("Location", builder.ToString());
            //    Response.End();
            //}
            #endregion

            #region hellgate
            // hellgate
            //Guid key;
            //if (String.Equals(ConfigurationManager.AppSettings["hellgate"], "true", StringComparison.InvariantCultureIgnoreCase) 
            //    && !Request.RawUrl.Contains("api/scheduler")
            //    && !Request.RawUrl.Contains("api/internal/payments/handler")
            //    && !Request.RawUrl.Contains("unitpay-b1e8.txt")
            //    )
            //{
            //    if (context.Request.Cookies == null || context.Request.Cookies["hellgate"] == null || !Guid.TryParse(context.Request.Cookies["hellgate"].Value, out key))
            //    {
            //        Response.Redirect(ConfigurationManager.AppSettings["dashboardURl"] + "/hellgate?returnUrl=" + context.Request.Url, false);
            //        return;
            //    }
            //    var hellGateUow = UnityConfig.GetConfiguredContainer().Resolve<IHellGateUOW>();
            //    if (hellGateUow.HellGateRepository.GetByID(x => x.Id == key) == null)
            //    {
            //        Response.Redirect(ConfigurationManager.AppSettings["dashboardURl"] + "/hellgate?returnUrl=" + context.Request.Url, false);
            //        return;
            //    }
            //}
            #endregion
        }

        private static ILog _logger = LogManager.GetLogger(typeof(MvcApplication));

        void Application_Error(object sender, EventArgs e)
        {
            Exception ex = Server.GetLastError().GetBaseException();
            _logger.Error(ex);
        }

        public override string GetVaryByCustomString(HttpContext context, string custom)
        {
            if (String.Equals(custom, "mobileDiffer"))
            {
                return context.Request.Browser.IsMobileDevice ? "mobile" : "other";
            }
            return base.GetVaryByCustomString(context, custom);
        }
    }
}
