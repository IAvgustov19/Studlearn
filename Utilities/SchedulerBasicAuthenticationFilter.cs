using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Utilities
{
    public class SchedulerBasicAuthenticationFilter : BasicAuthenticationFilter
    {

        public SchedulerBasicAuthenticationFilter()
        { }

        public SchedulerBasicAuthenticationFilter(bool active)
            : base(active)
        { }

        protected override bool OnAuthorizeUser(string username, string password, System.Web.Http.Controllers.HttpActionContext actionContext)
        {
            return String.Equals(username, "scheduler", StringComparison.Ordinal) && String.Equals(password, "8crP9jQrSxTMX3jVWg4C", StringComparison.Ordinal);
        }
    }
}