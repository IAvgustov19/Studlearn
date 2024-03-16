using SW.Workflow.Components.Emails;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Practices.Unity;
using SW.Shared.Constants.Delivery;
using SW.Frontend.Utilities.Filters;

namespace SW.Frontend.API.Internal
{
    public class SubscriptionsController : ApiUnityController
    {
        [HttpPost]
        [Route("api/subscribe")]
        [RecaptchaFilter(CapthcaRequired = true)]
        public HttpResponseMessage Subscribe([FromBody] string email, bool CaptchaValid = false)
        {
            if (!CaptchaValid)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, SW.Resources.Errors.Recpatcha);
            }

            try
            {
                var subscriber = Unity.Resolve<ISubscriber>();
                subscriber.Subscribe(email, DeliveryGroupCode.General);
                return Request.CreateResponse(HttpStatusCode.OK, "Поздравляем, ты успешно подписался на рассылку новостей");
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException || ex is ArgumentNullException || ex is ArgumentException)
                    return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
                else
                    throw;
            }
        }
    }
}
