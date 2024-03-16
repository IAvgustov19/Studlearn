using SW.Core.DataLayer.Account;
using SW.Core.DataLayer.Documents;
using SW.Frontend.Models;
using SW.Workflow.Coordinator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SW.Core.DataLayer.Sliders;
using DB = SW.Core.DataLayer;
using DTO = SW.Shared.Models;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace SW.Frontend.Controllers
{
    public class HomeController : UnityController
    {
        private IDocumentsUOW _documentsUOW;
        private IAccountUOW _accountUOW;
        private ISliderUOW _sliderUow;

        public HomeController(IDocumentsUOW documentsUOW, IAccountUOW accountUOW, ISliderUOW sliderUow)
        {
            _documentsUOW = documentsUOW;
            _accountUOW = accountUOW;
            _sliderUow = sliderUow;
        }

        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        public async Task<ActionResult> Index()
        {
            var model = new HomePageModel();
            model.FeaturedFreeWorks = await _documentsUOW.DocumentsRepository
                ._context.Database
                .SqlQuery<Core.DataLayer.FeaturedWork>("GetFeaturedFreeWorks")
                .ToListAsync();
            model.FeaturedPayedWorks = await _documentsUOW.DocumentsRepository
                ._context.Database
                .SqlQuery<Core.DataLayer.FeaturedWork>("GetFeaturedPaidWorks")
                .ToListAsync();
            model.RecentWorks = await _documentsUOW.DocumentsRepository
                ._context.Database
                .SqlQuery<Core.DataLayer.FeaturedWork>("GetFeaturedNewWorks")
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

            ViewBag.AuthorsCount =  _accountUOW.UsersRepository.GetAll().Count();
            ViewBag.PublishedWorksCount = _documentsUOW.DocumentsRepository.GetAll().Count(x => x.IsDeleted == false && x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden);

            model.Themes =  _documentsUOW.ThemesRepository.GetAll().OrderBy(x=>x.Name).ToList();

            return View(model);
        }

        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        public ActionResult About()
        {
            return View();
        }

        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        public ActionResult FAQ()
        {
            return View();
        }

        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        public ActionResult Income()
        {
            return View();
        }

        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        public ActionResult Terms()
        {
            return View();
        }

        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        public ActionResult Contacts()
        {
            return View();
        }

        //[CompressContent]
        //[OutputCache(CacheProfile = "TopLevelOneHour")]
        //public ActionResult Achievements()
        //{
        //    return View();
        //}

        [ChildActionOnly]
        public ActionResult AchivmentsPartial()
        {
            var achivments = _accountUOW.UserAchivmnetsRepository.GetAll().ToList();
            var result = achivments.Select(x => MapperManager.Map<DB.AspNetUserAchivment, SW.Shared.Models.Account.AchivmentsInfo>(x)).ToList();
            return PartialView(result);
        }
    }
}