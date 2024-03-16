using SW.Core.DataLayer.Account;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SW.Frontend.API.Internal
{
    public class AccountController : ApiController
    {
        private readonly IAccountUOW _accountUow;

        public AccountController(IAccountUOW accountUow)
        {
            _accountUow = accountUow;
        }

        [HttpGet]
        [Route("api/internal/account/me")]
        public HttpResponseMessage GetCurrentAccountInfo()
        {
            var response = new SW.Shared.Models.Account.AccountInfo();
            response.UserName = User?.Identity?.Name;
            response.IsAuthenticated = User?.Identity?.IsAuthenticated ?? false;
            return Request.CreateResponse(HttpStatusCode.OK, response);
        }
    }
}