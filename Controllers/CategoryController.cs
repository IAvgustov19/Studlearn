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
using System.Web.Routing;

namespace SW.Frontend.Controllers
{
    public class CategoryController : Controller
    {
        private readonly IDocumentsUOW _documentsUow;
        private readonly IAccountUOW _accountUOW;

        public CategoryController(IDocumentsUOW documentsUow, IAccountUOW accountUOW)
        {
            _documentsUow = documentsUow;
            _accountUOW = accountUOW;
        }

        //// GET: Category        
        //public ActionResult Index()
        //{
        //    return RedirectToAction("Details", new { id = 1 });
        //}

        [HandleError(View = "Error", ExceptionType = typeof(InvalidOperationException))]
        public ActionResult Index(String id, string sectionSlug = "", bool free = false, string minmax = null, bool unbuyed = false, bool recent = false, int[] type = null, int theme = 0, TimePeriodEnum period = TimePeriodEnum.Any, int page = 1, int rows = 50, SortFieldsEnum sort = SortFieldsEnum.Rating, bool direct = false)
        {
            if (id == null)
                return RedirectToAction("Details", new { id = 1 });

            int min = 0;
            int max = 10000;
            if (!string.IsNullOrEmpty(minmax) && minmax.Split(new char[] { ';' }).Length == 2)
            {
                if (!int.TryParse(minmax.Split(new char[] { ';' })[0], out min))
                    throw new InvalidOperationException("Bad request");
                if (!int.TryParse(minmax.Split(new char[] { ';' })[1], out max))
                    throw new InvalidOperationException("Bad request");
            }

            var category = _documentsUow.CategoriesRepository.GetAll().FirstOrDefault(x => x.Slug == id && x.Hidden == false);
            var worksQuery = _documentsUow.DocumentsRepository.GetAll()
                .Where(x => x.CategoryId == category.CategoryId)
                .Where(x => x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved);
            if (category == null || category.Hidden)
                throw new InvalidOperationException("Данной категории больше не существует на нашем сайте :(");

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

            if (unbuyed)
            {
                var unbuyedQuery = from docs in worksQuery
                                   from sales in _documentsUow.DocumentSalesRepository._context.DocumentSales
                                              .Where(x => x.DocumentId == docs.DocumentId && x.IsCompleted).DefaultIfEmpty()
                                   where docs.IsFree == false
                                   select new
                                   {
                                       d = docs,
                                       s = sales
                                   };
                worksQuery = unbuyedQuery.Where(x => x.s == null).Select(x => x.d);
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
            if (theme != 0)
                worksQuery = worksQuery
                    .Where(x => x.DocumentThemes.Select(y => y.ThemeId).Contains(theme));

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

            var section = category.Section;
            var sectionModel = new SectionModel()
            {
                Title = section.Title,
                Slug = section.Slug
            };

            //for filter
            //var types = _documentsUow.DocumentTypesRepository.GetAll().Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.DocumentTypeId, Title = x.Title });
            //var types = worksQuery.GroupBy(x => x.DocumentType).Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.Key.DocumentTypeId, Title = x.Key.Title, DocumentsCount = x.Count() });
            var allTypes = _documentsUow.DocumentTypesRepository.GetAll().Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.DocumentTypeId, Title = x.Title, Slug = x.Slug }).ToList();
            var currentTypes = worksQuery.GroupBy(x => x.DocumentType).Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.Key.DocumentTypeId, Title = x.Key.Title, DocumentsCount = x.Count(), Slug = x.Key.Slug }).ToList();
            var otherTypes = allTypes.Where(x => !currentTypes.Any(y => y.TypeId == x.TypeId)).ToList();
            currentTypes.AddRange(otherTypes);

            var siblings = _documentsUow.CategoriesRepository.GetAll()
                .Where(x => x.Hidden == false)
                .Where(x => x.ParentSectionId == category.ParentSectionId)
                .ToList()
                .Select(x => MapperManager.Map<DB.Category, DTO.Documents.CategoryPreview>(x));

            var viewModel = new CategoryViewModel
            {
                SectionModel = sectionModel,
                Documents = sortedWorksQuery
                .Skip((page - 1) * rows)
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
                    Category = new int[] { category.CategoryId },
                    Free = free,
                    PriceRangeMin = min,
                    PriceRangeMax = max,
                    Recent = recent,
                    Unbuyed = unbuyed,
                    Types = type,
                    Theme = theme,
                    SelectedPeriod = period
                },
                Categories = siblings,
                SorterModel = new SorterModel()
                {
                    SortedField = sort,
                    Ascending = direct
                },
                Types = currentTypes
            };
            viewModel.PagerModel.LastPage = Convert.ToInt32(Math.Ceiling(viewModel.PagerModel.Total / (decimal)viewModel.PagerModel.Rows));
            viewModel.PagerModel.Count = viewModel.Documents.Count();

            ViewBag.CategoryId = category.CategoryId;
            ViewBag.CategorySlug = category.Slug;
            ViewBag.CategoryTitle = category.Title;
            ViewBag.CategoryDescription = category.Description;
            ViewBag.CategorySEOTitle = string.IsNullOrEmpty(category.SEOTitle) ? category.Title : category.SEOTitle;
            ViewBag.CategorySEODescription = string.IsNullOrEmpty(category.SEODescription) ? category.Description : category.SEODescription;

            return View(viewModel);
        }

        [HandleError(View = "Error", ExceptionType = typeof(InvalidOperationException))]
        [OutputCache(Duration = 3000)]
        public ActionResult Details(Int32?[] id, bool free = false, string minmax = null, int[] type = null, int theme = 0, TimePeriodEnum period = TimePeriodEnum.Any, int page = 1, int rows = 10, SortFieldsEnum sort = SortFieldsEnum.Rating, bool direct = false)
        {
            var categories = _documentsUow.CategoriesRepository.GetAll().Where(x => id.Contains(x.CategoryId) && x.Hidden == false);
            if (categories.Count() == 0)
            {
                throw new InvalidOperationException("Данной категории больше не существует на нашем сайте :(");
            }

            if (!string.IsNullOrEmpty(categories.FirstOrDefault().Slug))
            {
                RouteValueDictionary routeValueDictionary = new RouteValueDictionary();
                routeValueDictionary.Add("id", categories.FirstOrDefault().Slug);
                // Add more parameters
                foreach (string parameter in Request.QueryString.AllKeys)
                {
                    if (parameter != "id")
                        routeValueDictionary.Add(parameter, Request.QueryString[parameter]);
                }

                return RedirectToActionPermanent("Index", routeValueDictionary);
            }

            if (id == null)
                return RedirectToAction("Details", new { id = 1 });
            int min = 0;
            int max = 10000;
            if (!string.IsNullOrEmpty(minmax) && minmax.Split(new char[] { ';' }).Length == 2)
            {
                if (!int.TryParse(minmax.Split(new char[] { ';' })[0], out min))
                    throw new InvalidOperationException("Bad request");
                if (!int.TryParse(minmax.Split(new char[] { ';' })[1], out max))
                    throw new InvalidOperationException("Bad request");
            }

            var worksQuery = _documentsUow.DocumentsRepository.GetAll()
                .Where(x => id.Contains(x.CategoryId))
                .Where(x => x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved);
            if (categories.Count() == 1 && categories.FirstOrDefault().Hidden)
                throw new InvalidOperationException("Данной категории больше не существует на нашем сайте :(");

            var types = worksQuery.Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.DocumentType.DocumentTypeId, Title = x.DocumentType.Title }).Distinct();
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

            if (theme != 0)
                worksQuery = worksQuery
                    .Where(x => x.DocumentThemes.Select(y => y.ThemeId).Contains(theme));

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

            var siblings = _documentsUow.CategoriesRepository.GetAll()
                .Where(x => x.Hidden == false)
                .Where(x => x.ParentSectionId == categories.FirstOrDefault().ParentSectionId)
                .ToList()
                .Select(x => MapperManager.Map<DB.Category, DTO.Documents.CategoryPreview>(x));

            var viewModel = new CategoryViewModel
            {
                Documents = sortedWorksQuery
                    .Skip((page - 1) * rows)
                    .Take(rows)
                    .ToList()
                    .Select(x => MapperManager.Map<DB.Document, DTO.Documents.DocumentPreviewEx>(x)),
                PagerModel = new PagerModel
                {
                    Rows = rows,
                    Total = worksQuery.Count(),
                    CurrentPage = page
                },
                Filter = new DTO.Filter.WorkFilterItem()
                {
                    Category = id.Select(x => x.Value).ToArray(),
                    Free = free,
                    PriceRangeMin = min,
                    PriceRangeMax = max,
                    Types = type,
                    Theme = theme,
                    SelectedPeriod = period
                },
                Categories = siblings,
                SorterModel = new SorterModel()
                {
                    SortedField = sort,
                    Ascending = direct
                },
                Types = types

            };
            viewModel.PagerModel.LastPage = Convert.ToInt32(Math.Ceiling(viewModel.PagerModel.Total / (decimal)viewModel.PagerModel.Rows));
            viewModel.PagerModel.Count = viewModel.Documents.Count();

            ViewBag.CategoryId = id;
            ViewBag.CategoryTitle = categories.Select(x => x.Title).ToList().Aggregate((current, next) => current + ", " + next);
            ViewBag.CategoryDescription = categories != null && categories.Count() > 0 ? categories.FirstOrDefault().Description : String.Empty;

            return View(viewModel);
        }

        [ChildActionOnly]
        public ActionResult Siblings(Int32 id)
        {
            var category = _documentsUow.CategoriesRepository.GetByID(id);
            var siblings = _documentsUow.CategoriesRepository.GetAll()
                .Where(y => y.Hidden == false)
                .Where(x => x.ParentSectionId == category.ParentSectionId)
                .ToList()
                .Select(x =>
                {
                    var m = MapperManager.Map<DB.Category, DTO.Documents.CategoryPreview>(x);
                    m.Count = x.Documents.Where(z =>
                        z.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved).Count();
                    return m;
                });
            ViewBag.CategoryId = category.CategoryId;
            ViewBag.CategoryTitle = category.Title;
            return PartialView(siblings);
        }

        [ChildActionOnly]
        public ActionResult PopularAuthors(Int32[] id)
        {
            var authors = _accountUOW.UsersRepository.GetAll()
                 .Where(x => x.Documents.Any(d => id.Contains(d.CategoryId)))
                 .Where(x => x.Documents.Where(d => d.DocumentStateId == 2).Count() > 0)
                 .OrderByDescending(x => x.Rating)
                 .Take(5)
                 .ToList()
                 .Select(x => new FeaturedAuthor()
                 {
                     Id = x.Id,
                     AvatartUrl = x.AvatartUrl,
                     Rating = x.Rating,
                     Slug = x.Slug,
                     UserName = x.UserName,
                     WorksPublishedCount = x.Documents.Where(d => d.DocumentStateId == 2).Count()
                 });
            return PartialView(authors);
        }
    }
}