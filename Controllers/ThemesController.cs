using MvcSiteMapProvider;
using SW.Core.DataLayer.Account;
using SW.Core.DataLayer.Documents;
using SW.Frontend.Models;
using SW.Shared.Models.Filter;
using SW.Workflow.Coordinator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using DB = SW.Core.DataLayer;
using DTO = SW.Shared.Models;
using System.Threading.Tasks;
using System.Data.Entity;
using SW.Core.DataLayer;
using SW.Shared.Models.Documents;

namespace SW.Frontend.Controllers
{
    public class ThemesController : Controller
    {
        private readonly IDocumentsUOW _documentsUow;

        public ThemesController(IDocumentsUOW documentsUow, IAccountUOW accountUOW)
        {
            _documentsUow = documentsUow;
        }

        [CompressContent]
        //[OutputCache(CacheProfile = "TopLevelOneHour")]
        [OutputCache(Duration = 3000)]
        public ActionResult Index()
        {
            var query = _documentsUow.DocumentThemesRepository.GetAll()
                .Join(_documentsUow.DocumentsRepository.GetAll(), dt => dt.DocumentId, d => d.DocumentId, (dt, d) => new { dt = dt, d = d });

            var themes = query
                .Where(x => x.d.IsDeleted == false && x.d.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.d.Category.Hidden)
                .GroupBy(x => x.dt.Theme)
                .Select(x => new ThemePreview() { Id = x.Key.Id, Name = x.Key.Name, DocumentsCount = x.Count() });
            //var themes = _documentsUow.ThemesRepository.GetAll()
            //    .OrderBy(x => x.Name)
            //    .Select(x => new ThemePreview
            //    {

            //    })
            //    .ToList();
            //ViewBag.Total = themes.Sum(x=>x.DocumentsCount);

            return PartialView(themes);
        }

        [CompressContent]
        //[OutputCache(CacheProfile = "TopLevelOneHour")]
        [OutputCache(Duration = 3000, VaryByParam = "showCount;authorId;categoryId;themeId;sectionId;typeId;")]
        public ActionResult _ThemesListPartial(bool showCount = true, string authorId = "", int categoryId = 0, int themeId = 0, int sectionId = 0, int typeId = 0)
        {
            var query = _documentsUow.DocumentThemesRepository.GetAll()
                .Join(_documentsUow.DocumentsRepository.GetAll(), dt => dt.DocumentId, d => d.DocumentId, (dt, d) => new { dt = dt, d = d });

            if (!string.IsNullOrEmpty(authorId))
            {
                query = query.Where(x => x.d.AuthorId == authorId);
            }
            if (sectionId != 0)
            {
                query = query.Where(x => x.d.Category.ParentSectionId == sectionId);
            }
            if (categoryId != 0)
            {
                query = query.Where(x => x.d.CategoryId == categoryId);
            }
            if (themeId != 0)
            {
                query = query.Where(x => x.d.DocumentThemes.Any(y => y.ThemeId == themeId));
            }

            if (typeId != 0)
            {
                query = query.Where(x => x.d.TypeId == typeId);
            }

            var themes = query
                .Where(x => x.d.IsDeleted == false && x.d.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.d.Category.Hidden)
                .GroupBy(x => x.dt.Theme)
                .Select(x => new ThemePreview() { Id = x.Key.Id, Name = x.Key.Name, DocumentsCount = x.Count(), Slug = x.Key.Slug });
            //var themes = _documentsUow.ThemesRepository.GetAll()
            //    .OrderBy(x => x.Name)
            //    .Select(x => new ThemePreview
            //    {

            //    })
            //    .ToList();

            return PartialView(themes);
        }
        [CompressContent]
        //[OutputCache(CacheProfile = "TopLevelOneHour")]
        [OutputCache(Duration = 3000, VaryByParam = "showCount;authorId;categoryId;themeId")]
        public ActionResult _ThemesSiblingsPartial(bool showCount = true, string authorId = "", int categoryId = 0, int themeId = 0)
        {
            var query = _documentsUow.DocumentThemesRepository.GetAll()
                .Join(_documentsUow.DocumentsRepository.GetAll(), dt => dt.DocumentId, d => d.DocumentId, (dt, d) => new { dt = dt, d = d });

            if (!string.IsNullOrEmpty(authorId))
            {
                query = query.Where(x => x.d.AuthorId == authorId);
            }
            if (categoryId != 0)
            {
                query = query.Where(x => x.d.CategoryId == categoryId);
            }
            if (themeId != 0)
            {
                query = query.Where(x => x.d.DocumentThemes.Any(y => y.ThemeId == themeId));
            }

            var themes = query
                .Where(x => x.d.IsDeleted == false && x.d.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.d.Category.Hidden)
                .GroupBy(x => x.dt.Theme)
                .Select(x => new ThemePreview() { Id = x.Key.Id, Name = x.Key.Name, DocumentsCount = x.Count() });
            //var themes = _documentsUow.ThemesRepository.GetAll()
            //    .OrderBy(x => x.Name)
            //    .Select(x => new ThemePreview
            //    {

            //    })
            //    .ToList();

            return PartialView(themes);
        }


        [CompressContent]
        //[HandleError(View = "Error", ExceptionType = typeof(InvalidOperationException))]
        public ActionResult Details(int id, bool free = false, string minmax = null, int[] type = null, int category = 0, int theme = 0, TimePeriodEnum period = TimePeriodEnum.Any, int page = 1, int rows = 50, SortFieldsEnum sort = SortFieldsEnum.Rating, bool direct = false)
        {
            int min = 0;
            int max = 10000;
            if (!string.IsNullOrEmpty(minmax) && minmax.Split(new char[] { ';' }).Length == 2)
            {
                if (!int.TryParse(minmax.Split(new char[] { ';' })[0], out min))
                    throw new InvalidOperationException("Bad request");
                if (!int.TryParse(minmax.Split(new char[] { ';' })[1], out max))
                    throw new InvalidOperationException("Bad request");
            }

            var worksQuery = _documentsUow.DocumentsRepository.GetAll().Where(x => x.DocumentThemes.Any(y => y.ThemeId == id))
                .Where(x => !x.IsDeleted && x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden);
            var mainTheme = _documentsUow.ThemesRepository.GetByID(id);
            if (mainTheme == null)
                throw new InvalidOperationException("Данной темы больше не существует на нашем сайте :(");

            var types = worksQuery.Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.DocumentType.DocumentTypeId, Title = x.DocumentType.Title }).Distinct();
            //var categories = _documentsUow.CategoriesRepository.GetAll().OrderBy(x => x.Title).Select(x => new CategoryPreview() { CategoryId = x.CategoryId, Title = x.Title });
            var categories = worksQuery.Select(x => new CategoryPreview() { CategoryId = x.CategoryId, Title = x.Category.Title }).Distinct().OrderBy(x => x.Title);

            //Filter
            if (category != 0)
            {
                worksQuery = worksQuery.Where(x => x.CategoryId == category);
            }
            if (type != null)
            {
                worksQuery = worksQuery.Where(x => type.Contains(x.TypeId));
            }
            if (theme != 0 && theme != mainTheme.Id)
            {
                worksQuery = worksQuery.Where(x => x.DocumentThemes.Any(y => y.ThemeId == theme/*additionalTheme*/));
            }
            if (free)
            {
                worksQuery = worksQuery.Where(x => x.Price == null || x.Price == 0);
            }
            else
            {
                worksQuery = worksQuery.Where(x => (min == 0 ? x.Price == null || (x.Price >= min && x.Price <= max) : (x.Price != null && x.Price >= min && x.Price <= max)));
            }

            switch (period)
            {
                case (TimePeriodEnum.LastDay):
                    var dateTime = DateTime.UtcNow.AddDays(-1);
                    worksQuery = worksQuery.Where(x => x.CreatedAt >= dateTime);
                    break;
                case (TimePeriodEnum.LastWeek):
                    var dateTime1 = DateTime.UtcNow.AddDays(-7);
                    worksQuery = worksQuery.Where(x => x.CreatedAt >= dateTime1);
                    break;
                case (TimePeriodEnum.LastMonth):
                    var dateTime2 = DateTime.UtcNow.AddMonths(-1);
                    worksQuery = worksQuery.Where(x => x.CreatedAt >= dateTime2);
                    break;
                case (TimePeriodEnum.LastYear):
                    var dateTime3 = DateTime.UtcNow.AddYears(-1);
                    worksQuery = worksQuery.Where(x => x.CreatedAt >= dateTime3);
                    break;
            }

            //Sorting
            var sortedWorksQuery = worksQuery;
            switch (sort)
            {
                case SortFieldsEnum.Rating:
                    if (direct)
                        sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.IsFree).ThenBy(x => x.Rating).ThenByDescending(x => x.CreatedAt);
                    else
                        sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.IsFree).ThenByDescending(x => x.Rating).ThenByDescending(x => x.CreatedAt);
                    break;
                case SortFieldsEnum.Date:
                    if (direct)
                        sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.IsFree).ThenBy(x => x.CreatedAt);
                    else
                        sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.IsFree).ThenByDescending(x => x.CreatedAt);
                    break;
                case SortFieldsEnum.Price:
                    if (direct)
                        sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.IsFree).ThenBy(x => x.Price ?? 0).ThenByDescending(x => x.CreatedAt);
                    else
                        sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.IsFree).ThenByDescending(x => x.Price ?? 0).ThenByDescending(x => x.CreatedAt);
                    break;
                case SortFieldsEnum.Title:
                    if (direct)
                        sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.IsFree).ThenBy(x => x.Title);
                    else
                        sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.IsFree).ThenByDescending(x => x.Title);
                    break;
            }


            var viewModel = new CategoryViewModel
            {
                Documents = sortedWorksQuery
                      .Skip((page - 1) * rows)
                //.Take(rows)
                      .ToList()
                      .Select(x => MapperManager.Map<Document, DTO.Documents.DocumentPreviewEx>(x)),
                //Documents = sortedWorksQuery
                //    .Skip((page - 1) * rows)
                //    .Take(rows)
                //    .ToList()
                //    .Select(x => MapperManager.Map<DB.Document, DTO.Documents.DocumentPreviewEx>(x)),
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
                    Types = type,
                    SelectedPeriod = period,
                    Category = category == 0 ? null : new int[1] { category },
                    Theme = theme//additionalTheme
                },
                SorterModel = new SorterModel()
                {
                    SortedField = sort,
                    Ascending = direct
                },
                Types = types,
                Categories = categories
            };

            viewModel.PagerModel.LastPage = Convert.ToInt32(Math.Ceiling(viewModel.PagerModel.Total / (decimal)viewModel.PagerModel.Rows));
            viewModel.PagerModel.Count = viewModel.Documents.Count();

            ViewBag.ThemeTitle = mainTheme.Name;
            ViewBag.ThemeId = mainTheme.Id;
            return View(viewModel);
        }
    }
}