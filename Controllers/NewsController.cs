using SW.Core.DataLayer;
using SW.Core.DataLayer.NewsUnit;
using SW.Frontend.Models;
using SW.Shared.Models.News;
using SW.Workflow.Coordinator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SW.Frontend.Controllers
{
    public class NewsController : Controller
    {
        INewsUOW _newsUOW;
        public NewsController(INewsUOW newsUOW)
        {
            _newsUOW = newsUOW;
        }

        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        public ActionResult Index(int page = 1, int rows = 10)
        {
            var newsQuery = _newsUOW.NewsRepository.GetAll();

            var news = newsQuery.OrderByDescending(x => x.CreateAt)
                .Skip((page - 1) * rows)
                .Take(rows)
                .ToList()
                .Select(x => MapperManager.Map<News, NewsDetail>(x));

            var viewModel = new NewsListModel()
            {
                News = news,
                PagerModel = new PagerModel
                {
                    Rows = rows,
                    Total = newsQuery.Count(),
                    CurrentPage = page
                }
            };
            viewModel.PagerModel.LastPage = Convert.ToInt32(Math.Ceiling(viewModel.PagerModel.Total / (decimal)viewModel.PagerModel.Rows));
            viewModel.PagerModel.Count = viewModel.News.Count();

            return View(viewModel);
        }

        public ActionResult Details(string id)
        {
            var news = _newsUOW.NewsRepository.GetAll()
                .FirstOrDefault(x => x.Slug == id);

            if (news == null)
                return RedirectToAction("Error", "News");

            return View(MapperManager.Map<News, NewsDetail>(news));
        }

        public ActionResult Error()
        {
            return View();
        }
    }
}