using SW.Core.DataLayer.Documents;
using SW.Workflow.Coordinator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DB = SW.Core.DataLayer;
using DTO = SW.Shared.Models;
using Microsoft.AspNet.Identity;
using System.Text.RegularExpressions;
using SW.Core.DataLayer.Account;
using SW.Core.DataLayer;
using SW.Workflow.Components.Files;
using System.Threading.Tasks;
using SW.Shared.Models.Documents;
using SW.Workflow.Components.Emails;
using Microsoft.Practices.Unity;
using SW.Workflow.Components;
using SW.Workflow.Components.Storage;
using System.Text;
using SW.Frontend.Utilities;
using Newtonsoft.Json;
using SW.Shared.Helpers.HTML;
using System.Configuration;
using SW.Workflow.Components.UnitPay;
using SW.Shared.Constants.Payment;
using System.Net;
using System.Net.Http;
using SW.Shared.Constants;
using log4net;
using SW.Workflow.Components.TextSearch;
using SW.Shared.Constants.Search;
using SW.Shared.Constants.Storage;
using SW.Shared.Helpers.Images;
using SW.Shared.Constants.Delivery;
using SW.Frontend.Microdata;
using SW.Frontend.Microdata.Products;
using SW.Frontend.Microdata.Products.Models;
using SW.Frontend.Models;
using SW.Shared.Models.Filter;
using SW.Frontend.Utilities.Filters;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.SqlClient;
using System.Web.UI;

namespace SW.Frontend.Controllers
{
    public class WorksController : UnityController
    {
        public IDocumentsUOW _documentsUOW;
        public IAccountUOW _accountUOW;
        private static ILog _logger = LogManager.GetLogger(typeof(WorksController));

        public IUnityContainer Unity { get; private set; }

        public WorksController(IDocumentsUOW documentsUOW, IAccountUOW accountUOW)
        {
            Unity = SW.Frontend.App_Start.UnityConfig.GetConfiguredContainer();
            _documentsUOW = documentsUOW;
            _accountUOW = accountUOW;
        }

        #region Page

        //[CompressContent]
        public ActionResult Index(Int32 id)
        {
            var dbWork = _documentsUOW.DocumentsRepository.GetByID(x => x.DocumentId == id);
            if (dbWork == null || dbWork.DocumentStateId != (int)SW.Shared.Constants.Documents.DocumentState.Approved || dbWork.Category.Hidden)
                return HttpNotFound();//  RedirectToAction("Error", "Works");
            if (!String.IsNullOrEmpty(dbWork.Slug))
                return RedirectToAction("details", "works", new { id = dbWork.Slug });
            var dtoWork = MapperManager.Map<DB.Document, DocumentPublic>(dbWork);
            var model = new WorkDetailsViewModel();
            SetupWorkAfterMapping(ref dtoWork, dbWork);
            model.DocumentPublic = dtoWork;
            var author = _accountUOW.UsersRepository.GetByID(dtoWork.AuthorId);
            model.AuthorImageUrl = author.AvatartUrl;
            model.WorksCount = _documentsUOW.DocumentsRepository.GetAll()
                .Count(x => x.AuthorId == author.Id && !x.IsDeleted && x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.Approved);
            model.AuthorRating = author.Rating;
            model.AuthorDescription = author.Description;
            model.FeaturedWorks = _documentsUOW.DocumentsRepository
                ._context.Database
                .SqlQuery<Core.DataLayer.FeaturedWork>("GetFeaturedNewWorks").Take(5);
            ViewBag.WorkId = id;
            ViewBag.MicrodataProductSnippet = GetWorkMicroData(dtoWork, dbWork);
            return View("Index", model);
        }

        [CompressContent]
        //[OutputCache(CacheProfile = "TopLevelOneDay")]
        public ActionResult Details(string id)
        {
            var dbWork = _documentsUOW.DocumentsRepository.GetAll().FirstOrDefault(x => x.Slug == id);
            if (dbWork == null || dbWork.DocumentStateId != (int)SW.Shared.Constants.Documents.DocumentState.Approved || dbWork.Category.Hidden)
                return HttpNotFound();// RedirectToAction("Error", "Works");
            var dtoWork = MapperManager.Map<DB.Document, DocumentPublic>(dbWork);
            SetupWorkAfterMapping(ref dtoWork, dbWork);
            var author = _accountUOW.UsersRepository.GetByID(dtoWork.AuthorId);
            var model = new WorkDetailsViewModel();
            model.DocumentPublic = dtoWork;
            model.AuthorImageUrl = author.AvatartUrl;
            model.WorksCount = _documentsUOW.DocumentsRepository.GetAll()
                .Count(x => x.AuthorId == author.Id && !x.IsDeleted && x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.Approved);
            model.AuthorRating = author.Rating;
            model.AuthorDescription = author.Description;
            model.FeaturedWorks = _documentsUOW.DocumentsRepository
                ._context.Database
                .SqlQuery<Core.DataLayer.FeaturedWork>("GetFeaturedNewWorks").Take(5);
            ViewBag.WorkId = dbWork.Id;
            ViewBag.MicrodataProductSnippet = GetWorkMicroData(dtoWork, dbWork);
            ViewBag.HasSales = _documentsUOW.DocumentSalesRepository.GetAll().Count(x => x.IsCompleted && x.DocumentId == dbWork.DocumentId) > 0;
            return View("Index", model);
        }

        public void SetupWorkAfterMapping(ref DocumentPublic dtoWork, Document dbWork)
        {

            dtoWork.CommentsNumber = _documentsUOW.CommentsRepository.GetAll().Count(x => x.DocumentId == dbWork.DocumentId);
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var userVote = _documentsUOW.DocumentsLikesHistoriesRepository.GetAll().FirstOrDefault(y => y.UserId == userId && y.DocumentId == dbWork.Id);
                dtoWork.IsUserVoted = userVote != null ? true : false;
                dtoWork.IsPositive = userVote != null ? userVote.Rating : false;
                dtoWork.IsPurchased = String.IsNullOrEmpty(userId) ? false : _documentsUOW.DocumentSalesRepository.GetAll().Any(x => x.DocumentId == dbWork.Id && x.BuyerId == userId && x.IsCompleted);
            }
        }

        public string GetWorkMicroData(DocumentPublic dtoWork, Document dbWork)
        {
            IProductRichSnippet productSnippet = Unity.Resolve<IProductRichSnippet>(Microdata.Format.JSONLD.ToString());
            int likes = _documentsUOW.DocumentsLikesHistoriesRepository.GetAll().Count(x => x.DocumentId == dbWork.DocumentId && x.Rating == true);
            int dislikes = _documentsUOW.DocumentsLikesHistoriesRepository.GetAll().Count(x => x.DocumentId == dbWork.DocumentId && x.Rating == false);
            int reviewsCount = dislikes + likes;
            const int MaxFiveStarRating = 5;
            decimal aggregateRating = reviewsCount == 0 ? 0 : Math.Round(MaxFiveStarRating / 100.0M * ((likes / (decimal)(reviewsCount)) * 100.0M), 2);
            var productModel = dtoWork.ConvertToMicrodataProduct(aggregateRating, reviewsCount);
            string result = productSnippet.Build(productModel);
            return result;
        }

        [HttpPost]
        [ValidateInput(false)]
        //[System.Web.Http.Cors.EnableCors(origins: "http://dashbord.sw.com", headers: "*", methods: "*")]
        public ActionResult Preview(string input)
        {
            var obj = JsonConvert.DeserializeObject<DocumentDetails>(input);
            obj.Content = obj.Content.SanitizeHTML();
            var dtoWork = MapperManager.Map<DocumentDetails, SW.Shared.Models.Documents.DocumentPublic>(obj);

            dtoWork.CreatedAt = DateTime.UtcNow;

            var category = _documentsUOW.CategoriesRepository
                    .GetAll()
                    .FirstOrDefault(x => x.CategoryId == obj.CategoryId);
            dtoWork.Category = MapperManager.Map<Category, SW.Shared.Models.Documents.CategoryPreview>(category);

            var type = _documentsUOW.DocumentTypesRepository
                    .GetAll()
                    .FirstOrDefault(x => x.DocumentTypeId == obj.TypeId);
            dtoWork.Type = type.Title;

            if (User.Identity.IsAuthenticated)
            {
                var userId = User.Identity.GetUserId();
                var user = _accountUOW.UsersRepository
                    .GetAll()
                    .FirstOrDefault(x => x.Id == userId);
                dtoWork.Author = MapperManager.Map<AspNetUser, SW.Shared.Models.Account.UserPreview>(user);
            }
            return View(dtoWork);
        }

        [ChildActionOnly]
        public ActionResult Comments(Int32 id)
        {
            //var comments = _documentsUOW.CommentsRepository.GetAll()
            //    .Where(x => x.DocumentId == id)
            //    .ToList()
            //    .Select(x => MapperManager.Map<DB.Comment, DTO.Documents.CommentPreview>(x));

            var userId = User.Identity.GetUserId();
            var comments = _documentsUOW.CommentsRepository
                .GetAll()
                .Where(x => x.DocumentId == id)
                .OrderBy(x => x.PostedAt)
                .ToList()
                .Select(x => new DTO.Documents.CommentPreview()
                {
                    CommentId = x.CommentId,
                    Message = x.Message,
                    PostedAt = x.PostedAt,
                    IsReply = x.IsReply,
                    RepliedTo = MapperManager.Map<AspNetUser, SW.Shared.Models.Account.UserPreview>(x.AspNetUser1),
                    Author = MapperManager.Map<AspNetUser, SW.Shared.Models.Account.UserPreview>(x.AspNetUser),
                    Rating = x.Rating,
                    ReplyDisabled = x.AspNetUser.Id == userId,
                    IsUserVoted = x.CommentsRatingHistories.FirstOrDefault(y => y.UserId == userId) != null ? true : false,
                    IsPositive = x.CommentsRatingHistories.FirstOrDefault(y => y.UserId == userId) != null ? x.CommentsRatingHistories.FirstOrDefault(y => y.UserId == userId).Rating : false,
                    Options = new CommentOptions()
                    {
                        EditEnabled = (DateTime.UtcNow - x.PostedAt).TotalMinutes < 15
                    }
                })
                .ToList();

            return PartialView(comments);
        }
        [Authorize]
        [HttpPost]
        public ActionResult PostComment(Int32 id, SW.Shared.Models.Documents.CommentPreview comment)
        {
            comment.Author = new DTO.Account.UserPreview()
            {
                Id = User.Identity.GetUserId()
            };
            comment.PostedAt = DateTime.UtcNow;

            var userMatch = Regex.Match(comment.Message, @"^(?'name'[^\s]+),\s", RegexOptions.CultureInvariant);
            if (userMatch.Success)
            {
                var userName = userMatch.Groups["name"].Value;
                var repliedUser = _accountUOW.UsersRepository
                    .GetAll()
                    .FirstOrDefault(x => x.UserName == userName);
                if (repliedUser != null)
                {
                    comment.RepliedTo = new DTO.Account.UserPreview();
                    comment.RepliedTo.Id = repliedUser.Id;
                    comment.Message = comment.Message.Replace(userMatch.Groups[0].Value, "");
                }
            }

            var dbcomment = MapperManager.Map<DTO.Documents.CommentPreview, DB.Comment>(comment);
            dbcomment.DocumentId = id;
            _documentsUOW.CommentsRepository.Insert(dbcomment);
            _documentsUOW.CommentsRepository.SaveChanges();
            //return PartialView(comments);
            return RedirectToAction("Index", new { id = dbcomment.DocumentId });
        }

        public ActionResult Error()
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;//редирект на стандартную 404
            return View();
        }

        #endregion

        #region Payment

        [HttpGet]
        public ActionResult Payment(Int64 id)
        {
            var coreDocument = _documentsUOW.DocumentsRepository.GetByID(id);
            if (coreDocument == null || coreDocument.DocumentStateId != (int)SW.Shared.Constants.Documents.DocumentState.Approved)
                return HttpNotFound();// RedirectToAction("Error", "Works");
            var dtoDocument = MapperManager.Map<DB.Document, DocumentPublic>(coreDocument);
            //var dtoDocument = MapperManager.Map<Core.DataLayer.Document, SW.Shared.Models.Documents.DocumentPreview>(coreDocument);
            return View(dtoDocument);
        }

        [HttpPost]
        public ActionResult Payment(PaymentRequest payment)
        {
            var document = _documentsUOW.DocumentsRepository.GetByID(payment.DocumentId);
            if (ModelState.IsValid)
            {
                try
                {
                    /*
                     * Actuall sale
                     */
                    var seller = _accountUOW.UsersRepository.GetByID(document.AuthorId);
                    var occuredAt = DateTime.UtcNow;
                    AspNetUser buyer = null;
                    var userId = HttpContext.User.Identity.GetUserId();
                    buyer = _accountUOW.UsersRepository.GetByID(userId);
                    if (!document.Price.HasValue)
                        throw new ArgumentException("Нельзя купить бесплатную работу");
                    var author = document.AspNetUser;
                    //var feePercentages = author.IndividualFee == 0 ? SW.Shared.Constants.Application.FeePercentages : author.IndividualFee;
                    var feePercentages = author.Documents.Count(x => x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.Approved) > 5 ? 35 : 50;

                    Decimal fee = (document.Price.Value / 100.0M) * feePercentages;
                    Decimal income = document.Price.Value;
                    Decimal profit = income - fee;
                    if (payment.Method == PaymentMethod.ExternalSystem)
                    {
                        throw new ArgumentException("Gone away :(");
                        //UnitPay unitPay = new UnitPay(ConfigurationManager.AppSettings["Unitpay_SecretKey"]);
                        //var desc = "Покупка работы: \"" + document.Title + "\"";
                        //byte[] bytes = Encoding.Unicode.GetBytes(desc);

                        //var newUnitPayment = new unitpay_payments()
                        //{
                        //    documentId = payment.DocumentId,
                        //    email = payment.Email,
                        //    userId = userId,
                        //    sum = Double.Parse(document.Price.ToString()),
                        //    dateCreate = DateTime.UtcNow,
                        //    unitpayId = "0000"
                        //};
                        //_documentsUOW.UnitPaymentsRepository.Insert(newUnitPayment);
                        //_documentsUOW.UnitPaymentsRepository.SaveChanges();
                        ////TODO: remove when released

                        //if (System.Configuration.ConfigurationManager.AppSettings["Simulate_Paying"] != null && bool.Parse(System.Configuration.ConfigurationManager.AppSettings["Simulate_Paying"]))
                        //{
                        //    var successSimulated = false;
                        //    var urlString = "http://localhost:57337/works/HandleUnitpay?method={2}&account={1}&date=2012-10-01 12:32:00&operator=beeline&paymentType=mc&projectId=1&phone=9XXXXXXXXX&sum={0}&sign=ghjsgad7sydahsdjhasd7as2&orderSum={0}&orderCurrency=USD&unitpayId=1234567";
                        //    var URL = new Uri(string.Format(urlString, newUnitPayment.sum, newUnitPayment.id, "pay"));
                        //    var random = new Random();
                        //    if (random.NextDouble() < 0.9)
                        //    {
                        //        try
                        //        {
                        //            var request = (HttpWebRequest)WebRequest.Create(URL);
                        //            var response = (HttpWebResponse)request.GetResponse();
                        //            successSimulated = true;
                        //        }
                        //        catch
                        //        {
                        //        }
                        //    }
                        //    ViewBag.Link = HttpUtility.UrlDecode(unitPay.form(ConfigurationManager.AppSettings["Unitpay_PublicKey"], income.ToString(), newUnitPayment.id.ToString(), desc));
                        //    ViewBag.Success = successSimulated;
                        //    return View("PaymentSimulate");
                        //}

                        //return Redirect(unitPay.form(ConfigurationManager.AppSettings["Unitpay_PublicKey"], income.ToString(), newUnitPayment.id.ToString(), desc));
                    }
                    else
                    {
                        if (!Request.IsAuthenticated)
                            throw new ArgumentException("Этот метод оплаты не поддерживает анонимных пользователей");

                        if (buyer.Balance < document.Price.Value)
                            throw new ArgumentException("Недостаточно средств");
                        if (document.DocumentSales.Any(ds => ds.BuyerId == buyer.Id))
                            throw new ArgumentException("Вы уже покупали эту работу");
                        if (buyer.Id == seller.Id)
                            throw new ArgumentException("Чувак, ты не можешь купить свою работу, это как-то тупо");
                        buyer.Balance -= income;
                    }
                    seller.Balance += profit;
                    var sale = new DocumentSale
                    {
                        SellerId = seller.Id,
                        BuyerId = buyer != null ? buyer.Id : null,
                        DocumentId = document.Id,
                        Income = income,
                        SellerBalance = seller.Balance,
                        BuyerBalance = buyer != null ? buyer.Balance : (decimal?)null,
                        OccuredAt = occuredAt,
                        Fee = fee,
                        BuyerEmail = payment.Email,
                        IsCompleted = true,
                        CompletedAt = DateTime.UtcNow,
                        ReferrerIncome = document.AspNetUser.AspNetUser1 != null ? (document.Price.Value / 100.0M) * (seller.AspNetUser1.ReferralPercent ?? SW.Shared.Constants.Application.ReferrerPercentages) : 0,
                        ReferrerBalance = seller.AspNetUser1 != null ? (decimal?)(seller.AspNetUser1.Balance + ((document.Price.Value / 100.0M) * (seller.AspNetUser1.ReferralPercent ?? SW.Shared.Constants.Application.ReferrerPercentages))) : null,
                    };
                    if (seller.AspNetUser1 != null)
                        seller.AspNetUser1.Balance += (document.Price.Value / 100.0M) * (seller.AspNetUser1.ReferralPercent ?? SW.Shared.Constants.Application.ReferrerPercentages);
                    _documentsUOW.DocumentSalesRepository.Insert(sale);
                    _documentsUOW.Commit();
                    _accountUOW.Commit();
                    /*
                     * issue access tokens
                     */
                    var files = new List<DTO.Storage.FileAndToken>();
                    var tokensIssuer = Unity.Resolve<ITokensIssuer>();
                    foreach (var file in document.UserFiles)
                    {
                        var token = tokensIssuer.Issue(file.UserFileId, payment.Email);
                        files.Add(new DTO.Storage.FileAndToken
                        {
                            Id = file.UserFileId,
                            Title = file.Title,
                            Token = token
                        });
                    }
                    /*
                     * mailing                    
                     */
                    var emailsComponent = Unity.Resolve<IEmailsQueueComponent>();
                    // purchase
                    var purchaseEmailModel = new SW.Shared.Models.Emails.PurchaseEmail()
                    {
                        DocumentId = document.Id,
                        DocumentTitle = document.Title,
                        Files = files
                    };
                    String purchaseEmailBody = FrontendUtilities.Instance.RenderViewToString(
                        ControllerContext,
                        "~/Views/Emails/PurchaseEmailView.cshtml",
                        model: purchaseEmailModel, partial: false
                        );
                    String purchaseEmailTitle = SW.Resources.Emails.PurchaseSuccessTitle;
                    emailsComponent.Push(payment.Email, purchaseEmailTitle, purchaseEmailBody);
                    // sale
                    if (!String.IsNullOrEmpty(seller.Email))
                    {
                        var saleEmailModel = MapperManager.Map<Core.DataLayer.DocumentSale, DTO.Documents.DocumentSalePreview>(sale);
                        var saleEmailBody = FrontendUtilities.Instance.RenderViewToString(
                            ControllerContext,
                            "~/Views/Emails/NewSaleEmailView.cshtml",
                            model: saleEmailModel, partial: false
                        );
                        var saleEmailTitle = SW.Resources.Emails.NewSaleTitle;
                        emailsComponent.Push(seller.Email, saleEmailTitle, saleEmailBody);
                    }
                    /*
                     * notifications
                     */
                    var notificationComponent = Unity.Resolve<INotificationComponent>();
                    notificationComponent.Push(new DTO.Notifications.NotificationDetails
                    {
                        Message = String.Format(SW.Resources.Dashboard.Notifications.NewSale,
                            profit.ToString("N"),
                            document.Title, Application.DefaultCurrencyIconClass, document.DocumentId
                        ),
                        TypeId = SW.Shared.Constants.Notifications.NotificationsType.Sale,
                        UserId = seller.Id
                    });

                    //added buyer to Buyers subscription
                    try
                    {
                        var subscriber = Unity.Resolve<ISubscriber>();
                        subscriber.SubscribeToGroup(payment.Email, DeliveryGroupCode.Buyers);
                    }
                    catch
                    {
                    }

                    var wpm = new SW.Frontend.Models.WorkPaymentModel()
                    {
                        Title = sale.Document.Title,
                        Link = ConfigurationManager.AppSettings["frontendUrl"].ToString() + "/Works/Index/" + sale.DocumentId
                    };
                    return View("PaymentSuccess", wpm);

                }
                catch (Exception ex)
                {
                    ViewBag.Error = ex.Message;
                    var wpm = new SW.Frontend.Models.WorkPaymentModel()
                    {
                        Title = document.Title,
                        Link = ConfigurationManager.AppSettings["frontendUrl"].ToString() + "/Works/Index/" + document.DocumentId,
                    };
                    return View("PaymentFailed", wpm);
                }
            }
            ViewBag.Error = "Не валидные входные данные";
            var wpmFailed = new SW.Frontend.Models.WorkPaymentModel()
            {
                Title = document.Title,
                Link = ConfigurationManager.AppSettings["frontendUrl"].ToString() + "/Works/Index/" + document.DocumentId,
            };
            return View("PaymentFailed", wpmFailed);
        }

        //Success=true&ErrorCode=0&Message=None&Details=&Amount=500&MerchantEmail=sales%40studlearn.com&MerchantName=studlearn.com&OrderId=dcaeeb9c-d51a-401c-833a-f5e821ed5d4a&PaymentId=699008067&TranDate=26.08.2021+21:54:51&BackUrl=https:%2F%2Fstudlearn.com%2F&CompanyName=??+??????+???????+?????????&EmailReq=sales%40studlearn.com&PhonesReq=9811662117
        public ActionResult PaymentSuccess()
        {
            _logger.Info(Request.Url.ToString());

            var orderId = Request.QueryString["OrderId"];
            var unitPayment = _documentsUOW.UnitPaymentsRepository.GetAll().FirstOrDefault(x => x.unitpayId == orderId);
            if (unitPayment == null)
            {
                return RedirectToAction("NotFound");
            }
            var sale = _documentsUOW.DocumentSalesRepository.GetAll().FirstOrDefault(x => x.Id == unitPayment.id);

            if (!sale.IsCompleted || unitPayment.status != 2)
            {
                return RedirectToAction("NotFound");
            }

            if (sale.PaymentService == "tinkoff")
            {
                decimal amount = 0;
                decimal.TryParse((Request.QueryString["Amount"] ?? "0").ToString(), out amount);
                //если данных об оплате нет или сумма в нашей БД не совпадает с суммой от Платежной системы
                var goodAmount = amount / 100M;
                if (sale == null || unitPayment.sum != goodAmount)
                {
                    throw new ArgumentException("Sale is not found.");
                }
            }

            //если покупка оплачена
            if (unitPayment.status == (byte)PaymentStatus.Paied)
            {
                //то находим ссылки доступа на файлы работы, которые были отосланы покупателю на почту
                var documentSale = sale;
                var files = new List<DTO.Storage.FileAndToken>();
                var tokensIssuer = Unity.Resolve<ITokensIssuer>();
                foreach (var file in documentSale.Document.UserFiles)
                {
                    var token = file.UserFileAccessTokens.OrderByDescending(x => x.ExpiresAt).FirstOrDefault();
                    if (token.ExpiresAt < DateTime.Now)
                    {
                        continue;
                    }
                    files.Add(new DTO.Storage.FileAndToken
                    {
                        Id = file.UserFileId,
                        Title = file.Title,
                        Token = token.Token
                    });
                }

                ViewBag.Files = files;
            }
            ViewBag.DocumentId = sale.DocumentId;
            var wpm = new SW.Frontend.Models.WorkPaymentModel()
            {
                Title = sale.Document.Title,
                Link = ConfigurationManager.AppSettings["frontendUrl"].ToString() + "/Works/Index/" + sale.DocumentId
            };

            return View(wpm);
        }
        public ActionResult PaymentFailed()
        {
            _logger.Info(Request.Url.ToString());

            var orderId = Request.QueryString["OrderId"];
            var unitPayment = _documentsUOW.UnitPaymentsRepository.GetAll().FirstOrDefault(x => x.unitpayId == orderId);
            var sale = _documentsUOW.DocumentSalesRepository.GetAll().FirstOrDefault(x => x.Id == unitPayment.id);

            if (sale == null)
            {
                throw new ArgumentException("Sale is not found.");
            }

            string paymentId = "";
            if (sale.PaymentService == "tinkoff")
            {
                paymentId = Request.QueryString["PaymentId"];
            }
            if (sale.PaymentService == "cloudpayment")
            {
                paymentId = orderId.ToString();
            }

            var wpm = new SW.Frontend.Models.WorkPaymentModel()
            {
                Title = sale.Document.Title,
                Link = ConfigurationManager.AppSettings["frontendUrl"].ToString() + "/Works/Index/" + sale.DocumentId,
                PaymentId = paymentId
            };

            return View(wpm);
        }

        #endregion

        #region UnitPay

        #endregion

        public async Task<ActionResult> File(Int32 fileId, Int32 documentId, Guid? token = null)
        {
            //var dbWork = _documentsUOW.DocumentsRepository.GetByID(x=> x.DocumentId == documentId);
            var dbFile = _documentsUOW.UserFilesRepository.GetAll().FirstOrDefault(x => x.UserFileId == fileId && x.Documents.Any(y => y.DocumentId == documentId));
            var dtoFile = MapperManager.Map<DB.UserFile, SW.Shared.Models.Storage.FilePreview>(dbFile);

            FileProxy wrapper = new FileProxy(_documentsUOW, _accountUOW, SW.Shared.Constants.Application.StorageConnectionStringName);
            var file = await wrapper.GetFile(User.Identity.GetUserId(), fileId, documentId, token);
            if (file == null)
                return View();

            ViewBag.WorkId = documentId;
            ViewBag.Token = token;
            return View(dtoFile);
        }

        [RecaptchaMVCFilter]
        [HttpPost]
        public async Task<ActionResult> DownloadFile(Int32 fileId = 0, Int32 documentId = 0, bool CaptchaValid = false, Guid? token = null)
        {
            var dbFile = _documentsUOW.UserFilesRepository.GetAll().FirstOrDefault(x => x.UserFileId == fileId && x.Documents.Any(y => y.DocumentId == documentId));
            var dtoFile = MapperManager.Map<DB.UserFile, SW.Shared.Models.Storage.FilePreview>(dbFile);
            FileProxy wrapper = new FileProxy(_documentsUOW, _accountUOW, SW.Shared.Constants.Application.StorageConnectionStringName);
            var file = await wrapper.GetFile(User.Identity.GetUserId(), fileId, documentId, token);
            if (file == null)
                return View("File", dtoFile);

            ViewBag.WorkId = documentId;
            ViewBag.Token = token;


            if (CaptchaValid == false)
            {
                ModelState.AddModelError(SW.Shared.Constants.Application.RecaptchaKey, Resources.Errors.Recpatcha);
                return View("File", dtoFile);
            }

            ViewBag.StartDownloadFile = true;
            return View("File", dtoFile);
        }

        /// <summary>
        /// Show files list only if work is free
        /// </summary>
        /// <param name="documentId"></param>
        /// <returns></returns>
        public async Task<ActionResult> FilesList(int documentId)
        {
            var document = _documentsUOW.DocumentsRepository.GetByID(documentId);
            if (document == null || (document.Price.HasValue && document.Price > 0))
                return View();
            var dbFiles = _documentsUOW.UserFilesRepository.GetAll()
                .Where(x => x.Documents.Any(d => d.DocumentId == documentId))
                .ToList();
            var dtoFiles = MapperManager.Map<List<DB.UserFile>, List<SW.Shared.Models.Storage.FilePreview>>(dbFiles);
            ViewBag.WorkId = documentId;
            return View(dtoFiles);
        }


        [ChildActionOnly]
        public ActionResult FromAuthors(int id, string authorId)
        {
            var works = _documentsUOW.DocumentsRepository.GetAll()
                .Where(x => x.IsDeleted == false && x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden)
                .Where(x => x.AuthorId == authorId && x.DocumentId != id)
                //.OrderByDescending(x => x.Rating)
                .Take(Application.MaxSimilarWorks)
                .ToList()
                .Select(x => MapperManager.Map<Document, DTO.Documents.DocumentPreviewEx>(x));
            return PartialView(works);
        }

        [ChildActionOnly]
        public ActionResult SimilarWorks(string mode = "SimilarWorks", string moduleTitle = "", int id = -1, string title = "")
        {
            try
            {
                ViewData["moduleTitle"] = moduleTitle;

                IEnumerable<DocumentPreviewEx> result = null;
                switch (mode)
                {
                    case "SimilarWorks":
                        ISearchText components = Unity.Resolve<ISearchText>("lucene");
                        var similarWorks = components
                            .SearchByTitleAndBriefDescription(title, "", SearchType.Approved, Application.MaxSimilarWorks + 1);
                        result = similarWorks
                           .Where(x => x.DocumentId != id)
                           .Take(Application.MaxSimilarWorks)
                          .ToList().Select(x => MapperManager.Map<SimilarDocument, DTO.Documents.DocumentPreviewEx>(x));
                        break;
                    case "NewWorks":
                        var minDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
                        result = _documentsUOW.DocumentsRepository.GetAll()
                .Where(x => x.IsDeleted == false && x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden)
                           .Where(x => x.CreatedAt >= minDate)
                           .OrderByDescending(x => x.CreatedAt)
                           .Take(Application.MaxSimilarWorks)
                           .ToList()
                           .Select(x => MapperManager.Map<Document, DTO.Documents.DocumentPreviewEx>(x));
                        break;
                    default: break;
                }
                return PartialView(result);
            }
            catch (Exception exc)
            {
                _logger.Error(exc);
                return PartialView(new List<DocumentPreviewEx>());
            }
        }

        public ActionResult UnbuyedWorks(bool free = false, string minmax = null, bool recent = false, int[] type = null, int theme = 0, TimePeriodEnum period = TimePeriodEnum.Any, int page = 1, int rows = 10, SortFieldsEnum sort = SortFieldsEnum.Rating, bool direct = false)
        {
            try
            {
                var query = from docs in _documentsUOW.DocumentsRepository._context.Documents
                    .Where(x => x.IsDeleted == false && x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden && !x.IsFree)

                            from sales in _documentsUOW.DocumentSalesRepository._context.DocumentSales
                                .Where(x => x.DocumentId == docs.DocumentId && x.IsCompleted).DefaultIfEmpty()
                            where docs.IsFree == false
                            select new
                            {
                                d = docs,
                                s = sales
                            };
                var worksQuery = query.Where(x => x.s == null).Select(x => x.d);

                int min = 0;
                int max = 10000;
                if (!string.IsNullOrEmpty(minmax) && minmax.Split(new char[] { ';' }).Length == 2)
                {
                    if (!int.TryParse(minmax.Split(new char[] { ';' })[0], out min))
                        throw new InvalidOperationException("Bad request");
                    if (!int.TryParse(minmax.Split(new char[] { ';' })[1], out max))
                        throw new InvalidOperationException("Bad request");
                }

                if (type != null)
                    worksQuery = worksQuery
                    .Where(x => type.Contains(x.TypeId));
                //Filter
                if (free)
                    worksQuery = worksQuery.Where(x => x.Price == null || x.Price == 0);
                else
                {
                    worksQuery = worksQuery.Where(x => (min == 0 ? x.Price == null || (x.Price >= min && x.Price <= max) : (x.Price != null && x.Price >= min && x.Price <= max)));
                }

                if (recent)
                {
                    var minDate = DateTime.UtcNow.Subtract(TimeSpan.FromDays(90));
                    worksQuery = worksQuery.Where(x => x.CreatedAt >= minDate);
                }

                //Sorting
                var sortedWorksQuery = worksQuery.OrderByDescending(x => x.Rating);
                switch (sort)
                {
                    case SortFieldsEnum.Rating:
                        if (direct)
                            sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.Rating).ThenByDescending(x => x.CreatedAt);
                        else
                            sortedWorksQuery = sortedWorksQuery.OrderByDescending(x => x.Rating).ThenByDescending(x => x.CreatedAt);
                        break;
                    case SortFieldsEnum.Date:
                        if (direct)
                            sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.CreatedAt);
                        else
                            sortedWorksQuery = sortedWorksQuery.OrderByDescending(x => x.CreatedAt);
                        break;
                    case SortFieldsEnum.Price:
                        if (direct)
                            sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.Price ?? 0).ThenByDescending(x => x.CreatedAt);
                        else
                            sortedWorksQuery = sortedWorksQuery.OrderByDescending(x => x.Price ?? 0).ThenByDescending(x => x.CreatedAt);
                        break;
                    case SortFieldsEnum.Title:
                        if (direct)
                            sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.Title);
                        else
                            sortedWorksQuery = sortedWorksQuery.OrderByDescending(x => x.Title);
                        break;
                }

                var types = worksQuery.Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.DocumentType.DocumentTypeId, Title = x.DocumentType.Title }).Distinct();

                var viewModel = new CategoryViewModel
                {
                    Documents = sortedWorksQuery
                          .Skip((page - 1) * rows)
                          //.Take(rows)
                          .ToList()
                          .Select(x => MapperManager.Map<Document, DTO.Documents.DocumentPreviewEx>(x)),
                    PagerModel = new PagerModel
                    {
                        Rows = rows,
                        Total = worksQuery.Count(),
                        CurrentPage = page
                    },
                    Filter = new DTO.Filter.WorkFilterItem()
                    {
                        Free = free,
                        PriceRangeMin = min,
                        PriceRangeMax = max,
                        Recent = recent,
                        Types = type,
                        Theme = theme,
                        SelectedPeriod = period
                    },
                    SorterModel = new SorterModel()
                    {
                        SortedField = sort,
                        Ascending = direct
                    },
                    Types = types
                };
                viewModel.PagerModel.LastPage = Convert.ToInt32(Math.Ceiling(viewModel.PagerModel.Total / (decimal)viewModel.PagerModel.Rows));
                viewModel.PagerModel.Count = viewModel.Documents.Count();

                return View(viewModel);
            }
            catch (Exception exc)
            {
                return PartialView(new List<DocumentPreviewEx>());
            }
        }

        [ChildActionOnly]
        public ActionResult CategoriesSiblings(Int32 id)
        {
            var category = _documentsUOW.CategoriesRepository.GetByID(id);
            var siblings = _documentsUOW.CategoriesRepository.GetAll()
                .Where(y => y.Hidden == false)
                .Where(x => x.ParentSectionId == category.ParentSectionId)
                .ToList()
                .Select(x =>
                {
                    var m = MapperManager.Map<DB.Category, DTO.Documents.CategoryPreview>(x);
                    m.Count = x.Documents.Where(z =>
                        z.IsDeleted == false && z.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !z.Category.Hidden).Count();
                    return m;
                });
            ViewBag.CategoryId = category.CategoryId;
            ViewBag.CategoryTitle = category.Title;
            return PartialView("~/Views/Category/Siblings.cshtml", siblings);
        }
    }
}