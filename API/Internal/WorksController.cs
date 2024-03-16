using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using SW.Core.DataLayer.Documents;
using SW.Core.DataLayer.Account;
using SW.Core.DataLayer;
using SW.Shared.Models.Documents;
using SW.Workflow.Coordinator;
using System.Text.RegularExpressions;
using SW.Workflow.Components.Statistics;
using Microsoft.Practices.Unity;
using System.Threading.Tasks;
using SW.Frontend.Utilities;

namespace SW.Frontend.API.Internal
{
    public class WorksController : ApiUnityController
    {
        private readonly IDocumentsUOW _documentsUow;
        private readonly IAccountUOW _accountUow;
        private readonly SW.Workflow.Components.Like.ILiker<Document> _liker;

        public WorksController(IDocumentsUOW documentsUow, IAccountUOW accountUow, SW.Workflow.Components.Like.ILiker<Document> liker)
        {
            _documentsUow = documentsUow;
            _accountUow = accountUow;
            _liker = liker;
        }

        [HttpGet]
        [Route("api/internal/works/{documentId}/{isVoted}/{isPositive}")]
        public HttpResponseMessage Negative([FromUri] long documentId, [FromUri] bool isVoted, [FromUri] bool isPositive)
        {
            var userId = User.Identity.GetUserId();
            var voter = _accountUow.UsersRepository.GetByID(x => x.Id == userId);
            var document = _documentsUow.DocumentsRepository.GetByID(x => x.DocumentId == documentId);

            if (document.AuthorId == userId)
                Request.CreateResponse(HttpStatusCode.BadRequest, "Недопустимое действие");

            try
            {
                _liker.Like(document, voter, isVoted, isPositive);
            }
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
            _documentsUow.Commit();

            var author = _accountUow.UsersRepository.GetByID(x => x.Id == document.AuthorId);
            if (!author.IsRatingUpdated)
            {
                author.IsRatingUpdated = true;
                _accountUow.Commit();
            }
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("api/internal/works/{documentId}/visit")]
        public async Task<HttpResponseMessage> NewVisit([FromUri] long documentId)
        {
            try
            {
                var visitsComponent = Unity.Resolve<IDayStatsComponent<DocumentVisitDto>>();
                var visit = new DocumentVisitDto
                {
                    OccuredAt = DateTime.UtcNow,
                    DocumentId = documentId,
                    IP = FrontendUtilities.GetCurrentIp(Request)
                };
                if (!await visitsComponent.IsExistsAsync(visit))
                    await visitsComponent.SaveAsync(visit);
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
                else
                    throw;
            }

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        [Route("api/internal/works/{documentId}/popularity")]
        public async Task<HttpResponseMessage> Popularity([FromUri] long documentId)
        {
            try
            {
                var beginDate = DateTime.Now.AddDays(-30).Date;

                var popularity = _documentsUow.DocumentsRepository.GetAll()
                    .FirstOrDefault(x => x.DocumentId == documentId)
                    .DocumentPopularityIndexes
                    .Where(x => x.Date >= beginDate);

                var result = Enumerable.Range(0, 30)
                    .Select(x => new object[]
                    {
                        beginDate.AddDays(x).Date,
                        popularity.FirstOrDefault(y => y.Date == beginDate.AddDays(x)) != null ? popularity.FirstOrDefault(y => y.Date == beginDate.AddDays(x)).Value : 0
                    }
                    );
                return Request.CreateResponse(HttpStatusCode.OK, result);
            }
            catch (Exception ex)
            {
                if (ex is InvalidOperationException)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
                else
                    throw;
            }
        }

    }
}