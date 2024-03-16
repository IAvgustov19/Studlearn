using SW.Core.DataLayer.ExternalWriters;
using SW.Frontend.Helpers;
using SW.Frontend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace SW.Frontend.API.Public
{
    public class GeoLocationController : ApiController
    {
        private IExternalWritersUOW _externalWritersUOW;
        public GeoLocationController(IExternalWritersUOW externalWritersUOW)
        {
            _externalWritersUOW = externalWritersUOW;
        }

        [HttpGet]
        [Route("api/public/writers/locations")]
        public HttpResponseMessage GetAll()
        {
            var geos = _externalWritersUOW.ExternalWritersRepository.GetAll()
                .Where(x => x.Latitude != null && x.Longitude != null)
                .Select(x => new GeoWritersModel()
                {
                    Lat = x.Latitude.Value,
                    Long = x.Longitude.Value,
                    Title = x.Title,
                    Slug = x.Slug,
                    Rating = x.Rating
                });
            return Request.CreateResponse(HttpStatusCode.OK, geos);
        }

        [HttpGet]
        [Route("api/public/writers/locations/{id}")]
        public HttpResponseMessage GetAllExclude([FromUri] int id)
        {
            var geos = _externalWritersUOW.ExternalWritersRepository.GetAll()
                .Where(x => x.Latitude != null && x.Longitude != null)
                .Where(x => x.Id != id)
                .Select(x => new GeoWritersModel()
                {
                    Lat = x.Latitude.Value,
                    Long = x.Longitude.Value,
                    Title = x.Title,
                    Slug = x.Slug,
                    Rating = x.Rating
                });
            return Request.CreateResponse(HttpStatusCode.OK, geos);
        }
    }
}
