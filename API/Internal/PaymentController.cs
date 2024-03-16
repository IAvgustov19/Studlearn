using SW.Core.DataLayer.Account;
using SW.Core.DataLayer.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DB = SW.Core.DataLayer;
using DTO = SW.Shared.Models;
using Microsoft.AspNet.Identity;
using SW.Shared.Helpers.Validators;
using Microsoft.Practices.Unity;
using Microsoft.Practices.Unity.Configuration;
using SW.Shared.Helpers.Validators.Extensions;
using System.Collections.Specialized;
using System.Configuration;
using SW.Shared.Models.Security;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SW.Shared.Helpers.Monads;
using SW.Shared.Helpers.Essential;
using SW.Shared.Constants.Payment;
using SW.Workflow.Components.Emails;
using SW.Frontend.Controllers;
using System.Web;
using SW.Workflow.Components.Storage;
using SW.Workflow.Coordinator;
using SW.Workflow.Components.UnitPay;
using log4net;
using SW.Workflow.Components;
using SW.Shared.Constants;
using SW.Shared.Constants.Delivery;
using System.Security.Cryptography;
using System.Text;
using SW.Core.DataLayer.Documents.Repositories;
using SW.Workflow.Components.Files;
using SW.Shared.Models.Documents;
using SW.Core.DataLayer;
using SW.Shared.Constants.Documents;

namespace SW.Frontend.API.Internal
{
    public class MyHmac
    {
        public string CreateToken(string message, string secret)
        {
            secret = secret ?? "";
            var encoding = new System.Text.ASCIIEncoding();
            byte[] keyByte = encoding.GetBytes(secret);
            byte[] messageBytes = encoding.GetBytes(message);
            using (var hmacsha256 = new HMACSHA256(keyByte))
            {
                byte[] hashmessage = hmacsha256.ComputeHash(messageBytes);
                return Convert.ToBase64String(hashmessage);
            }
        }
    }

    public class PaymentController : ApiUnityController
    {
        public readonly IDocumentsUOW _documentsUOW;
        public readonly IAccountUOW _accountUOW;
        INotificationComponent notificationComponent;
        private static ILog _logger = LogManager.GetLogger(typeof(PaymentController));

        public PaymentController(IDocumentsUOW documentsUOW, IAccountUOW accountUOW)
        {
            _documentsUOW = documentsUOW;
            _accountUOW = accountUOW;
            notificationComponent = Unity.Resolve<INotificationComponent>();
        }

        #region Cloudpayment

        [HttpGet]
        [AllowAnonymous]
        [Route("api/internal/payments/cloudpayment/get")]
        public HttpResponseMessage HandleCloudPaymentCheckTesting()
        {
            if (!String.Equals(ConfigurationManager.AppSettings["PaymentEnabled"], "true", StringComparison.OrdinalIgnoreCase))
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Платежи временно отключены, попробуйте позже");
            }

            //read data from request
            var requestBody = Request.Content.ReadAsStringAsync().Result;//

            requestBody = HttpUtility.UrlDecode(requestBody);

            var keyValues = requestBody.Split(new char[1] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            var hashtable = new Dictionary<string, string>();
            foreach (var item in keyValues)
            {
                var parts = item.Split(new char[1] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                var key = parts.Length > 0 ? parts[0] : "";
                var value = parts.Length > 1 ? parts[1] : "";

                hashtable.Add(key, value);
            }

            var additionalData = string.Empty;
            byte paymentSource = 0;
            var documentId = 0;
            if (hashtable.TryGetValue("Data", out additionalData))
            {
                var additionalObject = JObject.Parse(additionalData);
                var paymentSourceString = additionalObject["paymentSource"]?.ToString();
                if (!byte.TryParse(paymentSourceString, out paymentSource))
                {
                    //невалидный запрос
                }
                var documentIdString = additionalObject["documentId"]?.ToString();
                if (!int.TryParse(documentIdString, out documentId))
                {
                    //невалидный запрос
                }
            }
            else
            {
                //невалидный запрос
            }

            string email = hashtable["Email"].ToString();//email плательщика (необязательно)
            string accountId = hashtable["AccountId"].ToString();//идентификатор плательщика (обязательно для создания подписки)
            string status = hashtable["Status"].ToString();//Статус платежа в случае успешного завершения: Completed — для одностадийных платежей, Authorized — для двухстадийных
            string orderId = hashtable["InvoiceId"].ToString();//Номер заказа из параметров платежа
            var paymentAmountString = hashtable["PaymentAmount"].ToString();//Сумма списания
            decimal paymentAmount = 0;
            if (decimal.TryParse(paymentAmountString.Replace(".", ","), out paymentAmount))
            {
            }
            var amountString = hashtable["Amount"].ToString();//Сумма оплаты из параметров платежа
            decimal amount = 0;
            if (decimal.TryParse(amountString.Replace(".", ","), out amount))
            {
            }

            // input validation
            IRule emailRule = Unity.Resolve<IRule>(ValidationRuleType.Email.ToString());
            if (emailRule == null)
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Не могу найти правила валидации e-mail, сообщите разработчикам немедленно");
            if (String.IsNullOrEmpty(email))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "e-mail обязательное поле");
            if (!emailRule.TryValidate(email))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "e-mail не валидный");

            // init
            bool isAuthenticated = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;
            var userId = accountId;// isAuthenticated ? User.Identity.GetUserId() : String.Empty;
            var buyer = String.IsNullOrEmpty(userId) ? (DB.AspNetUser)null : _accountUOW.UsersRepository.GetByID(userId);
            var document = _documentsUOW.DocumentsRepository.GetByID(documentId);
            if (!document.Price.HasValue)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Нельзя выписать счет для бесплатной работы, безумцы <смайл рука-лицо>");
            if (document.DocumentStateId != (int)SW.Shared.Constants.Documents.DocumentState.Approved)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Нельзя выписать счет для работы которая имеет статус отличный от 'Утвержден (Approved)'");
            if (string.Equals(document.AspNetUser.Email, email, StringComparison.InvariantCultureIgnoreCase))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Похоже что вы пытаетесь купить собственную работу");

            //!!!проверим, а не пытаются ли ...
            var isExists = _documentsUOW.DocumentSalesRepository.Any(x => x.unitpay_payments.unitpayId == orderId);
            if (isExists)
            {
                //если есть заказ с таким номером и суммой, TransactionId, недавний, но не оплаченный еще
                //то возвращаем, что принимаем такой платеж
                return Request.CreateResponse(HttpStatusCode.OK, JObject.Parse("{\"code\":0}"));
                //
                //return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Похоже, что вы пытаетесь оплатить другой заказ");
            }

            //string tinkofPercentSetting = ConfigurationManager.AppSettings["TinkofPercent"] ?? "0";
            //decimal tinkofPercent = 0;
            //if (!decimal.TryParse(tinkofPercentSetting.Replace(".", ","), out tinkofPercent))
            //{
            //    decimal.TryParse(tinkofPercentSetting.Replace(",", "."), out tinkofPercent);
            //}
            //var tinkoffPart = document.Price.Value * tinkofPercent * 0.01M;
            //if (tinkoffPart <= 3.49M)
            //{
            //    tinkoffPart = 3.49M;
            //}
            ////округление до 2х цифр после запятой, иначе тиньков сумму неверно понимает
            //var sumWithTinkoffPercent = System.Math.Round(document.Price.Value + tinkoffPart, 2);

            //
            // insert payment information to the DB
            //
            var author = document.AspNetUser;
            //var feePercentages = 45;// author.IndividualFee == 0 ? 45/*SW.Shared.Constants.Application.FeePercentages*/ : author.IndividualFee;
            var feePercentages = author.Documents.Count(x => x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.Approved) > 5 ? 35 : 50;

            Decimal fee = (document.Price.Value / 100.0M) * feePercentages;
            Decimal income = document.Price.Value;
            Decimal profit = income - fee;
            DateTime now = DateTime.UtcNow;
            var sale = new DB.DocumentSale
            {
                SellerId = document.AuthorId,
                BuyerId = buyer == null ? null : buyer.Id,
                DocumentId = document.DocumentId,
                Income = income,
                SellerBalance = document.AspNetUser.Balance,
                BuyerBalance = buyer == null ? (decimal?)null : buyer.Balance,
                OccuredAt = now,
                Fee = fee,
                BuyerEmail = email,
                IsCompleted = false,
                PaymentSource = paymentSource,
                PaymentService = "cloudpayment",
                unitpay_payments = new DB.unitpay_payments
                {
                    unitpayId = orderId, //Guid.NewGuid().ToString(),
                    email = email,
                    sum = System.Math.Round(document.Price.Value, 2),//sumWithTinkoffPercent,//
                    itemsCount = 1,
                    dateCreate = now,
                    dateComplete = null,
                    status = (byte)PaymentStatus.Created,
                    error = null,
                    profit = document.Price.Value
                },
                ReferrerIncome = document.AspNetUser.AspNetUser1 != null ? (document.Price.Value / 100.0M) * (document.AspNetUser.AspNetUser1.ReferralPercent ?? SW.Shared.Constants.Application.ReferrerPercentages) : 0
            };

            _documentsUOW.DocumentSalesRepository.Insert(sale);
            _documentsUOW.Commit();

            // Отправить на почту invoce с сылкой
            var invoiceEmail = new SW.Shared.Models.Emails.InvoiceEmail()
            {
                DocumentTitle = document.Title,
                InvoiceUrl = this.Url.Link("Default", new { Controller = "Works", Action = "Payment", id = document.DocumentId, src = 1 }),
                DocumentPrice = System.Math.Round(document.Price.Value, 2)
            };
            var emailsComponent = Unity.Resolve<IEmailsQueueComponent>();
            var context = new System.Web.HttpContextWrapper(System.Web.HttpContext.Current);
            var routeData = new System.Web.Routing.RouteData();
            routeData.Values.Add("controller", "EmailsController");
            var controllerContext = new System.Web.Mvc.ControllerContext(context, routeData, new EmailsController());
            String purchaseEmailBody = SW.Frontend.Utilities.FrontendUtilities.Instance.RenderViewToString(
                            controllerContext,
                            "~/Views/Emails/InvoiceEmailView.cshtml",
                            model: invoiceEmail, partial: false
                            );
            String purchaseEmailTitle = SW.Resources.Emails.InvoiceTitle;
            emailsComponent.Push(email, purchaseEmailTitle, purchaseEmailBody);

            var result = JObject.Parse("{\"code\":0}");
            return Request.CreateResponse(HttpStatusCode.OK, result);
        }

        private Dictionary<string, string> ReadDataFromCloudpayments(string requestBody)
        {
            requestBody = HttpUtility.UrlDecode(requestBody);

            var keyValues = requestBody.Split(new char[1] { '&' }, StringSplitOptions.RemoveEmptyEntries);
            var hashtable = new Dictionary<string, string>();
            foreach (var item in keyValues)
            {
                var parts = item.Split(new char[1] { '=' }, StringSplitOptions.RemoveEmptyEntries);
                var key = parts.Length > 0 ? parts[0] : "";
                var value = parts.Length > 1 ? parts[1] : "";

                hashtable.Add(key, value);
            }

            return hashtable;
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("api/internal/payments/cloudpayment/check")]
        public HttpResponseMessage HandleCloudPaymentCheck()
        {
            try
            {
                if (!String.Equals(ConfigurationManager.AppSettings["PaymentEnabled"], "true", StringComparison.OrdinalIgnoreCase))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Платежи временно отключены, попробуйте позже");
                }

                var requestBody = Request.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(requestBody))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "no data");
                }

                _logger.Info("test HandleCloudPaymentCheck " + requestBody);


                //TODO: проверка целостности запроса от Cloudpayment https://developers.cloudpayments.ru/#proverka-uvedomleniy
                string cloudpaymentSecretKey = System.Configuration.ConfigurationManager.AppSettings["CloudpaymentSecretKey"] ?? "";
                if (string.IsNullOrEmpty(cloudpaymentSecretKey))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Отсутствует CloudpaymentSecretKey");
                }
                //var xtoken = Request.Headers.GetValues("X-Content-HMAC").FirstOrDefault();
                var token = Request.Headers.GetValues("Content-HMAC").FirstOrDefault();
                //var xsiteToken = new MyHmac().CreateToken(HttpUtility.UrlDecode(requestBody), cloudpaymentSecretKey);
                var siteToken = new MyHmac().CreateToken(requestBody, cloudpaymentSecretKey);
                //_logger.Info("xtoken=" + xtoken + "                 token=" + token);
                //_logger.Info("xsiteToken=" + xsiteToken + "                 siteToken=" + siteToken);

                if (token != siteToken)
                {
                    _logger.Info("bad token, token=" + token + "                 siteToken=" + siteToken);
                    return Request.CreateResponse(HttpStatusCode.OK, JObject.Parse("{\"code\":13}"));
                }

                //read data from request
                var hashtable = ReadDataFromCloudpayments(requestBody);

                var additionalData = string.Empty;
                byte paymentSource = 0;
                var documentId = 0;
                if (hashtable.TryGetValue("Data", out additionalData))
                {
                    var additionalObject = JObject.Parse(additionalData);
                    var paymentSourceString = additionalObject["paymentSource"]?.ToString();
                    if (!byte.TryParse(paymentSourceString, out paymentSource))
                    {
                        //невалидный запрос
                    }
                    var documentIdString = additionalObject["documentId"]?.ToString();
                    if (!int.TryParse(documentIdString, out documentId))
                    {
                        //невалидный запрос
                    }
                }
                else
                {
                    //невалидный запрос
                }

                string email = hashtable["Email"].ToString();//email плательщика (необязательно)
                string accountId = hashtable["AccountId"].ToString();//идентификатор плательщика (обязательно для создания подписки)
                string status = hashtable["Status"].ToString();//Статус платежа в случае успешного завершения: Completed — для одностадийных платежей, Authorized — для двухстадийных
                string orderId = hashtable["InvoiceId"].ToString();//Номер заказа из параметров платежа
                var paymentAmountString = hashtable["PaymentAmount"].ToString();//Сумма списания
                decimal paymentAmount = 0;
                if (!decimal.TryParse(paymentAmountString.Replace(".", ","), out paymentAmount))
                {
                    //невалидный запрос
                }
                var amountString = hashtable["Amount"].ToString();//Сумма оплаты из параметров платежа
                decimal amount = 0;
                if (!decimal.TryParse(amountString.Replace(".", ","), out amount))
                {
                    //невалидный запрос
                }

                // input validation
                IRule emailRule = Unity.Resolve<IRule>(ValidationRuleType.Email.ToString());
                if (emailRule == null)
                    return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Не могу найти правила валидации e-mail, сообщите разработчикам немедленно");
                if (String.IsNullOrEmpty(email))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "e-mail обязательное поле");
                if (!emailRule.TryValidate(email))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "e-mail не валидный");

                // init
                bool isAuthenticated = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;
                var userId = accountId;// isAuthenticated ? User.Identity.GetUserId() : String.Empty;
                var buyer = String.IsNullOrEmpty(userId) ? (DB.AspNetUser)null : _accountUOW.UsersRepository.GetByID(userId);
                var document = _documentsUOW.DocumentsRepository.GetByID(documentId);
                if (!document.Price.HasValue)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Нельзя выписать счет для бесплатной работы, безумцы <смайл рука-лицо>");
                if (document.DocumentStateId != (int)SW.Shared.Constants.Documents.DocumentState.Approved)
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Нельзя выписать счет для работы которая имеет статус отличный от 'Утвержден (Approved)'");
                if (string.Equals(document.AspNetUser.Email, email, StringComparison.InvariantCultureIgnoreCase))
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Похоже что вы пытаетесь купить собственную работу");

                //!!!проверим, а не пытаются ли ...
                var isExists = _documentsUOW.DocumentSalesRepository.Any(x => x.unitpay_payments.unitpayId == orderId);
                if (isExists)
                {
                    //если есть заказ с таким номером и суммой, TransactionId, недавний, но не оплаченный еще
                    //то возвращаем, что принимаем такой платеж
                    return Request.CreateResponse(HttpStatusCode.OK, JObject.Parse("{\"code\":0}"));
                    //
                    //return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Похоже, что вы пытаетесь оплатить другой заказ");
                }

                //string tinkofPercentSetting = ConfigurationManager.AppSettings["TinkofPercent"] ?? "0";
                //decimal tinkofPercent = 0;
                //if (!decimal.TryParse(tinkofPercentSetting.Replace(".", ","), out tinkofPercent))
                //{
                //    decimal.TryParse(tinkofPercentSetting.Replace(",", "."), out tinkofPercent);
                //}
                //var tinkoffPart = document.Price.Value * tinkofPercent * 0.01M;
                //if (tinkoffPart <= 3.49M)
                //{
                //    tinkoffPart = 3.49M;
                //}
                ////округление до 2х цифр после запятой, иначе тиньков сумму неверно понимает
                //var sumWithTinkoffPercent = System.Math.Round(document.Price.Value + tinkoffPart, 2);

                //
                // insert payment information to the DB
                //
                var author = document.AspNetUser;
                //var feePercentages = 45;// author.IndividualFee == 0 ? 45/*SW.Shared.Constants.Application.FeePercentages*/ : author.IndividualFee;
                var feePercentages = author.Documents.Count(x => x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.Approved) > 5 ? 35 : 50;

                Decimal fee = (document.Price.Value / 100.0M) * feePercentages;
                Decimal income = document.Price.Value;
                Decimal profit = income - fee;
                DateTime now = DateTime.UtcNow;
                var sale = new DB.DocumentSale
                {
                    SellerId = document.AuthorId,
                    BuyerId = buyer == null ? null : buyer.Id,
                    DocumentId = document.DocumentId,
                    Income = income,
                    SellerBalance = document.AspNetUser.Balance,
                    BuyerBalance = buyer == null ? (decimal?)null : buyer.Balance,
                    OccuredAt = now,
                    Fee = fee,
                    BuyerEmail = email,
                    IsCompleted = false,
                    PaymentSource = paymentSource,
                    PaymentService = "cloudpayment",
                    unitpay_payments = new DB.unitpay_payments
                    {
                        unitpayId = orderId, //Guid.NewGuid().ToString(),
                        email = email,
                        sum = System.Math.Round(document.Price.Value, 2),//sumWithTinkoffPercent,//
                        itemsCount = 1,
                        dateCreate = now,
                        dateComplete = null,
                        status = (byte)PaymentStatus.Created,
                        error = null,
                        profit = document.Price.Value
                    },
                    ReferrerIncome = document.AspNetUser.AspNetUser1 != null ? (document.Price.Value / 100.0M) * (document.AspNetUser.AspNetUser1.ReferralPercent ?? SW.Shared.Constants.Application.ReferrerPercentages) : 0
                };

                _documentsUOW.DocumentSalesRepository.Insert(sale);
                _documentsUOW.Commit();

                // Отправить на почту invoce с сылкой
                var invoiceEmail = new SW.Shared.Models.Emails.InvoiceEmail()
                {
                    DocumentTitle = document.Title,
                    InvoiceUrl = this.Url.Link("Default", new { Controller = "Works", Action = "Payment", id = document.DocumentId, src = 1 }),
                    DocumentPrice = System.Math.Round(document.Price.Value, 2)
                };
                var emailsComponent = Unity.Resolve<IEmailsQueueComponent>();
                var context = new System.Web.HttpContextWrapper(System.Web.HttpContext.Current);
                var routeData = new System.Web.Routing.RouteData();
                routeData.Values.Add("controller", "EmailsController");
                var controllerContext = new System.Web.Mvc.ControllerContext(context, routeData, new EmailsController());
                String purchaseEmailBody = SW.Frontend.Utilities.FrontendUtilities.Instance.RenderViewToString(
                                controllerContext,
                                "~/Views/Emails/InvoiceEmailView.cshtml",
                                model: invoiceEmail, partial: false
                                );
                String purchaseEmailTitle = SW.Resources.Emails.InvoiceTitle;
                emailsComponent.Push(email, purchaseEmailTitle, purchaseEmailBody);

                return Request.CreateResponse(HttpStatusCode.OK, JObject.Parse("{\"code\":0}"));//0	Платеж может быть проведен	Система выполнит авторизацию платежа
            }
            catch (Exception exc)
            {
                _logger.Error("test HandleCloudPaymentCheck ERROR " + exc.ToString());

                return Request.CreateResponse(HttpStatusCode.OK, JObject.Parse("{\"code\":13}"));//13	Платеж не может быть принят	Платеж будет отклонен
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("api/internal/payments/cloudpayment/pay")]
        public HttpResponseMessage HandleCloudPaymentPay()
        {
            try
            {
                var requestBody = Request.Content.ReadAsStringAsync().Result;
                if (string.IsNullOrEmpty(requestBody))
                {
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "no data");
                }

                _logger.Info("test HandleCloudPaymentPay " + requestBody);


                //TODO: проверка целостности запроса от Cloudpayment https://developers.cloudpayments.ru/#proverka-uvedomleniy
                string cloudpaymentSecretKey = System.Configuration.ConfigurationManager.AppSettings["CloudpaymentSecretKey"] ?? "";
                if (string.IsNullOrEmpty(cloudpaymentSecretKey))
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Отсутствует CloudpaymentSecretKey");
                }
                //var xtoken = Request.Headers.GetValues("X-Content-HMAC").FirstOrDefault();
                var token = Request.Headers.GetValues("Content-HMAC").FirstOrDefault();
                //var xsiteToken = new MyHmac().CreateToken(HttpUtility.UrlDecode(requestBody), cloudpaymentSecretKey);
                var siteToken = new MyHmac().CreateToken(requestBody, cloudpaymentSecretKey);
                //_logger.Info("xtoken=" + xtoken + "                 token=" + token);
                //_logger.Info("xsiteToken=" + xsiteToken + "                 siteToken=" + siteToken);

                if (token != siteToken)
                {
                    _logger.Info("bad token, token=" + token + "                 siteToken=" + siteToken);
                    return Request.CreateResponse(HttpStatusCode.OK, JObject.Parse("{\"code\":13}"));
                }

                var hashtable = ReadDataFromCloudpayments(requestBody);

                var additionalData = string.Empty;
                byte paymentSource = 0;
                var documentId = 0;
                if (hashtable.TryGetValue("Data", out additionalData))
                {
                    var additionalObject = JObject.Parse(additionalData);
                    var paymentSourceString = additionalObject["paymentSource"]?.ToString();
                    if (!byte.TryParse(paymentSourceString, out paymentSource))
                    {
                        //невалидный запрос
                    }
                    var documentIdString = additionalObject["documentId"]?.ToString();
                    if (!int.TryParse(documentIdString, out documentId))
                    {
                        //невалидный запрос
                    }
                }
                else
                {
                    //невалидный запрос
                }

                string email = hashtable["Email"].ToString();//email плательщика (необязательно)
                string accountId = hashtable["AccountId"].ToString();//идентификатор плательщика (обязательно для создания подписки)
                string status = hashtable["Status"].ToString();//Статус платежа в случае успешного завершения: Completed — для одностадийных платежей, Authorized — для двухстадийных
                string orderId = hashtable["InvoiceId"].ToString();//Номер заказа из параметров платежа
                var paymentAmountString = hashtable["PaymentAmount"].ToString();//Сумма списания
                decimal paymentAmount = 0;
                if (!decimal.TryParse(paymentAmountString.Replace(".", ","), out paymentAmount))
                {
                    //невалидный запрос
                }
                var amountString = hashtable["Amount"].ToString();//Сумма оплаты из параметров платежа
                decimal amount = 0;
                if (!decimal.TryParse(amountString.Replace(".", ","), out amount))
                {
                    //невалидный запрос
                }

                //токен валидный, обрабатываем платеж
                var unitPayment = _documentsUOW.UnitPaymentsRepository.GetAll().FirstOrDefault(x => x.unitpayId == orderId);
                var documentSale = _documentsUOW.DocumentSalesRepository.GetAll().FirstOrDefault(x => x.Id == unitPayment.id);
                var paymentFromDocumentSale = documentSale.unitpay_payments;
                if (documentSale == null)
                {
                    _logger.Error(paymentFromDocumentSale.unitpayId + ": " + "We did not find payment information in StudLearn system");
                    return Request.CreateResponse(HttpStatusCode.Accepted, "Мы не нашли информацию о платеже в системе StudLearn");
                }
                if (paymentFromDocumentSale.status == (byte)PaymentStatus.Paied)
                {
                    _logger.Error(paymentFromDocumentSale.unitpayId + ": " + "The sale has already been made");
                    return Request.CreateResponse(HttpStatusCode.Accepted, "Продажа уже произведена");
                }
                if (paymentFromDocumentSale.status == (byte)PaymentStatus.Error)
                {
                    _logger.Error(paymentFromDocumentSale.unitpayId + ": " + "The sale was completed with an error");
                    return Request.CreateResponse(HttpStatusCode.Accepted, "Продажа была завершена с ошибкой");
                }

                //если оплатили фейковую сумму (подделали клиентский скрипт), то есть не ту, которую реально стоит документ, то возвращаем 
                if (documentSale.unitpay_payments.sum != amount)
                {
                    _logger.Error(paymentFromDocumentSale.unitpayId + ": " + "Amount does not match");
                    return Request.CreateResponse(HttpStatusCode.Accepted, "Сумма не совпадает");
                }


                if (status == "Completed")
                {
                    // обновить балансы всех участников сделки
                    var author = documentSale.AspNetUser;
                    // var feePercentages = 45;// author.IndividualFee == 0 ? SW.Shared.Constants.Application.FeePercentages : author.IndividualFee;
                    var feePercentages = author.Documents.Count(x => x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.Approved) > 5 ? 35 : 50;

                    documentSale.AspNetUser.Balance += documentSale.Income - ((documentSale.Income / 100.0M) * feePercentages);
                    documentSale.SellerBalance = documentSale.AspNetUser.Balance;
                    if (documentSale.AspNetUser.AspNetUser1 != null)
                        documentSale.AspNetUser.AspNetUser1.Balance += documentSale.ReferrerIncome;

                    // разослать письма и нотификейшены
                    var files = new List<DTO.Storage.FileAndToken>();
                    var tokensIssuer = Unity.Resolve<ITokensIssuer>();
                    foreach (var file in documentSale.Document.UserFiles)
                    {
                        var tokenissue = tokensIssuer.Issue(file.UserFileId, documentSale.unitpay_payments.email);
                        files.Add(new DTO.Storage.FileAndToken
                        {
                            Id = file.UserFileId,
                            Title = file.Title,
                            Token = tokenissue
                        });
                    }
                    var emailsComponent = Unity.Resolve<IEmailsQueueComponent>();
                    // purchase email
                    var purchaseEmailModel = new SW.Shared.Models.Emails.PurchaseEmail()
                    {
                        DocumentId = documentSale.Document.Id,
                        DocumentTitle = documentSale.Document.Title,
                        Files = files
                    };
                    var context = new System.Web.HttpContextWrapper(System.Web.HttpContext.Current);
                    var routeData = new System.Web.Routing.RouteData();
                    routeData.Values.Add("controller", "EmailsController");
                    var controllerContext = new System.Web.Mvc.ControllerContext(context, routeData, new EmailsController());
                    String purchaseEmailBody = SW.Frontend.Utilities.FrontendUtilities.Instance.RenderViewToString(
                        controllerContext,
                        "~/Views/Emails/PurchaseEmailView.cshtml",
                        model: purchaseEmailModel, partial: false
                        );
                    String purchaseEmailTitle = SW.Resources.Emails.PurchaseSuccessTitle;
                    emailsComponent.Push(documentSale.unitpay_payments.email, purchaseEmailTitle, purchaseEmailBody);

                    // завершить оплату
                    //string tinkofPercentSetting = ConfigurationManager.AppSettings["TinkofPercent"] ?? "0";
                    //decimal tinkofPercent = 0;
                    //if (!decimal.TryParse(tinkofPercentSetting.Replace(".", ","), out tinkofPercent))
                    //{
                    //    decimal.TryParse(tinkofPercentSetting.Replace(",", "."), out tinkofPercent);
                    //}

                    documentSale.IsCompleted = true;
                    documentSale.CompletedAt = DateTime.UtcNow;
                    documentSale.unitpay_payments.status = (byte)PaymentStatus.Paied;
                    documentSale.unitpay_payments.dateComplete = DateTime.UtcNow;

                    //var tinkoffPart = documentSale.unitpay_payments.sum * tinkofPercent * 0.01M;
                    //if (tinkoffPart <= 3.49M)
                    //{
                    //    tinkoffPart = 3.49M;
                    //}

                    //documentSale.unitpay_payments.profit = documentSale.unitpay_payments.sum - tinkoffPart;

                    if (documentSale.AspNetUser.AspNetUser1 != null)
                        documentSale.ReferrerBalance = documentSale.AspNetUser.AspNetUser1.Balance;
                    _documentsUOW.Commit();

                    //// sale email
                    var seller = documentSale.AspNetUser;
                    var saleEmailModel = MapperManager.Map<Core.DataLayer.DocumentSale, DTO.Documents.DocumentSalePreview>(documentSale);
                    //skip email sending if user unsubscribe "Authors" category
                    var subsription = _accountUOW.SubscriptionsRepository.GetAll()
                        .FirstOrDefault(x => x.Email == seller.Email);
                    if (!String.IsNullOrEmpty(seller.Email) && subsription != null && subsription.DeliveryGroups.FirstOrDefault(x => x.Code == "Authors") != null)
                    {
                        var saleEmailBody = SW.Frontend.Utilities.FrontendUtilities.Instance.RenderViewToString(
                            controllerContext,
                            "~/Views/Emails/NewSaleEmailView.cshtml",
                            model: saleEmailModel, partial: false
                        );
                        var saleEmailTitle = SW.Resources.Emails.NewSaleTitle;
                        emailsComponent.Push(seller.Email, saleEmailTitle, saleEmailBody);
                    }

                    ////notification

                    Decimal profit = saleEmailModel.Income - saleEmailModel.Fee;
                    var notificationComponent = Unity.Resolve<INotificationComponent>();
                    notificationComponent.Push(new DTO.Notifications.NotificationDetails
                    {
                        Message = String.Format(SW.Resources.Dashboard.Notifications.NewSale,
                            profit.ToString("N"),
                            saleEmailModel.DocumentTitle, Application.DefaultCurrencyIconClass, saleEmailModel.DocumentId
                        ),
                        TypeId = SW.Shared.Constants.Notifications.NotificationsType.Sale,
                        UserId = seller.Id
                    });

                    try
                    {
                        var subscriber = Unity.Resolve<ISubscriber>();
                        subscriber.SubscribeToGroup(documentSale.unitpay_payments.email, DeliveryGroupCode.Buyers);
                    }
                    catch
                    {
                    }
                }

                _logger.Info(documentSale.unitpay_payments.unitpayId + ": " + " HandleCloudPaymentPay Request processed successfully");

                return Request.CreateResponse(HttpStatusCode.OK, JObject.Parse("{\"code\":0}"));//0	Платеж зарегистрирован
            }
            catch (Exception exc)
            {
                _logger.Error("test HandleCloudPaymentPay ERROR " + exc.ToString());
                return Request.CreateResponse(HttpStatusCode.OK, JObject.Parse("{\"code\":0}"));//0	Платеж зарегистрирован
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("api/internal/payments/cloudpayment/fail")]
        public HttpResponseMessage HandleCloudPaymentFail()
        {
            var requestBody = Request.Content.ReadAsStringAsync().Result;
            if (string.IsNullOrEmpty(requestBody))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "no data");
            }

            _logger.Info("test HandleCloudPaymentFail " + requestBody);
            return Request.CreateResponse(HttpStatusCode.OK, JObject.Parse("{\"code\":0}"));
        }

        #endregion

        #region TinkoffPayment
        [HttpPost]
        [AllowAnonymous]
        [Route("api/internal/payments/{documentId}/init")]
        public HttpResponseMessage InitTinkoffPayment(Int64 documentId, String email = "", byte paymentSource = 0)
        {
            if (!String.Equals(ConfigurationManager.AppSettings["PaymentEnabled"], "true", StringComparison.OrdinalIgnoreCase))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Платежи временно отключены, попробуйте позже");

            // input validation
            IRule emailRule = Unity.Resolve<IRule>(ValidationRuleType.Email.ToString());
            if (emailRule == null)
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Не могу найти правила валидации e-mail, сообщите разработчикам немедленно");
            if (String.IsNullOrEmpty(email))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "e-mail обязательное поле");
            if (!emailRule.TryValidate(email))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "e-mail не валидный");

            // init
            bool isAuthenticated = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;
            var userId = isAuthenticated ? User.Identity.GetUserId() : String.Empty;
            var buyer = String.IsNullOrEmpty(userId) ? (DB.AspNetUser)null : _accountUOW.UsersRepository.GetByID(userId);
            var document = _documentsUOW.DocumentsRepository.GetByID(documentId);
            if (!document.Price.HasValue)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Нельзя выписать счет для бесплатной работы, безумцы <смайл рука-лицо>");
            if (document.DocumentStateId != (int)SW.Shared.Constants.Documents.DocumentState.Approved)
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Нельзя выписать счет для работы которая имеет статус отличный от 'Утвержден (Approved)'");
            if (string.Equals(document.AspNetUser.Email, email, StringComparison.InvariantCultureIgnoreCase))
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Похоже что вы пытаетесь купить собственную работу");

            ////проверим, а не пытаются ли ...
            //var isExists = _documentsUOW.DocumentSalesRepository.Any(x => x.unitpay_payments.unitpayId == orderId);
            //if (isExists)
            //    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Похоже, что вы пытаетесь оплатить другой заказ");

            //string tinkofPercentSetting = ConfigurationManager.AppSettings["TinkofPercent"] ?? "0";
            //decimal tinkofPercent = 0;
            //if (!decimal.TryParse(tinkofPercentSetting.Replace(".", ","), out tinkofPercent))
            //{
            //    decimal.TryParse(tinkofPercentSetting.Replace(",", "."), out tinkofPercent);
            //}
            //var tinkoffPart = document.Price.Value * tinkofPercent * 0.01M;
            //if (tinkoffPart <= 3.49M)
            //{
            //    tinkoffPart = 3.49M;
            //}
            ////округление до 2х цифр после запятой, иначе тиньков сумму неверно понимает
            //var sumWithTinkoffPercent = System.Math.Round(document.Price.Value + tinkoffPart, 2);

            //
            // insert payment information to the DB
            //
            var author = document.AspNetUser;
            //var feePercentages = author.IndividualFee == 0 ? SW.Shared.Constants.Application.FeePercentages : author.IndividualFee;
            var feePercentages = author.Documents.Count(x => x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.Approved) > 5 ? 35 : 50;
            Decimal fee = (document.Price.Value / 100.0M) * feePercentages;
            Decimal income = document.Price.Value;
            Decimal profit = income - fee;
            DateTime now = DateTime.UtcNow;
            var sale = new DB.DocumentSale
            {
                SellerId = document.AuthorId,
                BuyerId = buyer == null ? null : buyer.Id,
                DocumentId = document.DocumentId,
                Income = income,
                SellerBalance = document.AspNetUser.Balance,
                BuyerBalance = buyer == null ? (decimal?)null : buyer.Balance,
                OccuredAt = now,
                Fee = fee,
                BuyerEmail = email,
                IsCompleted = false,
                PaymentSource = paymentSource,
                PaymentService = "tinkoff",
                unitpay_payments = new DB.unitpay_payments
                {
                    unitpayId = Guid.NewGuid().ToString(),
                    email = email,
                    sum = System.Math.Round(document.Price.Value, 2),//sumWithTinkoffPercent,//
                    itemsCount = 1,
                    dateCreate = now,
                    dateComplete = null,
                    status = (byte)PaymentStatus.Created,
                    error = null,
                    profit = document.Price.Value
                },
                ReferrerIncome = document.AspNetUser.AspNetUser1 != null ? (document.Price.Value / 100.0M) * (document.AspNetUser.AspNetUser1.ReferralPercent ?? SW.Shared.Constants.Application.ReferrerPercentages) : 0
            };

            //unitPay = new UnitPay(ConfigurationManager.AppSettings["UnitPaySecretKey"], Request.RequestUri);

            _documentsUOW.DocumentSalesRepository.Insert(sale);
            _documentsUOW.Commit();

            // Отправить на почту invoce с сылкой
            var invoiceEmail = new SW.Shared.Models.Emails.InvoiceEmail()
            {
                DocumentTitle = document.Title,
                InvoiceUrl = this.Url.Link("Default", new { Controller = "Works", Action = "Payment", id = document.DocumentId, src = 1 }),
                DocumentPrice = System.Math.Round(document.Price.Value, 2)
            };

            var emailsComponent = Unity.Resolve<IEmailsQueueComponent>();
            var context = new System.Web.HttpContextWrapper(System.Web.HttpContext.Current);
            var routeData = new System.Web.Routing.RouteData();
            routeData.Values.Add("controller", "EmailsController");
            var controllerContext = new System.Web.Mvc.ControllerContext(context, routeData, new EmailsController());
            String purchaseEmailBody = SW.Frontend.Utilities.FrontendUtilities.Instance.RenderViewToString(
                            controllerContext,
                            "~/Views/Emails/InvoiceEmailView.cshtml",
                            model: invoiceEmail, partial: false
                            );
            String purchaseEmailTitle = SW.Resources.Emails.InvoiceTitle;
            emailsComponent.Push(email, purchaseEmailTitle, purchaseEmailBody);

            return Request.CreateResponse(HttpStatusCode.OK,/*orderId*/ sale.unitpay_payments.unitpayId);
        }

        //success {"TerminalKey":"1628257385563","OrderId":"1629920587988.9","Success":true,"Status":"AUTHORIZED","PaymentId":697184971,"ErrorCode":"0","Amount":100,"CardId":98089037,"Pan":"220220******8093","ExpDate":"0723","Token":"e34f324085ef95f66dcc4fa546bfe70bbae9f2c57f7aa33f29e4d4f18ff75aee"}
        //error
        //canceled
        [HttpPost]
        [AllowAnonymous]
        [Route("api/internal/payments/handler")]
        public HttpResponseMessage HandleTinkoffPayment()
        {
            var requestBody = Request.Content.ReadAsStringAsync().Result;
            if (string.IsNullOrEmpty(requestBody))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "no data");
            }

            _logger.Info("test HandlePayment " + requestBody);
            var json = JObject.Parse(requestBody);

            /* status: Создан
             — AUTHORIZED Зарезервирован
            — REVERSED Резервирование отменено
            — CONFIRMED Подтвержден
            — PARTIAL_REFUNDED Возвращен частично
            — PARTIAL_REVERSED Резервирование отменено частично
            — REFUNDED Возвращен полностью
            — REJECTED Отклонен
             */

            var hashtable = new Dictionary<string, string>();
            foreach (var property in json)
            {
                if (property.Key == "Success")
                {
                    hashtable.Add(property.Key, property.Value.ToString().ToLower());
                    continue;
                }
                hashtable.Add(property.Key, property.Value.ToString());
            }

            hashtable.Remove("Token");

            var password = ConfigurationManager.AppSettings["TinkofPassword"];
            if (string.IsNullOrEmpty(password))
            {
                _logger.Error("TinkofPassword is empty");
                return Request.CreateResponse(HttpStatusCode.BadRequest, "invalid");
            }

            hashtable.Add("Password", password);
            var orderedKeys = hashtable.Keys.Cast<string>().OrderBy(c => c);
            var orderedList = (from x in orderedKeys select new KeyValuePair<string, string>(x, hashtable[x].ToString())).ToList();

            var concat = "";
            for (int i = 0; i < orderedList.Count; i++)
            {
                concat += orderedList[i].Value;
            }

            string token = json["Token"].ToString();//Подпись запроса
            string status = json["Status"].ToString();//Статус платежа
            string success = json["Success"].ToString();//Выполнение платежа
            string orderId = json["OrderId"].ToString();//Идентификатор заказа в системе продавца
            string errorCode = json["ErrorCode"].ToString();//	Код ошибки. Если ошибки не произошло, передается значение «0»
            var amount = decimal.Parse(json["Amount"].ToString());//Сумма в копейках

            using (SHA256 sha256Hash = SHA256.Create())
            {
                string hash = GetHash(sha256Hash, concat);//sha256_hash
                if (hash != token)
                {
                    _logger.Error(orderId + ": " + "invalid token");
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "invalid token");
                }
            }

            //токен валидный, обрабатываем платеж
            var unitPayments = _documentsUOW.UnitPaymentsRepository.GetAll().Where(x => x.unitpayId == orderId).ToList();
            var unitPayment = unitPayments.FirstOrDefault();
            var documentSale = _documentsUOW.DocumentSalesRepository.GetAll().FirstOrDefault(x => x.Id == unitPayment.id);
            var paymentFromDocumentSale = documentSale.unitpay_payments;
            if (documentSale == null)
            {
                _logger.Error(paymentFromDocumentSale.unitpayId + ": " + "We did not find payment information in StudLearn system");
                return Request.CreateResponse(HttpStatusCode.Accepted, "Мы не нашли информацию о платеже в системе StudLearn");
            }
            if (paymentFromDocumentSale.status == (byte)PaymentStatus.Paied)
            {
                _logger.Error(paymentFromDocumentSale.unitpayId + ": " + "The sale has already been made");
                return Request.CreateResponse(HttpStatusCode.Accepted, "Продажа уже произведена");
            }
            if (paymentFromDocumentSale.status == (byte)PaymentStatus.Error)
            {
                _logger.Error(paymentFromDocumentSale.unitpayId + ": " + "The sale was completed with an error");
                return Request.CreateResponse(HttpStatusCode.Accepted, "Продажа была завершена с ошибкой");
            }

            //если оплатили фейковую сумму (подделали клиентский скрипт), то есть не ту, которую реально стоит документ, то возвращаем 
            if (documentSale.unitpay_payments.sum != amount / 100M)
            {
                _logger.Error(paymentFromDocumentSale.unitpayId + ": " + "Amount does not match");
                return Request.CreateResponse(HttpStatusCode.Accepted, "Сумма не совпадает");
            }

            try
            {
                if (success == "True")
                {
                    switch (status)
                    {
                        case "AUTHORIZED":

                            break;
                        case "CONFIRMED":
                            // обновить балансы всех участников сделки
                            var author = documentSale.AspNetUser;
                            //var feePercentages = author.IndividualFee == 0 ? SW.Shared.Constants.Application.FeePercentages : author.IndividualFee;
                            var feePercentages = author.Documents.Count(x => x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.Approved) > 5 ? 35 : 50;
                            documentSale.AspNetUser.Balance += documentSale.Income - ((documentSale.Income / 100.0M) * feePercentages);
                            documentSale.SellerBalance = documentSale.AspNetUser.Balance;
                            if (documentSale.AspNetUser.AspNetUser1 != null)
                                documentSale.AspNetUser.AspNetUser1.Balance += documentSale.ReferrerIncome;

                            // разослать письма и нотификейшены
                            var files = new List<DTO.Storage.FileAndToken>();
                            var tokensIssuer = Unity.Resolve<ITokensIssuer>();
                            foreach (var file in documentSale.Document.UserFiles)
                            {
                                var tokenissue = tokensIssuer.Issue(file.UserFileId, documentSale.unitpay_payments.email);
                                files.Add(new DTO.Storage.FileAndToken
                                {
                                    Id = file.UserFileId,
                                    Title = file.Title,
                                    Token = tokenissue
                                });
                            }
                            var emailsComponent = Unity.Resolve<IEmailsQueueComponent>();
                            // purchase email
                            var purchaseEmailModel = new SW.Shared.Models.Emails.PurchaseEmail()
                            {
                                DocumentId = documentSale.Document.Id,
                                DocumentTitle = documentSale.Document.Title,
                                Files = files
                            };

                            var context = new System.Web.HttpContextWrapper(System.Web.HttpContext.Current);
                            var routeData = new System.Web.Routing.RouteData();
                            routeData.Values.Add("controller", "EmailsController");
                            var controllerContext = new System.Web.Mvc.ControllerContext(context, routeData, new EmailsController());
                            String purchaseEmailBody = SW.Frontend.Utilities.FrontendUtilities.Instance.RenderViewToString(
                                controllerContext,
                                "~/Views/Emails/PurchaseEmailView.cshtml",
                                model: purchaseEmailModel, partial: false
                                );
                            String purchaseEmailTitle = SW.Resources.Emails.PurchaseSuccessTitle;
                            emailsComponent.Push(documentSale.unitpay_payments.email, purchaseEmailTitle, purchaseEmailBody);

                            // завершить оплату
                            //string tinkofPercentSetting = ConfigurationManager.AppSettings["TinkofPercent"] ?? "0";
                            //decimal tinkofPercent = 0;
                            //if (!decimal.TryParse(tinkofPercentSetting.Replace(".", ","), out tinkofPercent))
                            //{
                            //    decimal.TryParse(tinkofPercentSetting.Replace(",", "."), out tinkofPercent);
                            //}
                            documentSale.IsCompleted = true;
                            documentSale.CompletedAt = DateTime.UtcNow;
                            documentSale.unitpay_payments.status = (byte)PaymentStatus.Paied;
                            documentSale.unitpay_payments.dateComplete = DateTime.UtcNow;
                            //var tinkoffPart = documentSale.unitpay_payments.sum * tinkofPercent * 0.01M;
                            //if (tinkoffPart <= 3.49M)
                            //{
                            //    tinkoffPart = 3.49M;
                            //}

                            //documentSale.unitpay_payments.profit = documentSale.unitpay_payments.sum - tinkoffPart;

                            if (documentSale.AspNetUser.AspNetUser1 != null)
                                documentSale.ReferrerBalance = documentSale.AspNetUser.AspNetUser1.Balance;
                            _documentsUOW.Commit();

                            // sale email
                            var seller = documentSale.AspNetUser;
                            var saleEmailModel = MapperManager.Map<Core.DataLayer.DocumentSale, DTO.Documents.DocumentSalePreview>(documentSale);

                            //skip email sending if user unsubscribe "Authors" category
                            var subsription = _accountUOW.SubscriptionsRepository.GetAll()
                                .FirstOrDefault(x => x.Email == seller.Email);
                            if (!String.IsNullOrEmpty(seller.Email) && subsription != null && subsription.DeliveryGroups.FirstOrDefault(x => x.Code == "Authors") != null)
                            {
                                var saleEmailBody = SW.Frontend.Utilities.FrontendUtilities.Instance.RenderViewToString(
                                    controllerContext,
                                    "~/Views/Emails/NewSaleEmailView.cshtml",
                                    model: saleEmailModel, partial: false
                                );
                                var saleEmailTitle = SW.Resources.Emails.NewSaleTitle;
                                emailsComponent.Push(seller.Email, saleEmailTitle, saleEmailBody);
                            }

                            //notification
                            Decimal profit = saleEmailModel.Income - saleEmailModel.Fee;
                            var notificationComponent = Unity.Resolve<INotificationComponent>();
                            notificationComponent.Push(new DTO.Notifications.NotificationDetails
                            {
                                Message = String.Format(SW.Resources.Dashboard.Notifications.NewSale,
                                    profit.ToString("N"),
                                    saleEmailModel.DocumentTitle, Application.DefaultCurrencyIconClass, saleEmailModel.DocumentId
                                ),
                                TypeId = SW.Shared.Constants.Notifications.NotificationsType.Sale,
                                UserId = seller.Id
                            });

                            try
                            {
                                var subscriber = Unity.Resolve<ISubscriber>();
                                subscriber.SubscribeToGroup(documentSale.unitpay_payments.email, DeliveryGroupCode.Buyers);
                            }
                            catch
                            {
                            }
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    // обновить информацию о платеже с сообщением об ошибке
                    documentSale.unitpay_payments.error = $"{status} {errorCode} - {TinkoffErrorsDictionary[Convert.ToInt32(errorCode)]}";
                    documentSale.unitpay_payments.status = (byte)PaymentStatus.Error;
                    _documentsUOW.Commit();
                }

                _logger.Info(documentSale.unitpay_payments.unitpayId + ": " + "Request processed successfully");
                return Request.CreateResponse(HttpStatusCode.OK, "OK");
            }
            catch (Exception e)
            {
                _logger.Error(documentSale.unitpay_payments.unitpayId + ": " + e.Message + "; \r\n " + e.StackTrace);
                return Request.CreateResponse(HttpStatusCode.Accepted, "error");
            }
        }
        #endregion

        #region helpers
        public static String sha256_hash(String value)
        {
            using (SHA256 hash = SHA256Managed.Create())
            {
                return String.Join("", hash
                  .ComputeHash(Encoding.UTF8.GetBytes(value))
                  .Select(item => item.ToString("x2")));
            }
        }

        public Dictionary<int, string> TinkoffErrorsDictionary = new Dictionary<int, string>() {
            {  7 ,  "Покупатель не найден" },
            {53  ,"Обратитесь к продавцу"},
{99  ,"Платеж отклонен"},
{100 ,"Повторите попытку позже"},
{101 ,"Не пройдена идентификация 3DS"},
{102 ,"Операция отклонена, пожалуйста обратитесь в интернет-магазин или воспользуйтесь другой картой"},
{103 ,"Повторите попытку позже"},
{119 ,"Превышено кол-во запросов на авторизацию"},
{191 ,"Некорректный статус договора, обратитесь к вашему менеджеру"},
{1001    ,"Свяжитесь с банком, выпустившим карту, чтобы провести платеж"},
{1003    ,"Неверный merchant ID"},
{1004    ,"Карта украдена. Свяжитесь с банком, выпустившим карту"},
{1005    ,"Платеж отклонен банком, выпустившим карту"},
{1006    ,"Свяжитесь с банком, выпустившим карту, чтобы провести платеж"},
{1007    ,"Карта украдена. Свяжитесь с банком, выпустившим карту"},
{1008    ,"Платеж отклонен, необходима идентификация"},
{1012    ,"Такие операции запрещены для этой карты"},
{1013    ,"Повторите попытку позже"},
{1014    ,"Карта недействительна. Свяжитесь с банком, выпустившим карту"},
{1015    ,"Попробуйте снова или свяжитесь с банком, выпустившим карту"},
{1019    ,"Платеж отклонен — попробуйте снова"},
{1030    ,"Повторите попытку позже"},
{1033    ,"Истек срок действия карты. Свяжитесь с банком, выпустившим карту"},
{1034    ,"Попробуйте повторить попытку позже"},
{1038    ,"Превышено количество попыток ввода ПИН-кода"},
{1039    ,"Платеж отклонен — счет не найден"},
{1041    ,"Карта утеряна. Свяжитесь с банком, выпустившим карту"},
{1043    ,"Карта украдена. Свяжитесь с банком, выпустившим карту"},
{1051    ,"Недостаточно средств на карте"},
{1053    ,"Платеж отклонен — счет не найден"},
{1054    ,"Истек срок действия карты"},
{1055    ,"Неверный ПИН"},
{1057    ,"Такие операции запрещены для этой карты"},
{1058    ,"Такие операции запрещены для этой карты"},
{1059    ,"Подозрение в мошенничестве. Свяжитесь с банком, выпустившим карту"},
{1061    ,"Превышен дневной лимит платежей по карте"},
{1062    ,"Платежи по карте ограничены"},
{1063    ,"Операции по карте ограничены"},
{1064    ,"Проверьте сумму"},
{1065    ,"Превышен дневной лимит транзакций"},
{1075    ,"Превышено число попыток ввода ПИН-кода"},
{1076    ,"Платеж отклонен — попробуйте снова"},
{1077    ,"Коды не совпадают — попробуйте снова"},
{1080    ,"Неверный срок действия"},
{1082    ,"Неверный CVV"},
{1086    ,"Платеж отклонен — не получилось подтвердить ПИН-код"},
{1088    ,"Ошибка шифрования. Попробуйте снова"},
{1089    ,"Попробуйте повторить попытку позже"},
{1091    ,"Банк, выпустивший карту недоступен для проведения авторизации"},
{1092    ,"Платеж отклонен — попробуйте снова"},
{1093    ,"Подозрение в мошенничестве. Свяжитесь с банком, выпустившим карту"},
{1094    ,"Системная ошибка"},
{1096    ,"Повторите попытку позже"},
{9999    ,"Внутренняя ошибка системы"}
        };
        private string GetHash(HashAlgorithm hashAlgorithm, string input)
        {
            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }

        #endregion

        #region OldPaymentSystem
        //[HttpPost]
        //[AllowAnonymous]
        //[Route("api/internal/payments/{documentId}/init")]
        //public HttpResponseMessage InitNewPayment(Int64 documentId, String email = "")
        //{
        //    if (!String.Equals(ConfigurationManager.AppSettings["PaymentEnabled"], "true", StringComparison.OrdinalIgnoreCase))
        //        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Платежи временно отключены, попробуйте позже");
        //    // input validation
        //    IRule emailRule = Unity.Resolve<IRule>(ValidationRuleType.Email.ToString());
        //    if (emailRule == null)
        //        return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, "Не могу найти правила валидации e-mail, сообщите разработчикам немедленно");
        //    if (String.IsNullOrEmpty(email))
        //        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "e-mail обязательное поле");
        //    if (!emailRule.TryValidate(email))
        //        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "e-mail не валидный");
        //    // init
        //    bool isAuthenticated = (System.Web.HttpContext.Current.User != null) && System.Web.HttpContext.Current.User.Identity.IsAuthenticated;
        //    var userId = isAuthenticated ? User.Identity.GetUserId() : String.Empty;
        //    var buyer = String.IsNullOrEmpty(userId) ? (DB.AspNetUser)null : _accountUOW.UsersRepository.GetByID(userId);
        //    var document = _documentsUOW.DocumentsRepository.GetByID(documentId);
        //    if (!document.Price.HasValue)
        //        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Нельзя выписать счет для бесплатной работы, безумцы <смайл рука-лицо>");
        //    if (document.DocumentStateId != (int)SW.Shared.Constants.Documents.DocumentState.Approved)
        //        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Нельзя выписать счет для работы которая имеет статус отличный от 'Утвержден (Approved)'");
        //    if (string.Equals(document.AspNetUser.Email, email, StringComparison.InvariantCultureIgnoreCase))
        //        return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "Похоже что вы пытаетесь купить собственную работу");
        //    // insert payment information to the DB
        //    Decimal fee = (document.Price.Value / 100.0M) * SW.Shared.Constants.Application.FeePercentages;
        //    Decimal income = document.Price.Value;
        //    Decimal profit = income - fee;
        //    DateTime now = DateTime.UtcNow;
        //    var sale = new DB.DocumentSale
        //    {
        //        SellerId = document.AuthorId,
        //        BuyerId = buyer == null ? null : buyer.Id,
        //        DocumentId = document.DocumentId,
        //        Income = income,
        //        SellerBalance = document.AspNetUser.Balance,
        //        BuyerBalance = buyer == null ? (decimal?)null : buyer.Balance,
        //        OccuredAt = now,
        //        Fee = fee,
        //        BuyerEmail = email,
        //        IsCompleted = false,
        //        unitpay_payments = new DB.unitpay_payments
        //        {
        //            unitpayId = "undefined",
        //            email = email,
        //            sum = -1.0M,
        //            itemsCount = 1,
        //            dateCreate = now,
        //            dateComplete = null,
        //            status = (byte)PaymentStatus.Created,
        //            error = null,
        //            profit = -1.0M
        //        },
        //        ReferrerIncome = document.AspNetUser.AspNetUser1 != null ? (document.Price.Value / 100.0M) * (document.AspNetUser.AspNetUser1.ReferralPercent ?? SW.Shared.Constants.Application.ReferrerPercentages) : 0
        //    };
        //    _documentsUOW.DocumentSalesRepository.Insert(sale);
        //    _documentsUOW.Commit();
        //    // return redirect url
        //    /*
        //     * https://unitpay.ru/pay/19790-40218?sum=10&account=demo&desc=Описание+платежа
        //     */
        //    var uri = new UriBuilder("https://unitpay.ru/pay/19790-40218");
        //    var @params = System.Web.HttpUtility.ParseQueryString(String.Empty);
        //    @params.Add("account", sale.Id.ToString());
        //    @params.Add("sum", document.Price.Value.ToString(System.Globalization.CultureInfo.GetCultureInfo("en")));
        //    @params.Add("desc", document.Title);
        //    @params.Add("currency", "RUB");
        //    /*
        //     * Цифровая подпись запроса. Формируется как md5 хеш результата склеивания параметров: account, currency, desc, sum и SECRET KEY.
        //     */
        //    //var md5 = new Md5();
        //    //String secretKey = ConfigurationManager.AppSettings["UnitPaySecretKey"];
        //    //String sign = md5.Compute(@params["account"] + @params["currency"] + @params["desc"] + @params["sum"] + secretKey);
        //    //@params.Add("sign", sign);

        //    String secretKey = ConfigurationManager.AppSettings["UnitPaySecretKey"];
        //    String sign = sha256_hash(@params["account"] + "{up}" + @params["currency"] + "{up}" + @params["desc"] + "{up}" + @params["sum"] + "{up}" + secretKey);
        //    @params.Add("signature", sign);

        //    var q = @params.AllKeys.Select(x => x + "=" + HttpUtility.UrlEncode(@params[x])).Aggregate((x, y) => x + "&" + y);
        //    uri.Query = q;
        //    String url = uri.ToString();
        //    // Отправить на почту invoce с сылкой
        //    var invoiceEmail = new SW.Shared.Models.Emails.InvoiceEmail()
        //    {
        //        DocumentTitle = document.Title,
        //        InvoiceUrl = url
        //    };

        //    var emailsComponent = Unity.Resolve<IEmailsQueueComponent>();
        //    var context = new System.Web.HttpContextWrapper(System.Web.HttpContext.Current);
        //    var routeData = new System.Web.Routing.RouteData();
        //    routeData.Values.Add("controller", "EmailsController");
        //    var controllerContext = new System.Web.Mvc.ControllerContext(context, routeData, new EmailsController());
        //    String purchaseEmailBody = SW.Frontend.Utilities.FrontendUtilities.Instance.RenderViewToString(
        //                    controllerContext,
        //                    "~/Views/Emails/InvoiceEmailView.cshtml",
        //                    model: invoiceEmail, partial: false
        //                    );
        //    String purchaseEmailTitle = SW.Resources.Emails.InvoiceTitle;
        //    emailsComponent.Push(email, purchaseEmailTitle, purchaseEmailBody);

        //    return Request.CreateResponse(HttpStatusCode.OK, url);
        //}

        #endregion
    }
}
