using SW.Core.DataLayer;
using SW.Core.DataLayer.Account;
using SW.Core.DataLayer.Documents;
using SW.Core.DataLayer.NewsUnit;
using SW.Core.DataLayer.Sliders;
using SW.Frontend.Models;
using SW.Shared.Models.News;
using SW.Workflow.Coordinator;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SW.Frontend.Controllers
{
    public class ErrorController : Controller
    {
        private IDocumentsUOW _documentsUOW;
        private IAccountUOW _accountUOW;
        private ISliderUOW _sliderUow;

        public ErrorController(IDocumentsUOW documentsUOW, IAccountUOW accountUOW, ISliderUOW sliderUow)
        {
            _documentsUOW = documentsUOW;
            _accountUOW = accountUOW;
            _sliderUow = sliderUow;
        }

        public ActionResult Index()
        {
            return View();
        }

        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        public async Task<ActionResult> PageNotFound()
        {
            Response.StatusCode = (int)HttpStatusCode.NotFound;
            var model = new HomePageModel();
            model.FeaturedFreeWorks = await _documentsUOW.DocumentsRepository
                ._context.Database
                .SqlQuery<Core.DataLayer.FeaturedWork>("GetFeaturedFreeWorks")
                .ToListAsync();
            int page = 0;
            model.FeaturedAuthors = await _accountUOW.UsersRepository
                ._context.Database
                .SqlQuery<Core.DataLayer.FeaturedAuthor>("GetFeaturedAuthors @p0, @p1",
                    new SqlParameter("p0", SW.Shared.Constants.Application.MaxFeaturedAuthors),
                    new SqlParameter("p1", page)
                    )
                .ToListAsync();
            //model.Slides = _sliderUow.SliderRepository.GetAll();

            return View(model);
        }
    }
}