using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.IdentityModel.Services;
using System.Web;

namespace SW.Frontend
{
    public class AuthorizationManager : ClaimsAuthorizationManager
    {
        public override bool CheckAccess(AuthorizationContext context)
        {
            var action = context.Action.First().Value;
            var claims = context.Principal.Claims;
            var resource = context.Resource.First().Value;

            var userResources = claims.Where
                (c => c.Type == SW.Core.DataLayer.Account.ResourcesType.Type);
            if (userResources != null &&
                userResources.FirstOrDefault(x => x.Value == resource) != null)
                return true;

            return false;
        }
    }
}