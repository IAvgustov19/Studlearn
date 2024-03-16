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
using SW.Workflow.Components.Emails;
using SW.Frontend.Controllers;
using Microsoft.Practices.Unity;
using SW.Frontend.Models;

namespace SW.Frontend.API.Internal
{
    public class CommentsController : ApiUnityController
    {
        private readonly IDocumentsUOW _documentsUow;
        private readonly IAccountUOW _accountUow;
        private readonly SW.Workflow.Components.Like.ILiker<Comment> _liker;

        public CommentsController(IDocumentsUOW documentsUow, IAccountUOW accountUow, SW.Workflow.Components.Like.ILiker<Comment> liker)
        {
            _documentsUow = documentsUow;
            _accountUow = accountUow;
            _liker = liker;
        }

        private string ReplaceLink(string message)
        {
            Regex regex = new Regex(@"https?:\/\/(www\.)?[-a-zA-Z0-9@:%._\+~#=]{1,256}\.[a-zA-Z0-9()]{1,6}\b([-a-zA-Z0-9()!@:%_\+.~#?&\/\/=]*)");
            MatchCollection matches = regex.Matches(message);
            if (matches.Count > 0)
            {
                foreach (Match match in matches)
                {
                    message = message.Replace(match.Value, "<a href=\"" + match.Value + "\">" + match.Value + "</a>");
                }
            }
            return message;
        }

        [HttpGet]
        [Route("api/internal/comments/{documentId}/{count}/{skip}")]
        public IEnumerable<CommentPreview> GetComments(Int32 documentId, Int32 count, Int32 skip)
        {
            var userId = User.Identity.GetUserId();

            var sections = _documentsUow.CommentsRepository
                .GetAll()
                .Where(x => x.DocumentId == documentId)
                .OrderByDescending(x => x.PostedAt)
                .Skip(skip)
                .Take(count)
                .ToList()
                .Select(x => new CommentPreview()
                {
                    CommentId = x.CommentId,
                    Message = ReplaceLink(x.Message),
                    PostedAt = x.PostedAt,
                    IsReply = x.IsReply,
                    RepliedTo = x.AspNetUser1 != null ? new Shared.Models.Account.UserPreview
                    {
                        Id = x.AspNetUser1.Id,
                        UserName = x.AspNetUser1.UserName,
                        Email = x.AspNetUser1.Email,
                        FirstName = x.AspNetUser1.FirstName,
                        LastName = x.AspNetUser1.LastName,
                        PreviewAvatar = !String.IsNullOrEmpty(x.AspNetUser1.AvatartUrl) ? x.AspNetUser1.AvatartUrl : SW.Shared.Constants.DefaultImages.NoAvatar,
                        Rating = x.AspNetUser1.Rating,
                        Slug = x.AspNetUser1.Slug
                    }
                    : null,
                    Author = new Shared.Models.Account.UserPreview
                    {
                        Id = x.AspNetUser.Id,
                        UserName = x.AspNetUser.UserName,
                        Email = x.AspNetUser.Email,
                        FirstName = x.AspNetUser.FirstName,
                        LastName = x.AspNetUser.LastName,
                        PreviewAvatar = !String.IsNullOrEmpty(x.AspNetUser.AvatartUrl) ? x.AspNetUser.AvatartUrl : SW.Shared.Constants.DefaultImages.NoAvatar,
                        Rating = x.AspNetUser.Rating,
                        Slug = x.AspNetUser.Slug
                    },
                    Rating = x.Rating,
                    IsUserVoted = x.CommentsRatingHistories.FirstOrDefault(y => y.UserId == userId) != null ? true : false,
                    IsPositive = x.CommentsRatingHistories.FirstOrDefault(y => y.UserId == userId) != null ? x.CommentsRatingHistories.FirstOrDefault(y => y.UserId == userId).Rating : false,
                    UserType = x.UserType,
                    UserTypeColor = x.UserTypeColor,
                    UserTypeDescription = x.UserTypeDescription,
                    Options = new CommentOptions()
                    {
                        EditEnabled = (DateTime.UtcNow - x.PostedAt).TotalMinutes < 15 && x.AuthorId == userId
                    },
                })
                .OrderBy(x => x.PostedAt)
                .ToList();

            return sections;
        }

        [HttpPost]
        [Route("api/internal/comments/{documentId}")]
        public void PostComment([FromBody] CommentPreview comment, [FromUri] int documentId)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                Request.CreateResponse(HttpStatusCode.Unauthorized, "You should be authorize to use comments");
                return;
            }

            var repliedUserEmail = "";
            var isReply = false;
            var userMatch = Regex.Match(comment.Message, @"^(?'name'[^\s]+),\s", RegexOptions.CultureInvariant);
            if (userMatch.Success)
            {
                var userName = userMatch.Groups["name"].Value;
                var repliedUser = _accountUow.UsersRepository
                    .GetAll()
                    .FirstOrDefault(x => x.UserName == userName);
                if (repliedUser != null)
                {
                    comment.RepliedTo.Id = repliedUser.Id;
                    comment.Message = comment.Message.Replace(userMatch.Groups[0].Value, "");
                    repliedUserEmail = repliedUser.Email;
                    isReply = true;
                }
            }

            var userId = User.Identity.GetUserId();

            var commentRepo = _documentsUow.CommentsRepository;
            var dtoComment = MapperManager.Map<CommentPreview, Comment>(comment);
            dtoComment.AuthorId = userId;
            dtoComment.PostedAt = DateTime.UtcNow;
            dtoComment.DocumentId = documentId;

            commentRepo.Insert(dtoComment);
            _documentsUow.Commit();

            // Отправить на почту уведомления
            try
            {
                var document = _documentsUow.DocumentsRepository.GetAll()
                               .FirstOrDefault(x => x.DocumentId == dtoComment.DocumentId);
                var dbComment = dtoComment;// _documentsUow.CommentsRepository.GetAll().FirstOrDefault(x => x.CommentId == dtoComment.CommentId);
                var commentator = _accountUow.UsersRepository.GetByID(dbComment.AuthorId);

                if (document == null || dbComment == null)
                {
                    return;
                }

                var emailModel = new NewCommentModel()
                {
                    WorkName = document.Title,
                    WorkLink = "https://studlearn.com" + (String.IsNullOrEmpty(document.Slug) ? "/works/index/" + document.DocumentId : "/works/details/" + document.Slug) + "#SwCommentsWrapper",
                    Message = dbComment.Message,
                    UserImageLink = (commentator.AvatartUrl != null ? commentator.AvatartUrl : ("https://studlearn.com" + SW.Shared.Constants.DefaultImages.NoAvatar)),
                    UserName = commentator.UserName,
                    UserLink = "https://studlearn.com" + (String.IsNullOrEmpty(commentator.Slug) ? "/authors/details/" + commentator.Id : "/authors/profile/" + commentator.Slug),
                };
                var emailsComponent = Unity.Resolve<IEmailsQueueComponent>();
                var context = new System.Web.HttpContextWrapper(System.Web.HttpContext.Current);
                var routeData = new System.Web.Routing.RouteData();
                routeData.Values.Add("controller", "EmailsController");
                var controllerContext = new System.Web.Mvc.ControllerContext(context, routeData, new EmailsController());
                var purchaseEmailBody = SW.Frontend.Utilities.FrontendUtilities.Instance.RenderViewToString(
                                controllerContext,
                               isReply ? "~/Views/Emails/newanswerindocumentview.cshtml" : "~/Views/Emails/NewCommentInDocumentView.cshtml",
                                model: emailModel, partial: false
                                );
                var purchaseEmailTitle = isReply ? "Ответ на Ваш комментарий" : "Комментарий к Вашей работе";
                var targetEmail = isReply ? repliedUserEmail : document.AspNetUser.Email;


                //если комментатор НЕ отвечает сам на свой же коммент
                if (commentator.Email != targetEmail)
                {
                    _logger.Info($"targetEmail={targetEmail}  {purchaseEmailTitle} {dbComment.Message}");

                    //check delivery

                    emailsComponent.Push(targetEmail, purchaseEmailTitle, purchaseEmailBody);
                }

                //отправим письмо автору работы, если два пользователя переписываются на карточке его работы
                if (isReply && document.AspNetUser.Email != repliedUserEmail/*ответ не на коммент автора*/ && document.AspNetUser.Email != commentator.Email/*комментирующий НЕ автор*/)
                {
                    purchaseEmailBody = SW.Frontend.Utilities.FrontendUtilities.Instance.RenderViewToString(
                              controllerContext,
                              "~/Views/Emails/NewCommentInDocumentView.cshtml",
                              model: emailModel, partial: false
                              );
                    emailsComponent.Push(document.AspNetUser.Email, "Ответ на комментарий к Вашей работе", purchaseEmailBody);
                }
            }
            catch (Exception exc)
            {
                _logger.Info(exc.Message + exc.StackTrace);
            }
        }


        [HttpPost]
        [Route("api/internal/comment/{commentId}/edit")]
        public HttpResponseMessage EditComment([FromBody] CommentPreview comment, [FromUri] int commentId)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                Request.CreateResponse(HttpStatusCode.Unauthorized, "You should be authorize to use comments");
            }
            var userId = User.Identity.GetUserId();

            var userMatch = Regex.Match(comment.Message, @"^(?'name'[^\s]+),\s", RegexOptions.CultureInvariant);
            if (userMatch.Success)
            {
                var userName = userMatch.Groups["name"].Value;
                var repliedUser = _accountUow.UsersRepository
                    .GetAll()
                    .FirstOrDefault(x => x.UserName == userName);
                if (repliedUser != null)
                {
                    comment.RepliedTo.Id = repliedUser.Id;
                    comment.Message = comment.Message.Replace(userMatch.Groups[0].Value, "");
                }
            }

            var commentRepo = _documentsUow.CommentsRepository;
            var dtoComment = MapperManager.Map<CommentPreview, Comment>(comment);

            var existComment = commentRepo.GetAll().FirstOrDefault(x => x.CommentId == comment.CommentId);
            if (existComment == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Недопустимое действие");
            if ((DateTime.UtcNow - existComment.PostedAt).TotalMinutes > 15)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Время редактирования истекло");

            existComment.Message = dtoComment.Message;
            _documentsUow.Commit();
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        [Route("api/internal/comment/{commentId}/delete")]
        public HttpResponseMessage DeleteComment([FromUri] int commentId)
        {
            if (User.Identity.IsAuthenticated == false)
            {
                return Request.CreateResponse(HttpStatusCode.Unauthorized, "You should be authorize to use comments");
            }
            var userId = User.Identity.GetUserId();
            if (!((System.Security.Claims.ClaimsIdentity)User.Identity).HasClaim(SW.Core.DataLayer.Account.ResourcesType.Type, "Moderator"))
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Недостаточно прав");
            }
            var commentRepo = _documentsUow.CommentsRepository;
            var existComment = commentRepo.GetAll().FirstOrDefault(x => x.CommentId == commentId);
            if (existComment == null)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Недопустимое действие");

            commentRepo.Delete(existComment);
            _documentsUow.Commit();
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        [HttpGet]
        [Route("api/internal/comments/vote/{commentId}/{isVoted}/{isPositive}")]
        public HttpResponseMessage Negative([FromUri] int commentId, [FromUri] bool isVoted, [FromUri] bool isPositive)
        {
            var userId = User.Identity.GetUserId();
            var voter = _accountUow.UsersRepository.GetByID(userId);
            var comment = _documentsUow.CommentsRepository.GetByID(x => x.CommentId == commentId);

            if (comment.AuthorId == userId)
                return Request.CreateResponse(HttpStatusCode.BadRequest, "Недопустимое действие");

            try
            {
                _liker.Like(comment, voter, isVoted, isPositive);
            }
            catch (InvalidOperationException ex)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, ex.Message);
            }
            _documentsUow.Commit();

            var author = _accountUow.UsersRepository.GetByID(x => x.Id == comment.AuthorId);
            if (!author.IsRatingUpdated)
            {
                author.IsRatingUpdated = true;
                _accountUow.Commit();
            }
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}