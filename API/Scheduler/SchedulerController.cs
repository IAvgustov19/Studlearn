using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.Practices.Unity;
using SW.Workflow.Components.Emails;
using System.Threading.Tasks;
using SW.Workflow.Components.Files;
using SW.Workflow.Components.User;
using SW.Frontend.Utilities;
using SW.Workflow.Components.Cleaning;
using SW.Workflow.Components.TextSearch;
using System.Configuration;
using SW.Shared.Constants.Search;
using System.Text;

namespace SW.Frontend.API.Scheduler
{
    /*
    Outdated and moved to the separate Application -> SW.BackgroundTasks
    */
    //[SchedulerBasicAuthenticationFilter]
    //public class SchedulerController : ApiUnityController
    //{        
    //    /// <summary>
    //    /// We have emails queue in the QueuedMails. This method will try to sent all waiting emails
    //    /// </summary>
    //    /// <returns></returns>
    //    [HttpGet]
    //    [Route("api/scheduler/handle-emails-queue")]
    //    public HttpResponseMessage HandleEmails()
    //    {
    //        var emailsQueueComponent = Unity.Resolve<IEmailsQueueComponent>();

    //        return Request.CreateResponse(
    //            HttpStatusCode.OK, emailsQueueComponent.Handle()
    //        );
    //    }

    //    /// <summary>
    //    /// User upload files (image, archives, pdf) and we generate preview images (used in the disk module) by schedule
    //    /// </summary>
    //    /// <returns></returns>
    //    [HttpGet]
    //    [Route("api/scheduler/handle-file-preview")]
    //    public async Task<HttpResponseMessage> HandleFilesPreview()
    //    {
    //        var imagePreviewComponent = Unity.Resolve<IFilePreviewComponent>();

    //        return Request.CreateResponse(
    //            HttpStatusCode.OK, await imagePreviewComponent.Handle()
    //        );
    //    }

    //    /// <summary>
    //    /// Recalculate user ratings considering new comments, works likes/dislikes
    //    /// </summary>
    //    /// <returns></returns>
    //    [HttpGet]
    //    [Route("api/scheduler/user-rating-calculate")]
    //    public HttpResponseMessage HandleUserRatings()
    //    {
    //        var imagePreviewComponent = Unity.Resolve<IUserRatingComponent>();
    //        imagePreviewComponent.Calculate();
    //        return Request.CreateResponse(
    //            HttpStatusCode.OK
    //        );
    //    }

    //    [HttpGet]
    //    [Route("api/scheduler/notifications/need-verification-works")]
    //    public HttpResponseMessage HandleWorksOnModerateNotifications()
    //    {
    //        var component = Unity.Resolve<IModeratorNotificationsComponent>();
    //        return Request.CreateResponse(
    //                HttpStatusCode.OK, component.NeedsVerificationDelivery()
    //            );
    //    }

    //    [HttpGet]
    //    [Route("api/scheduler/db-cleaning")]
    //    public async Task<HttpResponseMessage> Cleaning()
    //    {
    //        IList<ICleaningComponent> components = new List<ICleaningComponent>();
    //        components.Add(Unity.Resolve<ICleaningComponent>("dbMails"));
    //        components.Add(Unity.Resolve<ICleaningComponent>("dbInvoices"));
    //        components.Add(Unity.Resolve<ICleaningComponent>("dbSlugs"));
    //        components.Add(Unity.Resolve<ICleaningComponent>("dbWorks"));

    //        Int32 success = 0, errors = 0;
    //        StringBuilder sb = new StringBuilder();
    //        sb.AppendFormat("<ul>");
    //        foreach (var component in components)
    //        {
    //            try
    //            {
    //                await component.Perofm();
    //                success++;
    //                sb.AppendFormat("<li>Task '{0}' completed with 'ok' status and responded:<p>{1}</p></li>", component.Title, component.ResultMessage);
    //            }
    //            catch (Exception ex)
    //            {
    //                _logger.Error(ex.ToString());
    //                errors++;
    //                sb.AppendFormat("<li>Task '{0}' completed with 'error' status and responded:<p>{1}</p></li>", component.Title, ex.ToString());
    //            }
    //        }
    //        sb.AppendFormat("</ul>");

    //        return Request.CreateResponse(HttpStatusCode.OK,
    //            String.Format("<p>Cleaning tasks were finished with status Ok - {0}, Error - {1}</p><p>{2}</p>", success, errors, sb.ToString())
    //            );
    //    }


    //    [HttpGet]
    //    [Route("api/scheduler/build-search-index")]
    //    public HttpResponseMessage BuildSearchIndex()
    //    {
    //        try
    //        {
    //            if (ConfigurationManager.AppSettings["LuceneEnabled"] == "true")
    //            {
    //                ISearchText components = Unity.Resolve<ISearchText>("lucene");
    //                components.BuildIndex(SearchType.NotDeleted);
    //                components.BuildIndex(SearchType.Approved);
    //                return Request.CreateResponse(HttpStatusCode.OK,
    //                    String.Format("Build Search Index successfully.")
    //                );
    //            }
    //            return Request.CreateResponse(HttpStatusCode.NoContent,
    //                    String.Format("Lucene is off.")
    //                );
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.Error(ex);
    //            return Request.CreateResponse(HttpStatusCode.BadRequest,
    //            String.Format("Build Search Index failed.")
    //            );
    //        }
    //    }
    //}
}
