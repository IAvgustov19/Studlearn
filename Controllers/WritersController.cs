using SW.Core.DataLayer;
using SW.Core.DataLayer.NewsUnit;
using SW.Frontend.Models;
using SW.Shared.Models.ExternalWriters;
using SW.Shared.Models.News;
using SW.Workflow.Components.TextSearch;
using SW.Workflow.Coordinator;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.Unity;
using SW.Core.DataLayer.ExternalWriters;
using SW.Shared.Models.Documents;
using Microsoft.AspNet.Identity;
using SW.Core.DataLayer.Account;
using SW.Workflow.Components.Slug;
using System.Net;
using System.IO;
using Newtonsoft.Json;
using System.Threading.Tasks;
using SW.Workflow.Azure.Storage;
using SW.Frontend.Utilities;
using SW.Frontend.Utilities.Filters;
using SW.Frontend.Microdata;
using SW.Frontend.Microdata.Products;
using SW.Frontend.Microdata.Products.Models;
using Microsoft.Data.Edm;
using System.Text.RegularExpressions;

namespace SW.Frontend.Controllers
{
    //[System.IdentityModel.Services.ClaimsPrincipalPermission(System.Security.Permissions.SecurityAction.Demand, Resource = "Statistics", Operation = "all")]
    public class WritersController : UnityController
    {
        private AzureStorageProvider _azureProvider = new AzureStorageProvider(SW.Shared.Constants.Application.StorageConnectionStringName);
        private const int SearchResultsLimit = 100;
        private IExternalWritersUOW _externalWritersUOW;
        private IAccountUOW _accountUOW;


        public WritersController(IExternalWritersUOW externalWritersUOW, IAccountUOW accountUOW)
        {
            _externalWritersUOW = externalWritersUOW;
            _accountUOW = accountUOW;
        }

        [OutputCache(CacheProfile = "TopLevelTenMinutes")]
        public ActionResult Index(string q = "", int page = 1, int rows = 10)
        {
            var list = Enumerable.Empty<Shared.Models.ExternalWriters.ExternalWriterModel>();
            int total = 0;
            bool lucenEnabled = String.Equals(ConfigurationManager.AppSettings["LuceneEnabled"], "true", StringComparison.OrdinalIgnoreCase);
            //TODO: вернуть этот код
            //ViewBag.SearchEnabled = lucenEnabled;
            //if (lucenEnabled)
            //{
            //    ISearchText searchComponent = Unity.Resolve<ISearchText>("lucene");
            //    var results = searchComponent.SearchExternalWriters(q, take: SearchResultsLimit);
            //    total = results.Count();
            //    if (total == 1)
            //    {
            //        return RedirectToAction("Profile", "Writers", new { id = results.First().Slug });
            //    }
            //    if (total != 0)
            //    {
            //        list = results
            //            .Skip((page - 1) * rows)
            //            .Take(rows);
            //    }
            //}
            var viewModel = new ExternalWritersSearchModel
            {
                Writers = list,
                PagerModel = new PagerModel
                {
                    QueryString = q,
                    Rows = rows,
                    Total = total,
                    CurrentPage = page
                }

            };
            viewModel.PagerModel.LastPage = Convert.ToInt32(Math.Ceiling(viewModel.PagerModel.Total / (decimal)viewModel.PagerModel.Rows));
            viewModel.PagerModel.Count = viewModel.Writers.Count();

            viewModel.TotalWriters = _externalWritersUOW.ExternalWritersRepository.GetAll().Count();
            viewModel.RandomWriters = _externalWritersUOW.ExternalWritersRepository
                .GetAll()
                .Where(x => x.LinkToAzureImage != null && !x.IsDeleted)
                .Select(x => new ShortWriterInfo
                {
                    ImageLink = x.LinkToAzureImage,
                    Title = x.Title,
                    Tags = x.Tags.Select(y => y.Title),
                    Slug = x.Slug
                })
                //.OrderBy(x => Guid.NewGuid())
                //.Take(3 * 19 - 1)
                .ToList();
            return View(viewModel);
        }

        public ActionResult Profile(string id, int page = 1, int rows = 10)
        {
            var writer = _externalWritersUOW.ExternalWritersRepository.GetAll()
                .FirstOrDefault(x => x.Slug == id);

            if (writer == null)
                return RedirectToAction("Error", "Authors");

            var userId = User.Identity.GetUserId();
            var user = _accountUOW.UsersRepository.GetAll()
                .FirstOrDefault(x => x.Id == userId);

            var result = new WriterModel
            {
                Id = writer.Id,
                Name = writer.Title,
                Rating = writer.Rating,
                RatingCount = writer.ExternalWritersRatings.Count(),
                Image = writer.LinkToAzureImage,
                Description = writer.Description,
                Website = writer.Website,
                VkUrl = writer.VkUrl,
                Email = writer.Email,
                Phone = writer.Phone,
                Tags = writer.Tags.Select(x => x.Title),
                IsMalefactor = writer.IsMalefactor,
                Address = writer.Address,
                Latitude = writer.Latitude,
                Longitude = writer.Longitude,
                NewReview = new NewReviewModel(),
                ShowEdit = ((System.Security.Claims.ClaimsIdentity)User.Identity).HasClaim(SW.Core.DataLayer.Account.ResourcesType.Type, "Moderator") // пользователь модератор
                    || (user != null && user.EmailConfirmed && string.Equals(user.Email, writer.Email, StringComparison.InvariantCultureIgnoreCase))
            };

            ExternalWritersReview userReview;
            if (userId == null)
            {
                IPAddress ip = null;
                if (!IPAddress.TryParse(Request.UserHostAddress, out ip))
                    throw new ArgumentException("IP адрес не найден");
                var ipBytes = ip.GetAddressBytes();

                userReview = writer.ExternalWritersReviews
                    .FirstOrDefault(y => y.IP != null && y.IP.SequenceEqual(ipBytes) && (!y.ParentId.HasValue || y.ParentId == 0) && (DateTime.UtcNow - y.CreatedAt).TotalDays <= 1);
            }
            else
            {
                userReview = writer.ExternalWritersReviews.FirstOrDefault(y => y.UserId == userId && (!y.ParentId.HasValue || y.ParentId == 0));
            }

            ViewBag.WriterSlug = writer.Slug;

            result.IsUserVoted = userReview != null;
            result.UserRating = userReview != null
                ? writer.ExternalWritersRatings.FirstOrDefault(y => y.ReviewId == userReview.Id).Value
                : 0;

            ViewBag.Page = page;
            ViewBag.Rows = rows;
            IProductRichSnippet productSnippet = Unity.Resolve<IProductRichSnippet>(Microdata.Format.JSONLD.ToString());
            var productModel = result.ConvertToMicrodataProduct();
            ViewBag.MicrodataProductSnippet = productSnippet.Build(productModel);
            return View(result);
        }

        [ChildActionOnly]
        public ActionResult ReviewsList(int id, int page = 1, int rows = 10)
        {
            var writer = _externalWritersUOW.ExternalWritersRepository.GetAll()
            .FirstOrDefault(x => x.Id == id);

            if (writer == null)
                return RedirectToAction("Error", "Authors");

            var q = _externalWritersUOW.ExternalWritersReviewsRepository.GetAll()
                .Where(x => x.WriterId == id)
                .Where(x => !x.IsDeleted)
                .Where(x => !x.ParentId.HasValue || x.ParentId == 0)//только комментарии 1 уровня
                .OrderByDescending(x => x.CreatedAt);
            //  .OrderBy(x => x.ParentId).ThenBy(x => x.CreatedAt);
            // .OrderByDescending(x => x.CreatedAt);

            var reviewsSource = q
                    .Skip((page - 1) * rows)
                    .Take(rows)
                    .ToList();

            var userId = User.Identity.GetUserId();
            IPAddress i2p = null;
            IPAddress.TryParse(Request.UserHostAddress, out i2p);


            //
            var reviews = new List<CommentPreview>();
            foreach (var x in reviewsSource)
            {
                var r = prepareCommentPreviewModel(x, userId, i2p, writer, null/*parentReview*/);

                reviews.Add(r);
            }

            var result = new ExternalWritersReviewModel
            {
                Reviews = reviews,
                PagerModel = new PagerModel
                {
                    Rows = rows,
                    Total = q.Count(),
                    CurrentPage = page
                }
            };
            result.PagerModel.LastPage = Convert.ToInt32(Math.Ceiling(result.PagerModel.Total / (decimal)result.PagerModel.Rows));
            result.PagerModel.Count = result.Reviews.Count();

            result.WriterId = _externalWritersUOW.ExternalWritersRepository.GetAll()
                .First(x => x.Id == id)
                .Slug;
            return PartialView(result);
        }

        private CommentPreview prepareCommentPreviewModel(ExternalWritersReview x, string userId, IPAddress i2p, ExternalWriter writer, ExternalWritersReview initial)
        {
            var r = new CommentPreview()
            {
                CommentId = x.Id,
                Message = x.Message,
                PostedAt = x.CreatedAt,
                Author = x.UserId != null ? MapperManager.Map<AspNetUser, SW.Shared.Models.Account.UserPreview>(x.AspNetUser) : null,
                Rating = !string.IsNullOrEmpty(x.UserTitle)
                     ? (_externalWritersUOW.ExternalWritersRatingsRepository.GetAll().FirstOrDefault(y => y.ReviewId == x.Id) != null
                         ? _externalWritersUOW.ExternalWritersRatingsRepository.GetAll().FirstOrDefault(y => y.WriterId == writer.Id && y.IP == x.IP).Value
                         : 0)
                     : (x.AspNetUser.ExternalWritersRatings.FirstOrDefault(y => y.WriterId == x.WriterId) != null
                         ? x.AspNetUser.ExternalWritersRatings.FirstOrDefault(y => y.WriterId == x.WriterId).Value
                         : 0),
                //IsPositive = x.CommentsRatingHistories.FirstOrDefault(y => y.UserId == userId) != null ? x.CommentsRatingHistories.FirstOrDefault(y => y.UserId == userId).Rating : false,
                Options = new CommentOptions()
                {
                    EditEnabled = (DateTime.UtcNow - x.CreatedAt).TotalMinutes < 15,
                    AvatarEnabled = !string.IsNullOrEmpty(x.AspNetUser != null ? x.AspNetUser.AvatartUrl : null)
                },
                Likes = x.ExternalWritersReviewsRatings.Count(y => y.Value == true),
                Dislikes = x.ExternalWritersReviewsRatings.Count(y => y.Value == false),
                ParentId = x.ParentId.HasValue ? x.ParentId.Value : 0
            };

            r.AuthorTitle = string.IsNullOrEmpty(x.UserTitle) ? r.Author.UserName : x.UserTitle;
            r.ItemReviewed = writer.Title;

            ExternalWritersReviewsRating userLike = null;
            if (userId == null)
            {
                if (i2p != null)
                {
                    var ipBytes = i2p.GetAddressBytes();
                    userLike = x.ExternalWritersReviewsRatings.FirstOrDefault(y => y.IP != null && y.IP.SequenceEqual(ipBytes));
                }
            }
            else
            {
                userLike = x.ExternalWritersReviewsRatings.FirstOrDefault(y => y.UserId == userId);
            }
            r.Liked = userLike?.Value;

            //если это отзыв (а не ответ на  отзыв)
            if (x.ParentId.HasValue && x.ParentId > 0)
            {
                r.IsReply = true;
                var initialReview = initial;// reviewsSource.FirstOrDefault(y => y.Id == x.ParentId);
                if (initialReview != null && !string.IsNullOrEmpty(initialReview.UserId))
                {
                    var user = _accountUOW.UsersRepository.GetAll().FirstOrDefault(y => y.Id == initialReview.UserId);
                    r.RepliedTo = MapperManager.Map<AspNetUser, SW.Shared.Models.Account.UserPreview>(user);
                }
                else
                {
                    //hack TODO
                    r.RepliedTo = new Shared.Models.Account.UserPreview() { UserName = initialReview.UserTitle, Slug = "" };
                }
                //hack TODO
                r.RepliedTo.FirstName = initialReview.Message;
            }
            r.ChildComments = getChilds(r, userId, i2p, writer, x);

            //отмечаем цветом модератора или исполнителя
            if (x.AspNetUser != null && x.AspNetUser.AspNetUserClaims.Any(y => y.ClaimValue == "Moderator"))
            {
                r.UserType = "Модератор";
                r.UserTypeColor = "danger";
                r.UserTypeColorSource = "#f3565d";
            }
            else if (x.AspNetUser != null
              && x.AspNetUser.EmailConfirmed
              && !string.IsNullOrEmpty(writer.Email)
              && string.Equals(x.AspNetUser.Email, writer.Email))
            {
                r.UserType = "Исполнитель";
                r.UserTypeColor = "success";
                r.UserTypeColorSource = "#45b6af";
            }
            return r;
        }

        private List<CommentPreview> getChilds(CommentPreview review, string userId, IPAddress i2p, ExternalWriter writer, ExternalWritersReview reviewsSource)
        {
            var childComments = _externalWritersUOW.ExternalWritersReviewsRepository.GetAll()
              .Where(x => x.ParentId == review.CommentId)
              .OrderBy(x => x.CreatedAt)
              .ToList();

            if (childComments.Count == 0)
            {
                return new List<CommentPreview>();
            }

            var reviews = new List<CommentPreview>();
            foreach (var x in childComments)
            {
                var r = prepareCommentPreviewModel(x, userId, i2p, writer, reviewsSource);
                reviews.Add(r);
            }

            return reviews;
        }

        private WriterModel prepareWriterModelToErrorResponse(ExternalWriter writer, NewReviewModel model)
        {
            var result = new WriterModel
            {
                Id = writer.Id,
                Name = writer.Title,
                Rating = writer.Rating,
                RatingCount = writer.ExternalWritersRatings.Count(),
                Image = writer.LinkToAzureImage,
                Description = writer.Description,
                Website = writer.Website,
                VkUrl = writer.VkUrl,
                Email = writer.Email,
                Phone = writer.Phone,
                Tags = writer.Tags.Select(x => x.Title),
                Address = writer.Address,
                Latitude = writer.Latitude,
                Longitude = writer.Longitude,
                NewReview = model
            };

            ExternalWritersReview lastUserReview;
            var userId2 = User.Identity.GetUserId();
            if (userId2 == null)
            {
                IPAddress i2p = null;
                if (!IPAddress.TryParse(Request.UserHostAddress, out i2p))
                    throw new ArgumentException("IP адрес не найден");
                var ipBytes = i2p.GetAddressBytes();
                lastUserReview = writer.ExternalWritersReviews
                   .FirstOrDefault(y => y.IP != null && y.IP.SequenceEqual(ipBytes) && (!y.ParentId.HasValue || y.ParentId == 0) && (DateTime.UtcNow - y.CreatedAt).TotalDays <= 1);
            }
            else
            {
                lastUserReview = writer.ExternalWritersReviews.FirstOrDefault(y => y.UserId == userId2 && (!y.ParentId.HasValue || y.ParentId == 0));
            }
            result.IsUserVoted = lastUserReview != null;
            result.UserRating = lastUserReview != null
                ? writer.ExternalWritersRatings.FirstOrDefault(y => y.ReviewId == lastUserReview.Id).Value
                : 0;

            return result;
        }

        [RecaptchaMVCFilter]
        [HttpPost]
        public ActionResult PostReview(NewReviewModel model, bool CaptchaValid)
        {
            var writer = _externalWritersUOW.ExternalWritersRepository
                .GetAll()
                .FirstOrDefault(x => x.Id == model.Id);

            if (writer == null)
                return RedirectToAction("Error", "Authors");

            if (CaptchaValid == false)
            {
                var result = prepareWriterModelToErrorResponse(writer, model);
                ModelState.AddModelError(SW.Shared.Constants.Application.RecaptchaKey, Resources.Errors.Recpatcha);
                return View("Profile", result);
            }

            //если это не ответ на отзыв, то кол-во баллов не может быть равно 0 (если это так, то это боты обошли капчу)
            if (model.parentComment == 0 && model.score == 0)
            {
                var result = prepareWriterModelToErrorResponse(writer, model);
                ModelState.AddModelError(SW.Shared.Constants.Application.RecaptchaKey, Resources.Errors.Recpatcha);
                return View("Profile", result);
            }

            var userId = User.Identity.GetUserId();
            var user = _accountUOW.UsersRepository.GetAll().FirstOrDefault(x => x.Id == userId);

            ExternalWritersReview review = null;
            IPAddress ip = null;

            //если это отзыв (а не ответ на отзыв), то проверяем ограничения на 1 отзыв для зарегистрированного пользователя, либо 1 отзыв в час для незарегистрированного
            if (model.parentComment == 0)
            {
                if (userId == null)
                {
                    if (!IPAddress.TryParse(Request.UserHostAddress, out ip))
                        throw new ArgumentException("IP адрес не найден");

                    var today = DateTime.Now;
                    var ipBytes = ip.GetAddressBytes();
                    review = _externalWritersUOW.ExternalWritersReviewsRepository
                                    .GetAll()
                                    .Where(x => x.IP == ipBytes && x.WriterId == writer.Id)
                                    .ToList()
                                    .FirstOrDefault(x => x.CreatedAt.AddDays(1) >= today);
                }
                else
                {
                    review = _externalWritersUOW.ExternalWritersReviewsRepository
                                    .GetAll()
                                    .FirstOrDefault(x => x.UserId == userId && x.WriterId == writer.Id && (!x.ParentId.HasValue || x.ParentId == 0));
                }
            }
            //если это ответ на отзыв
            else
            {
                //если это незарегистрированный пользователь, возвращаем ошибку
                if (userId == null)
                {
                    var result = prepareWriterModelToErrorResponse(writer, model);
                    ModelState.AddModelError(SW.Shared.Constants.Application.RecaptchaKey, "Ответить на отзыв могут только зарегистрированные пользователи");
                    return View("Profile", result);
                }
            }

            //если предыдущего отзыва нет
            if (review == null)
            {
                //добавляем отзыв или ответ на отзыв
                var newReview = new ExternalWritersReview()
                {
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Message = model.Message,
                    WriterId = writer.Id,
                    IP = userId == null ? ip.GetAddressBytes() : null,
                    UserTitle = userId == null ? model.Name : null,
                    ParentId = model.parentComment
                };
                _externalWritersUOW.ExternalWritersReviewsRepository.Insert(newReview);
                _externalWritersUOW.Commit();//чтобы Id сгенерировался у нового отзыва

                //если это отзыв
                if (model.parentComment == 0)
                {
                    if (userId != null)
                    {
                        var rating = _externalWritersUOW.ExternalWritersRatingsRepository
                            .GetAll()
                            .FirstOrDefault(x => x.UserId == userId && x.WriterId == writer.Id);
                        if (rating == null)
                        {
                            _externalWritersUOW.ExternalWritersRatingsRepository.Insert(new ExternalWritersRating()
                            {
                                UserId = userId,
                                WriterId = writer.Id,
                                Value = (byte)model.score,
                                ReviewId = newReview.Id
                            });
                        }
                        else
                        {
                            rating.Value = (byte)model.score;
                        }
                    }
                    else
                    {
                        _externalWritersUOW.ExternalWritersRatingsRepository.Insert(new ExternalWritersRating()
                        {
                            WriterId = writer.Id,
                            Value = (byte)model.score,
                            IP = ip != null ? ip.GetAddressBytes() : null,
                            ReviewId = newReview.Id
                        });
                    }

                    writer.Rating = Math.Round((double)writer.ExternalWritersRatings.Sum(x => x.Value) / writer.ExternalWritersRatings.Count(), 2);

                    _externalWritersUOW.Commit();
                }
            }
            else
            {
                if (userId == null)
                {
                    var result = prepareWriterModelToErrorResponse(writer, model);
                    ModelState.AddModelError(SW.Shared.Constants.Application.RecaptchaKey, "Возможно оставлять только один отзыв в день на исполнителя");
                    return View("Profile", result);
                }
                else
                {
                    var result = prepareWriterModelToErrorResponse(writer, model);
                    ModelState.AddModelError(SW.Shared.Constants.Application.RecaptchaKey, "Возможно оставлять только один отзыв");
                    return View("Profile", result);
                }
            }
            return RedirectToAction("Profile", "Writers", new { id = writer.Slug });
        }

        public ActionResult New()
        {
            var model = new ExternalWriteMvcModel();
            return View(model);
        }

        [HttpPost]
        public async Task<ActionResult> New(ExternalWriteMvcModel writer)
        {
            if (ModelState.IsValid)
            {
                var existWriter = _externalWritersUOW.ExternalWritersRepository.GetAll()
                    .FirstOrDefault(x => (!string.IsNullOrEmpty(writer.VkUrl) && x.VkUrl == writer.VkUrl));
                if (existWriter != null)
                    throw new ArgumentException("Исполнитель с таким профилем уже существует");
                existWriter = new ExternalWriter()
                {
                    Title = writer.Title,
                    Email = writer.Email,
                    Phone = writer.Phone,
                    Website = (writer.Website ?? "").TrimEnd('/'),
                    VkUrl = (writer.VkUrl ?? "").TrimEnd('/'),
                    Description = writer.Description,
                    Slug = writer.Title.GenerateSlugEx(),
                    AddedAt = DateTime.UtcNow,
                    Address = writer.Address
                };

                if (!string.IsNullOrEmpty(writer.Address))
                {
                    var point = Shared.Helpers.Google.GoogleGeo.GetCoordinates(writer.Address);
                    if (point.Latitude != 0 && point.Longitude != 0)
                    {
                        existWriter.Latitude = point.Latitude;
                        existWriter.Longitude = point.Longitude;
                    }
                }

                byte[] data = null;
                if (writer.ImageFile != null)
                {
                    using (MemoryStream target = new MemoryStream())
                    {
                        writer.ImageFile.InputStream.CopyTo(target);
                        data = target.ToArray();
                    }
                }
                else if (!string.IsNullOrEmpty(writer.ImageUrl))
                {
                    data = await DownloadImageFromWebsiteAsync(writer.ImageUrl);
                }

                try
                {
                    existWriter.LinkToAzureImage = data == null || data.Count() == 0
                        ? default(String)
                        : (await _azureProvider.UploadPublicImage(existWriter.Slug, data, true, 200, 200, "carve")).ToString();
                }
                catch
                {
                    existWriter.LinkToAzureImage = default(string);
                }

                _externalWritersUOW.ExternalWritersRepository.Insert(existWriter);
                _externalWritersUOW.Commit();
                return RedirectToAction("Profile", new { id = existWriter.Slug });
            }
            return View(writer);
        }

        private async Task<byte[]> DownloadImageFromWebsiteAsync(string url)
        {
            try
            {
                System.Net.HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                using (WebResponse response = await request.GetResponseAsync())
                using (var result = new MemoryStream())
                {
                    Stream imageStream = response.GetResponseStream();
                    await imageStream.CopyToAsync(result);
                    return result.ToArray();
                }
            }
            catch (WebException ex)
            {
                return null;
            }
        }
    }
}