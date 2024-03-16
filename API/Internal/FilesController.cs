using SW.Core.DataLayer.Account;
using SW.Core.DataLayer.Documents;
using SW.Workflow.Components.Files;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNet.Identity;
using System.IO;
using log4net;
using Microsoft.Practices.Unity;
using SW.Workflow.Components.Statistics;
using SW.Shared.Models.Documents;
using SW.Frontend.Utilities.Filters;

namespace SW.Frontend.API.Internal
{
    public class FilesController : ApiUnityController
    {
        private readonly IDocumentsUOW _documentsUow;
        private readonly IAccountUOW _accountUow;
        private static ILog _logger = LogManager.GetLogger(typeof(FilesController));

        public FilesController(IDocumentsUOW documentsUow, IAccountUOW accountUow)
        {
            _documentsUow = documentsUow;
            _accountUow = accountUow;
        }

        [HttpGet]
        [Route("api/file/{fileId}/{documentId}")]
       // [RecaptchaFilter(CapthcaRequired = false)]
        public async Task<HttpResponseMessage> GetFile(Int32 fileId, Int32 documentId/*, bool CaptchaValid = false*/, Guid? token = null)
        {
            FileProxy wrapper = new FileProxy(_documentsUow, _accountUow, SW.Shared.Constants.Application.StorageConnectionStringName);
            var file = await wrapper.GetFile(User.Identity.GetUserId(), fileId, documentId, token);
            if (file == null)
            {
                _logger.Error("Файл недоступен или удален");
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Файл недоступен или удален");
            }
            if (file.Data == null)
            {
                _logger.Error("К сожалению, мы не нашли содержимое \"" + file.Name + "\" файла в нашем хранилище. Пожалуйста, свяжитесь с нами (sales@studlearn.com) и укажите номер заказа и название файла. Мы решим эту проблему в ближайшее время или вернем вам деньги." + "\r\n Token: " + token + ", fileId: " + fileId + ", documentId" + documentId);
                return Request.CreateResponse(HttpStatusCode.BadRequest, "К сожалению, мы не нашли содержимое \"" + file.Name + "\" файла в нашем хранилище. Пожалуйста, свяжитесь с нами (sales@studlearn.com) и укажите номер заказа и название файла. Мы решим эту проблему в ближайшее время или вернем вам деньги.");
            }

            ////если капча невалидная и пытаются получится доступ к файлу без токена, то запрещаем такой доступ
            //if (!CaptchaValid && (token == null || token == Guid.Empty))
            //{
            //    _logger.Error("Невалидная капча");
            //    return Request.CreateResponse(HttpStatusCode.BadRequest, SW.Resources.Errors.Recpatcha);
            //}

            var response = new HttpResponseMessage();
            Stream fileStream = new MemoryStream(file.Data);
            response.Content = new StreamContent(fileStream);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment");
            response.Content.Headers.ContentDisposition.FileName = file.Name;
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(file.Type);
            response.Content.Headers.ContentLength = file.Data.Length;
            // update download statistics
            try
            {
                var downloadsComponent = Unity.Resolve<IDayStatsComponent<DocumentDownloadDto>>();
                var downloadDto = new DocumentDownloadDto
                {
                    OccuredAt = DateTime.UtcNow,
                    DocumentId = documentId,
                    IP = Utilities.FrontendUtilities.GetCurrentIp(Request)
                };
                if (!await downloadsComponent.IsExistsAsync(downloadDto))
                    await downloadsComponent.SaveAsync(downloadDto);
            }
            catch (Exception ex)
            {
                _logger.Error("UPDATE WORK DOWNLOADS INFO FAILED", ex);
            }
            return response;
        }
    }
}
