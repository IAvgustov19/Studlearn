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
using SW.Core.DataLayer.ExternalWriters;
using SW.Shared.Models;
using System.IO;
using HtmlAgilityPack;
using SW.Shared.Helpers.Essential;
using SW.Shared.Helpers.Social;
using Newtonsoft.Json.Linq;
using System.IdentityModel.Services;
using System.Security.Permissions;
using SW.Frontend.Models;
using SW.Frontend.Utilities;

namespace SW.Frontend.API.Internal
{
    public class ExternalWritersController : ApiController
    {
        private IExternalWritersUOW _externalWritersUOW;
        private IAccountUOW _accountUOW;

        public ExternalWritersController(IExternalWritersUOW externalWritersUOW, IAccountUOW accountUOW)
        {
            _externalWritersUOW = externalWritersUOW;
            _accountUOW = accountUOW;
        }

        [HttpPost]
        [Route("api/internal/externalwriters/{id}/score/{score}")]
        public double PostRating([FromUri] string id, [FromUri] int score)
        {
            //запрещаем пока ставить оценки без отзыва
            Request.CreateResponse(HttpStatusCode.Unauthorized, "Post score unavailable");
            return 0;

            if (User.Identity.IsAuthenticated == false)
            {
                Request.CreateResponse(HttpStatusCode.Unauthorized, "You should be authorize to use ratings");
            }
            var userId = User.Identity.GetUserId();
            var user = _accountUOW.UsersRepository.GetAll()
                .FirstOrDefault(x => x.Id == userId);

            if (user == null)
                throw new ArgumentException("Только зарегистрированные пользователи могут оставлять отзывы");

            int idInt = 0;
            if (!int.TryParse(id, out idInt))
                throw new ArgumentException("Исполнитель не найден");

            var writer = _externalWritersUOW.ExternalWritersRepository
                .GetAll()
                .FirstOrDefault(x => x.Id == idInt);

            if (writer == null)
                throw new ArgumentException("Исполнитель не найден");

            var rating = _externalWritersUOW.ExternalWritersRatingsRepository
                    .GetAll()
                    .FirstOrDefault(x => x.UserId == userId && x.WriterId == writer.Id);
            if (rating == null)
            {
                _externalWritersUOW.ExternalWritersRatingsRepository.Insert(new ExternalWritersRating()
                {
                    UserId = userId,
                    WriterId = writer.Id,
                    Value = (byte)score,
                });
            }
            else
            {
                rating.Value = (byte)score;
            }

            writer.Rating = Math.Round((double)writer.ExternalWritersRatings.Sum(x => x.Value) / writer.ExternalWritersRatings.Count(), 2);
            _externalWritersUOW.Commit();

            return writer.Rating;
        }


        [HttpPost]
        [Route("api/internal/externalwriters/website")]
        public async System.Threading.Tasks.Task<HttpResponseMessage> GetDataByUrl([FromBody]string url)
        {
            var siteInfo = new SiteInfo();

            try
            {
                // load
                HttpClient hc = new HttpClient();
                hc.Timeout = new TimeSpan(0, 0, 3);
                string html = await hc.GetStringAsync(url);
                // parse
                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);
                // parse title
                var titleNode = doc.DocumentNode.SelectSingleNode("//head//title");
                if (titleNode != null)
                    siteInfo.Title = titleNode.InnerText.Trim().SubstringEx(32);
                // parse description
                var descriptionNode = doc.DocumentNode.SelectSingleNode("//head//meta[@name=\"description\"]");
                if (descriptionNode != null)
                {
                    var contentAttribute = descriptionNode.Attributes["content"];
                    if (contentAttribute != null)
                        siteInfo.Description = contentAttribute.Value.Trim().SubstringEx(1024);
                }
                // parse image
                var imageNode = doc.DocumentNode.SelectSingleNode("//head//meta[contains(@name, \"image\")]");
                if (imageNode != null)
                {
                    var contentAttribute = imageNode.Attributes["content"];
                    if (contentAttribute != null)
                        siteInfo.ImageUrl = contentAttribute.Value;
                }


            }
            catch (WebException)
            {
                // May be in future we will gather statistics for it
                // May be is a key word
            }

            return Request.CreateResponse(HttpStatusCode.OK, siteInfo);
        }

        [HttpPost]
        [Route("api/internal/externalwriters/vk")]
        public async System.Threading.Tasks.Task<HttpResponseMessage> GetDataByVkUrl([FromBody]string url)
        {
            var siteInfo = new SiteInfo();

            try
            {
                string vkUrl = url
                    .GetVkId()
                    .GetVkUserUrl("photo_200");
                if (!String.IsNullOrEmpty(vkUrl))
                {
                    using (HttpClient client = new HttpClient())
                    {
                        client.Timeout = new TimeSpan(0, 0, 2);
                        string jsonRaw = await client.GetStringAsync(vkUrl);
                        JToken jsonToken = JObject.Parse(jsonRaw);
                        var rToken = jsonToken.SelectToken("response");
                        if (rToken != null)
                        {
                            string fName = rToken[0]["first_name"].Value<string>();
                            string lName = rToken[0]["last_name"].Value<string>();
                            string photo = rToken[0]["photo_200"].Value<string>();
                            siteInfo.Title = lName + " " + fName;
                            siteInfo.ImageUrl = photo;
                        }
                    }
                }

            }
            catch (WebException)
            {
                // May be in future we will gather statistics for it
                // May be is a key word
            }

            return Request.CreateResponse(HttpStatusCode.OK, siteInfo);
        }

        [HttpPost]
        [Route("api/internal/externalwriters/{id}/swap-malefactor")]
        [ClaimsPrincipalPermission(SecurityAction.Demand, Resource = "Moderator", Operation = "all")]
        public HttpResponseMessage SwapMalefactor([FromUri]int id)
        {
            var writer = _externalWritersUOW.ExternalWritersRepository.GetByID(id);
            if (writer == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Исполнитель не найден");
            writer.IsMalefactor = !writer.IsMalefactor;
            _externalWritersUOW.Commit();
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpPost]
        [Route("api/internal/externalwriters/updatefield")]
        [Authorize]
        public HttpResponseMessage UpdateField([FromBody] EditWiterFieldValueModel input)
        {
            if (User.Identity.IsAuthenticated == false)
                throw new ArgumentException("You should be authorize to use comments");

            var writer = _externalWritersUOW.ExternalWritersRepository.GetByID(input.WriterId);
            if (writer == null)
                throw new ArgumentException("Исполнитель не найден");

            var userId = User.Identity.GetUserId();
            var user = _accountUOW.UsersRepository.GetAll()
                .FirstOrDefault(x => x.Id == userId);
            if (!((System.Security.Claims.ClaimsIdentity)User.Identity).HasClaim(SW.Core.DataLayer.Account.ResourcesType.Type, "Moderator") // пользователь модератор
                && !(user.EmailConfirmed && string.Equals(user.Email, writer.Email, StringComparison.InvariantCultureIgnoreCase))) // пользователь имеет тот же имейл что и исполнитель
            {
                throw new ArgumentException("Недостаточно прав");
            }

            if (string.IsNullOrEmpty(input.Value))
                throw new ArgumentException("Вы можетет только дополнить или изменить данные. Для удаления данных обратитесь в поддержку.");

            switch (input.Field.ToLower())
            {
                case "name":
                    if (input.Value.Length > 32)
                        throw new ArgumentException("Не более 32 символов");
                    writer.Title = input.Value;
                    break;
                case "description":
                    if (input.Value.Length > 1024)
                        throw new ArgumentException("Не более 1024 символов");
                    writer.Description = input.Value;
                    break;
                case "vk":
                    if (input.Value.Length > 256)
                        throw new ArgumentException("Не более 256 символов");
                    if (!new System.ComponentModel.DataAnnotations.UrlAttribute().IsValid(input.Value))
                        throw new ArgumentException("Введите валидный адрес");
                    writer.VkUrl = input.Value;
                    break;
                case "website":
                    if (input.Value.Length > 256)
                        throw new ArgumentException("Не более 256 символов");
                    if (!new System.ComponentModel.DataAnnotations.UrlAttribute().IsValid(input.Value))
                        throw new ArgumentException("Введите валидный адрес");
                    writer.Website = input.Value;
                    break;
                case "phone":
                    if (input.Value.Length > 50)
                        throw new ArgumentException("Не более 50 символов");
                    if (!new System.ComponentModel.DataAnnotations.PhoneAttribute().IsValid(input.Value))
                        throw new ArgumentException("Введите валидный номер");
                    writer.Phone = input.Value;
                    break;
                case "address":
                    if (input.Value.Length > 1024)
                        throw new ArgumentException("Не более 1024 символов");
                    writer.Address = input.Value;
                    if (!string.IsNullOrEmpty(writer.Address))
                    {
                        var point = Shared.Helpers.Google.GoogleGeo.GetCoordinates(writer.Address);
                        if (point.Latitude != 0 && point.Longitude != 0)
                        {
                            writer.Latitude = point.Latitude;
                            writer.Longitude = point.Longitude;
                        }
                    }
                    break;

                default:
                    throw new ArgumentException("Поле не найдено");
            }
            _externalWritersUOW.Commit();

            return Request.CreateResponse(HttpStatusCode.OK);
        }

        #region Review likes

        [HttpGet]
        [Route("api/internal/externalwriters/review/like/{id}/{like}")]
        public HttpResponseMessage Like([FromUri] int id, [FromUri] bool like)
        {
            var userId = User.Identity.GetUserId();
            var user = _accountUOW.UsersRepository.GetAll()
                .FirstOrDefault(x => x.Id == userId);

            var writer = _externalWritersUOW.ExternalWritersReviewsRepository
                    .GetAll()
                    .FirstOrDefault(x => x.Id == id);

            if (writer == null)
                throw new ArgumentException("Отзыв не найден");

            var newLike = new ExternalWritersReviewsRating()
            {
                UserId = userId,
                Value = like,
                ReviewId = id
            };

            if (userId == null)
            {
                IPAddress i2p = FrontendUtilities.GetCurrentIp(Request);
                if (i2p == null)
                    throw new Exception("К сожалению вы не можете отмечать отзывы.");
                var ipBytes = i2p.GetAddressBytes();
                newLike.IP = ipBytes;
                _externalWritersUOW.ExternalWritersReviewsRatingsRepository.Delete(writer.ExternalWritersReviewsRatings.Where(y => y.IP.SequenceEqual(ipBytes)));
            }
            else
            {
                newLike.UserId = userId;
                _externalWritersUOW.ExternalWritersReviewsRatingsRepository.Delete(writer.ExternalWritersReviewsRatings.Where(y => y.UserId == userId));
            }
            _externalWritersUOW.ExternalWritersReviewsRatingsRepository.Insert(newLike);
            _externalWritersUOW.Commit();
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        [Route("api/internal/externalwriters/review/{id}/delete")]
        public HttpResponseMessage DeleteLike([FromUri] int id)
        {
            var userId = User.Identity.GetUserId();
            var user = _accountUOW.UsersRepository.GetAll()
                .FirstOrDefault(x => x.Id == userId);

            var writer = _externalWritersUOW.ExternalWritersReviewsRepository
                    .GetAll()
                    .FirstOrDefault(x => x.Id == id);

            if (writer == null)
                throw new ArgumentException("Отзыв не найден");

            IEnumerable<ExternalWritersReviewsRating> existLike;

            if (userId == null)
            {
                IPAddress i2p = FrontendUtilities.GetCurrentIp(Request);
                if (i2p == null)
                    throw new Exception("К сожалению вы не можете отмечать отзывы.");
                var ipBytes = i2p.GetAddressBytes();
                existLike = writer.ExternalWritersReviewsRatings
                    .Where(y => y.IP.SequenceEqual(ipBytes));
            }
            else
            {
                existLike = writer.ExternalWritersReviewsRatings.Where(y => y.UserId == userId);
            }
            _externalWritersUOW.ExternalWritersReviewsRatingsRepository.Delete(existLike);
            _externalWritersUOW.Commit();
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        #endregion
    }
}