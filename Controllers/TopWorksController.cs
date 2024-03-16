using SW.Core.DataLayer;
using SW.Core.DataLayer.Documents;
using SW.Frontend.Models;
using SW.Shared.Constants;
using SW.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SW.Frontend.Controllers
{
    public class TopWorksController : Controller
    {
        private readonly IDocumentsUOW _documentsUOW;

        public const int MaxTopWorkItemsPerPage = 50;

        public TopWorksController(IDocumentsUOW documentsUOW)
        {
            _documentsUOW = documentsUOW;
        }

        public ActionResult Index()
        {
            return RedirectToAction("Free");
        }


        [OutputCache(CacheProfile = "TopLevelOneDay")]
        public ActionResult Free(int? y = null, int? m = null)
        {
            int year = y.HasValue ? y.Value : DateTime.UtcNow.Year;
            var works = GetTopWorks(year, m, true);
            ViewBag.Year = year;
            ViewBag.Month = m;
            ViewBag.MonthText = m != null ? new DateTime(year, m.Value, 1).ToString("MMMM", System.Globalization.CultureInfo.CurrentCulture) : "";
            return View(works);
        }


        [OutputCache(CacheProfile = "TopLevelOneDay")]
        public ActionResult Paid(int? y, int? m = null)
        {
            int year = y.HasValue ? y.Value : DateTime.UtcNow.Year;
            var works = GetTopWorks(year, m, false);
            ViewBag.Year = year;
            ViewBag.Month = m;
            ViewBag.MonthText = m != null ? new DateTime(year, m.Value, 1).ToString("MMMM", System.Globalization.CultureInfo.CurrentCulture) : "";
            return View(works);
        }

        [OutputCache(CacheProfile = "TopLevelOneDay")]
        public ActionResult Recent()
        {
            IEnumerable<TopWork> works = _documentsUOW.DocumentsRepository
                .GetAll()
                .Where(x=>x.IsDeleted==false && x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden)
                .OrderByDescending(x => x.CreatedAt)
                .Take(MaxTopWorkItemsPerPage)
                .Select(x => new TopWork
                {
                    DocumentId = x.DocumentId,
                    BriefDescription = x.BriefDescription,
                    CategoryImage = x.Category.FullImageLink,
                    CategoryTitle = x.Category.Title,
                    TypeTitle = x.DocumentType.Title,
                    ImageId = x.UserFiles1.Any() ? x.UserFiles1.FirstOrDefault().UserFileId : (long?)null,
                    Price = x.Price,
                    Rating = x.Rating,
                    Slug = x.Slug,
                    Title = x.Title,
                    YoutubeId = x.YoutubeId,
                    Visits = x.DocumentVisits.Count,
                    Author = new Shared.Models.Account.UserPreview
                    {
                        Id = x.AspNetUser.Id,
                        UserName = x.AspNetUser.UserName,
                        Slug = x.AspNetUser.Slug
                    }
                })
                .ToList();

            foreach (var work in works)
            {
                work.Color = GetColor(work.TypeTitle);
            }

            return View(works);
        }

        [ChildActionOnly]
        public ActionResult Menu(int year, int? month, bool onlyFree = true)
        {
            ViewBag.OnlyFree = onlyFree;
            var menuItems = GetMenu(year, month);
            return View(menuItems);
        }

        public string GetColor(string typeTitle)
        {
            switch (typeTitle)
            {
                case "Диплом":
                    return "orange";
                case "Другое":
                    return "grey";
                case "Исходник":
                    return "black";
                case "Контрольная":
                    return "purple";
                case "Курсовая":
                    return "green";
                case "Лабораторная":
                    return "blue";
                case "Презентация":
                    return "pink";
                case "Реферат":
                    return "yellow";
                case "Статья":
                    return "red";
                case "Шпаргалка":
                    return "aero";
                default:
                    return "black";
            }
        }

        public IEnumerable<TopWork> GetTopWorks(int? year, int? month, bool onlyFree = true)
        {
            #region Build query
            var visitsQuery = _documentsUOW.DocumentsVisitsRepository
                .GetAll()
                .Where(x => x.Document.IsDeleted == false) ;

            if (year.HasValue)
                visitsQuery = visitsQuery.Where(x => x.OccuredAt.Year == year);
            if (month.HasValue)
                visitsQuery = visitsQuery.Where(x => x.OccuredAt.Month == month);

            if (onlyFree)
                visitsQuery = visitsQuery.Where(x => x.Document.Price.HasValue == false || x.Document.Price.Value == 0);
            else
                visitsQuery = visitsQuery.Where(x => x.Document.Price.HasValue == true && x.Document.Price.Value > 0);
            #endregion

            var topWorkStats = visitsQuery
                 .GroupBy(x => x.DocumentId)
                 .OrderByDescending(x => x.Count())
                 .Select(x => new
                 {
                     DocumentId = x.Key,
                     Visits = x.Count()
                 })
                 .AsEnumerable()
                 .ToDictionary(x => x.DocumentId, x => x.Visits);

            long[] topWorksIds = topWorkStats.Keys.ToArray<long>();

            var works = _documentsUOW.DocumentsRepository
                .GetAll()
                .Where(x => topWorksIds.Contains(x.DocumentId) && x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden)
                .Select(x => new TopWork
                {
                    DocumentId = x.DocumentId,
                    BriefDescription = x.BriefDescription,
                    CategoryImage = x.Category.FullImageLink,
                    CategoryTitle = x.Category.Title,
                    TypeTitle = x.DocumentType.Title,
                    ImageId = x.UserFiles1.Any() ? x.UserFiles1.FirstOrDefault().UserFileId : (long?)null,
                    Price = x.Price,
                    Rating = x.Rating,
                    Slug = x.Slug,
                    Title = x.Title,
                    YoutubeId = x.YoutubeId,
                    CreatedAt = x.CreatedAt,
                    Author = new Shared.Models.Account.UserPreview
                    {
                        Id = x.AspNetUser.Id,
                        UserName = x.AspNetUser.UserName,
                        Slug = x.AspNetUser.Slug
                    }
                }).ToList();

            foreach (var work in works)
            {
                work.Visits = topWorkStats[work.DocumentId];
                work.Color = GetColor(work.TypeTitle);
            }
            return works.OrderByDescending(x => x.Visits).ToList();
        }

        public IEnumerable<TopWorkMenuItem> GetMenu(int year, int? month)
        {
            var menuItems = new List<TopWorkMenuItem>();
            var now = DateTime.UtcNow;
            var startDate = new DateTime(2015, 12, 1);
            for (int i = DateTime.UtcNow.Year; i >= startDate.Year; i--)
            {
                var yearMenuItem = new TopWorkMenuItem()
                {
                    Title = $"{i}",
                    Year = i,
                    Active = i == year
                };

                foreach (Month m in Enum.GetValues(typeof(Month)).Cast<Month>().OrderByDescending(x => x))
                {
                    DateTime checkDate = new DateTime(i, (int)m, 1);
                    if (i == startDate.Year)
                        if (checkDate < startDate)
                            continue;

                    if (now > checkDate)
                    {
                        var monthMenuItem = new TopWorkMenuItem
                        {
                            Title = checkDate.ToString("MMMM", System.Globalization.CultureInfo.CurrentCulture),
                            Year = i,
                            Season = Season.Summer,
                            Month = (int)m,
                            Active = i == year && month != null && (int)m == month
                        };
                        yearMenuItem.Children.Add(monthMenuItem);
                    }
                }

                menuItems.Add(yearMenuItem);
            }
            return menuItems;
        }
    }


}