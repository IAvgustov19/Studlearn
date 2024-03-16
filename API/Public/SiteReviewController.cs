using Microsoft.Practices.Unity;
using SW.Frontend.Controllers;
using SW.Frontend.Models;
using SW.Workflow.Components.Emails;
using System;
using System.Net;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;

namespace SW.Frontend.API.Public
{
    public class SiteReviewController : ApiUnityController
    {
        [AllowAnonymous]
        [HttpPost]
        [Route("api/sitereview/postreview")]
        public HttpResponseMessage PostReview(SiteReviewModel review)
        {
            var emailsComponent = Unity.Resolve<IEmailsQueueComponent>();
            var context = new System.Web.HttpContextWrapper(System.Web.HttpContext.Current);
            var routeData = new System.Web.Routing.RouteData();
            routeData.Values.Add("controller", "EmailsController");
            var controllerContext = new System.Web.Mvc.ControllerContext(context, routeData, new EmailsController());
            var purchaseEmailBody = SW.Frontend.Utilities.FrontendUtilities.Instance.RenderViewToString(
                            controllerContext,
                            "~/Views/Emails/NewSiteReviewEmailView.cshtml",
                            model: review, partial: false
                            );
            var purchaseEmailTitle = SW.Resources.Emails.NewSiteReviewTitle;
            var targetEmail = WebConfigurationManager.AppSettings["SiteReviewTargetEmail"];
            if (string.IsNullOrEmpty(targetEmail))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, $"Сервис отзывов временно недоступен. Пожалуйста, обратитесь к Администратору.");
            }

            emailsComponent.Push(targetEmail, purchaseEmailTitle, purchaseEmailBody);

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}