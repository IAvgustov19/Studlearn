using SW.Frontend.Models;
using SW.Shared.Models.Documents;
using SW.Shared.Models.Emails;
using SW.Shared.Models.Storage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SW.Frontend.Controllers
{
    public class EmailsController : Controller
    {
        // GET: EmailsDemo
        public ActionResult NewSaleEmailView()
        {
            var doc = new DocumentPublic
            {
                DocumentId = 1,
                Title = "Lorem ipsum",
                Content = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Fusce ut ante accumsan, elementum eros nec, cursus ex. Quisque vitae laoreet lectus. Nullam vehicula quis dui et eleifend. Donec tempus egestas sem eleifend venenatis. Aenean non massa venenatis, laoreet est eget, cursus lorem. Maecenas non iaculis nunc. Vestibulum erat nunc, maximus eu lacus eget, feugiat tempor neque. Proin eget volutpat leo. Integer tristique vel ante eu sollicitudin. Ut accumsan nibh non volutpat rhoncus. Etiam vitae lorem et purus sagittis viverra eget eget mi. Donec commodo libero in mollis mollis. Proin dictum, nunc eget ultricies efficitur, nibh ipsum pharetra leo, ac viverra justo tellus et dolor. Suspendisse eu venenatis mauris, vel malesuada metus. Fusce lobortis tristique enim, vitae pellentesque nibh elementum at. Integer eu lacus non diam mollis tristique posuere a ante. Vivamus porttitor, purus et egestas congue, quam est efficitur lorem, et tincidunt felis metus sit amet eros. Quisque molestie venenatis tellus, eget gravida eros volutpat id. Sed accumsan risus ut augue volutpat, nec lacinia leo porta. Donec sollicitudin neque id blandit aliquet. Pellentesque mattis finibus risus, sit amet cursus augue hendrerit fringilla. Pellentesque et nisl feugiat, feugiat risus sed, convallis tellus. Nulla velit neque, aliquam a lobortis non, gravida non.",
                Price = 4.99M,
                Rating = 324
            };
            return View(doc);
        }

        public ActionResult PurchaseEmailView()
        {
            var model = new PurchaseEmail
            {
                DocumentId = 1,
                DocumentTitle = "Lorem ipsum",
                Files = new List<FileAndToken>
                {
                    new FileAndToken {Id = 1, Token = Guid.NewGuid(), Title = "Private file 1"},
                    new FileAndToken {Id = 2, Token = Guid.NewGuid(), Title = "Private file 2"},
                    new FileAndToken {Id = 3, Token = Guid.NewGuid(), Title = "Private file 3"},
                    new FileAndToken {Id = 4, Token = Guid.NewGuid(), Title = "Private file 4"}
                }
            };

            return View(model);
        }


        public ActionResult InvoiceEmailView()
        {
            var model = new SW.Shared.Models.Emails.InvoiceEmail()
            {
                DocumentTitle = "Название документа",
                InvoiceUrl = ConfigurationManager.AppSettings["frontendUrl"].ToString(),
                DocumentPrice=1000
            };
            return View(model);
        }

        public ActionResult NewCommentInDocumentView()
        {
            var model = new NewCommentModel()
            {
                WorkName = "Название работы",
                WorkLink = "https://studlearn.com" ,
                Message = "Текст комментария",
                UserImageLink = SW.Shared.Constants.DefaultImages.NoAvatar,
                UserName = "Имя Комментатора",
                UserLink = "https://studlearn.com" 
            };
            return View(model);
        }

        public ActionResult NewAnswerInDocumentView()
        {
            var model = new NewCommentModel()
            {
                WorkName = "Название работы",
                WorkLink = "https://studlearn.com",
                Message = "Текст комментария",
                UserImageLink = SW.Shared.Constants.DefaultImages.NoAvatar,
                UserName = "Имя Комментатора",
                UserLink = "https://studlearn.com"
            };
            return View(model);
        }

        public ActionResult NewSiteReviewEmailView()
        {
            var model = new SiteReviewModel()
            {
                Comment= "Все отлично. Понятно, где искать работу.",
                Emotion = "Отлично!",
                Points = 5
            };
            return View(model);
        }
    }
}