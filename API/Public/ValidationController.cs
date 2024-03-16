using SW.Core.DataLayer.ExternalWriters;
using SW.Frontend.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SW.Frontend.API.Public
{
    public class ValidationController : ApiController
    {
        private IExternalWritersUOW _externalWritersUOW;
        public ValidationController(IExternalWritersUOW externalWritersUOW)
        {
            _externalWritersUOW = externalWritersUOW;
        }

        [HttpPost]
        [Route("api/public/writers/validation/uniqueemail")]
        public HttpResponseMessage UniqueEmail([FromBody] String mail)
        {
            if (string.IsNullOrEmpty(mail))
            {
                return Request.CreateResponse(HttpStatusCode.OK, false);
            }

            var writer = _externalWritersUOW.ExternalWritersRepository.GetByID(x => x.Email == mail);
            return Request.CreateResponse(HttpStatusCode.OK, writer != null);
        }

        [HttpPost]
        [Route("api/public/writers/validation/uniquewebsite")]
        public HttpResponseMessage UniqueWebsite([FromBody] String website)
        {
            if (string.IsNullOrEmpty(website))
            {
                return Request.CreateResponse(HttpStatusCode.OK, false);
            }

            try
            {
                var uri = new Uri(website);
                var writer = _externalWritersUOW.ExternalWritersRepository.GetByID(x => x.Website.ToLower().Contains(uri.Host.ToLower()));
                return Request.CreateResponse(HttpStatusCode.OK, writer != null);
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.OK, false);
            }
        }

        [HttpPost]
        [Route("api/public/writers/validation/uniquevk")]
        public HttpResponseMessage UniqueVK([FromBody] String VkUrl)
        {
            //var existWriter = new Core.DataLayer.ExternalWriter();
            //if (!string.IsNullOrEmpty(VkUrl))
            //{
            //    string Id = null;
            //    var uri = new Uri(VkUrl);
            //    if (uri.Segments[1] != null)
            //    {
            //        Id = (uri.Segments)[1];
            //        try
            //        {
            //            VkApiHelper.GetVKUser(Id, existWriter);
            //        }
            //        catch
            //        {
            //            try
            //            {
            //                VkApiHelper.GetVKGroup(Id, existWriter);
            //            }
            //            catch { }
            //        }
            //    }
            //}

            //if (existWriter.VkId != null)
            //{
            //    var existWriterByVkId = _externalWritersUOW.ExternalWritersRepository.GetAll()
            //        .FirstOrDefault(x => x.VkId == existWriter.VkId);
            //    return Request.CreateResponse(HttpStatusCode.OK, existWriterByVkId != null);
            //}
            if (string.IsNullOrEmpty(VkUrl))
            {
                return Request.CreateResponse(HttpStatusCode.OK, false);
            }

            try
            {
                VkUrl = VkUrl.TrimEnd('/');
                var writer = _externalWritersUOW.ExternalWritersRepository.GetByID(x => x.VkUrl == VkUrl);
                return Request.CreateResponse(HttpStatusCode.OK, writer != null);
            }
            catch
            {
                return Request.CreateResponse(HttpStatusCode.OK, false);
            }
        }
    }
}
