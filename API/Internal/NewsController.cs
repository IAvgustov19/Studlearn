using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Practices.Unity;
using SW.Frontend.Utilities;
using SW.Shared.Models.News;
using SW.Workflow.Components.Statistics;

namespace SW.Frontend.API.Internal
{
    public class NewsController : ApiUnityController
    {
        [HttpPost]
        [Route("api/internal/news/{newsId}/visit")]
        public async Task<HttpResponseMessage> NewVisit([FromUri] int newsId)
        {
            try
            {
                var visitsComponent = Unity.Resolve<IDayStatsComponent<NewsVisitDto>>();
                var visit = new NewsVisitDto
                {
                    OccuredAt = DateTime.UtcNow,
                    NewsId = newsId,
                    IP = FrontendUtilities.GetCurrentIp(Request)
                };
                if (!await visitsComponent.IsExistsAsync(visit))
                    await visitsComponent.SaveAsync(visit);
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
                throw;
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}
