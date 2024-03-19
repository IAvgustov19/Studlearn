using SW.Core.DataLayer;
using SW.Core.DataLayer.Documents;
using DTO = SW.Shared.Models;
using SW.Frontend.Models;
using DB = SW.Core.DataLayer;
using SW.Workflow.Coordinator;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Constants = SW.Shared.Constants;
using SW.Shared.Models.Filter;
using System.IdentityModel.Services;
using System.Security.Permissions;
using SW.Shared.Models.Documents;
using System.Web.Http.Controllers;

namespace SW.Frontend.Controllers
{
    //[ClaimsPrincipalPermission(SecurityAction.Demand, Resource = "Users", Operation = "all")]

    public class CatalogController : Controller
    {
        private readonly IDocumentsUOW _documentsUow;

        public CatalogController(IDocumentsUOW documentsUow)
        {
            _documentsUow = documentsUow;
        }

        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        public ActionResult Index()
        {
            var sections = _documentsUow.SectionsRepository.GetAll()
                .Where(x => x.Categories.Any(c => c.Hidden == false))
                .OrderBy(x => !x.Order.HasValue)
                .ThenBy(x => x.Order)
                .Select(x => new SectionModel()
                {
                    Title = x.Title,
                    Slug = x.Slug,
                    CategoriesModels = x.Categories
                        .Where(y => y.Hidden == false)
                        .OrderBy(y => y.Title)
                        .Select(y => new CategoriesModel()
                        {
                            CategoryId = y.CategoryId,
                            Title = y.Title,
                            Count = y.Documents
                              .Where(z => z.DocumentStateId == (int)Constants.Documents.DocumentState.Approved).Count(),
                            Slug = y.Slug
                        })
                }).ToList();
            ViewBag.Total = sections.Sum(x => x.CategoriesModels.Sum(y => y.Count));

            var worksQuery = _documentsUow.DocumentsRepository.GetAll()
              .Where(x => x.Category.ParentSectionId == 2)//Программирование
            .Where(x => !x.IsDeleted && x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden);
            var sectionTypes = worksQuery.GroupBy(x => x.DocumentType).Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.Key.DocumentTypeId, Title = x.Key.Title, DocumentsCount = x.Count(), Slug = x.Key.Slug });
            ViewBag.SectionTypes = sectionTypes.OrderBy(x => x.Title).ToList();

            var query = _documentsUow.DocumentThemesRepository.GetAll()
                .Join(_documentsUow.DocumentsRepository.GetAll(), dt => dt.DocumentId, d => d.DocumentId, (dt, d) => new { dt = dt, d = d });
            var themes = query
                .Where(x => x.d.IsDeleted == false && x.d.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.d.Category.Hidden)
                .GroupBy(x => x.dt.Theme)
                .Select(x => new ThemePreview() { Id = x.Key.Id, Name = x.Key.Name, Slug = x.Key.Slug, DocumentsCount = x.Count() });
            ViewBag.Themes = themes.ToList();

            return View(sections);
        }

        [CompressContent]
        public ActionResult _CatalogPartial()
        {
            var sections = _documentsUow.SectionsRepository.GetAll()
                .Where(x => x.Categories.Any(c => c.Hidden == false))
                .OrderBy(x => !x.Order.HasValue)
                .ThenBy(x => x.Order)
                .Select(x => new SectionModel()
                {
                    Title = x.Title,
                    Slug = x.Slug,
                    CategoriesModels = x.Categories
                        .Where(y => y.Hidden == false)
                        .OrderBy(y => y.Title)
                        .Select(y => new CategoriesModel()
                        {
                            CategoryId = y.CategoryId,
                            Title = y.Title,
                            Count = y.Documents
                              .Where(z => z.DocumentStateId == (int)Constants.Documents.DocumentState.Approved).Count(),
                            Slug = y.Slug
                        })
                }).ToList();
            ViewBag.Total = sections.Sum(x => x.CategoriesModels.Sum(y => y.Count));

            var worksQuery = _documentsUow.DocumentsRepository.GetAll()
              .Where(x => x.Category.ParentSectionId == 2)//Программирование
            .Where(x => !x.IsDeleted && x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden);
            var sectionTypes = worksQuery.GroupBy(x => x.DocumentType).Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.Key.DocumentTypeId, Title = x.Key.Title, DocumentsCount = x.Count(), Slug = x.Key.Slug });
            ViewBag.SectionTypes = sectionTypes.OrderBy(x => x.Title).ToList();

            var query = _documentsUow.DocumentThemesRepository.GetAll()
                .Join(_documentsUow.DocumentsRepository.GetAll(), dt => dt.DocumentId, d => d.DocumentId, (dt, d) => new { dt = dt, d = d });
            var themes = query
                .Where(x => x.d.IsDeleted == false && x.d.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.d.Category.Hidden)
                .GroupBy(x => x.dt.Theme)
                .Select(x => new ThemePreview() { Id = x.Key.Id, Name = x.Key.Name, Slug = x.Key.Slug, DocumentsCount = x.Count() });
            ViewBag.Themes = themes.ToList();

            return PartialView(sections);
        }

        [CompressContent]
        //[OutputCache(CacheProfile = "TopLevelOneHour")]
        //[OutputCache(Duration = 3000, VaryByParam = "sectionSlug;categorySlug;worktypeSlug;themeSlug")]

        [Route("catalog/{sectionSlug?}/{categorySlug?}/{worktypeSlug?}/{themeSlug?}")]
        public ActionResult Details(string sectionSlug = "", string categorySlug = "", string worktypeSlug = "", string themeSlug = "", bool free = false, string minmax = null, bool unbuyed = false, bool recent = false, TimePeriodEnum period = TimePeriodEnum.Any, int page = 1, int rows = 50, SortFieldsEnum sort = SortFieldsEnum.Category, bool direct = false)
        {
            //if (!(string.IsNullOrEmpty(sectionSlug) || ))
            //    throw new InvalidOperationException("Данного раздела больше не существует на нашем сайте :(");

            var worksQuery = _documentsUow.DocumentsRepository.GetAll()
              .Where(x => x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden);

            //filtering
            var responseFilterSectionId = 0;
            var responseFilterCategoryId = 0;
            var responseFilterWorkTypeId = 0;
            var responseFilterThemeId = 0;

            var responseFilterSectionTitle = "";
            var responseFilterCategoryTitle = "";
            var responseFilterWorkTypeTitle = "";
            var responseFilterThemeTitle = "";

            if (!string.IsNullOrEmpty(sectionSlug))
            {
                var section = _documentsUow.SectionsRepository.GetAll().FirstOrDefault(x => x.Slug.ToLower() == sectionSlug.ToLower());
                worksQuery = worksQuery.Where(x => x.Category.ParentSectionId == section.SectionId);
                responseFilterSectionId = section.SectionId;
                responseFilterSectionTitle = section.Title;
            }

            if (!string.IsNullOrEmpty(categorySlug))
            {
                var category = _documentsUow.CategoriesRepository.GetAll().FirstOrDefault(x => x.Slug.ToLower() == categorySlug.ToLower());
                worksQuery = worksQuery.Where(x => x.CategoryId == category.CategoryId);
                responseFilterCategoryId = category.CategoryId;
                responseFilterCategoryTitle = category.Title;
            }
            if (!string.IsNullOrEmpty(worktypeSlug))
            {
                var workType = _documentsUow.DocumentTypesRepository.GetAll().FirstOrDefault(x => x.Slug.ToLower() == worktypeSlug.ToLower());
                worksQuery = worksQuery.Where(x => x.TypeId == workType.DocumentTypeId);
                responseFilterWorkTypeId = workType.DocumentTypeId;
                responseFilterWorkTypeTitle = workType.Title;
            }
            if (!string.IsNullOrEmpty(themeSlug))
            {
                var theme = _documentsUow.ThemesRepository.GetAll().FirstOrDefault(x => x.Slug.ToLower() == themeSlug.ToLower());
                worksQuery = worksQuery.Where(x => x.DocumentThemes.Select(y => y.ThemeId).Contains(theme.Id));
                responseFilterThemeId = theme.Id;
                responseFilterThemeTitle = theme.Name;
            }

            int min = 0;
            int max = 10000;
            if (!string.IsNullOrEmpty(minmax) && minmax.Split(new char[] { ';' }).Length == 2)
            {
                if (!int.TryParse(minmax.Split(new char[] { ';' })[0], out min))
                    throw new InvalidOperationException("Bad request");
                if (!int.TryParse(minmax.Split(new char[] { ';' })[1], out max))
                    throw new InvalidOperationException("Bad request");
            }

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
                case SortFieldsEnum.Category:
                    if (direct)
                        sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.IsFree).ThenBy(x => x.CategoryId).ThenByDescending(x => x.CreatedAt);
                    else
                        sortedWorksQuery = sortedWorksQuery.OrderBy(x => x.IsFree).ThenByDescending(x => x.CategoryId).ThenByDescending(x => x.CreatedAt);
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

            //for filter
            var allTypes = _documentsUow.DocumentTypesRepository.GetAll().Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.DocumentTypeId, Title = x.Title, Slug = x.Slug }).ToList();
            var currentTypes = worksQuery.GroupBy(x => x.DocumentType).Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.Key.DocumentTypeId, Title = x.Key.Title, DocumentsCount = x.Count(), Slug = x.Key.Slug }).ToList();
            var otherTypes = allTypes.Where(x => !currentTypes.Any(y => y.TypeId == x.TypeId)).ToList();
            currentTypes.AddRange(otherTypes);

            var siblingsQuery = _documentsUow.CategoriesRepository.GetAll()
                .Where(x => x.Hidden == false);
            if (responseFilterSectionId != -1)
            {
                siblingsQuery = siblingsQuery.Where(x => x.ParentSectionId == responseFilterSectionId);
            }
            var siblings = siblingsQuery
              .ToList()
              .Select(x => MapperManager.Map<DB.Category, DTO.Documents.CategoryPreview>(x));
            var sections = _documentsUow.SectionsRepository.GetAll()
                .Where(x => x.Categories.Any(c => c.Hidden == false))
                .OrderBy(x => !x.Order.HasValue)
                .ThenBy(x => x.Order)
                .Select(x => new SectionModel()
                {
                    Title = x.Title,
                    Slug = x.Slug,
                    CategoriesModels = x.Categories
                        .Where(y => y.Hidden == false)
                        .OrderBy(y => y.Title)
                        .Select(y => new CategoriesModel()
                        {
                            CategoryId = y.CategoryId,
                            Title = y.Title,
                            //Count = y.Documents
                            //  .Where(z => z.DocumentStateId == (int)Constants.Documents.DocumentState.Approved).Count(),
                            Slug = y.Slug
                        })
                }).ToList();

            var materializedSortedWorks = sortedWorksQuery.Skip((page - 1) * rows).ToList();
            var catalogModel = new CatalogViewModel()
            {
                Documents = materializedSortedWorks
                .Select(x => MapperManager.Map<Document, DTO.Documents.DocumentPreviewEx>(x)).ToList(),
                //Title = section.Title,
                PagerModel = new PagerModel
                {
                    Rows = rows,
                    Total = 10,// worksQuery.Count(),
                    CurrentPage = page
                },
                Filter = new DTO.Filter.WorkFilterItem()
                {
                    SectionSlug = sectionSlug,
                    CategorySlug = categorySlug,
                    TypeSlug = worktypeSlug,
                    ThemeSlug = themeSlug,
                    SectionTitle = responseFilterSectionTitle,
                    CategoryTitle = responseFilterCategoryTitle,
                    TypeTitle = responseFilterWorkTypeTitle,
                    ThemeTitle = responseFilterThemeTitle,
                    Section = responseFilterSectionId,
                    Free = free,
                    PriceRangeMin = min,
                    Category = responseFilterCategoryId == 0 ? new int[] { } : new int[] { responseFilterCategoryId },
                    Types = responseFilterWorkTypeId == 0 ? new int[] { } : new int[] { responseFilterWorkTypeId },
                    Theme = responseFilterThemeId,
                    PriceRangeMax = max,
                    Recent = recent,
                    Unbuyed = unbuyed,
                    SelectedPeriod = period
                },
                SorterModel = new SorterModel()
                {
                    SortedField = sort,
                    Ascending = direct
                },
                Sections = sections,
                Types = currentTypes,
                //Categories = section.Categories
                //        .Where(y => y.Hidden == false)
                //        .OrderBy(y => y.Title)
                //        .Select(y => new CategoriesModel()
                //        {
                //            CategoryId = y.CategoryId,
                //            Title = y.Title,
                //            Count = y.Documents.Where(z => z.DocumentStateId == (int)Constants.Documents.DocumentState.Approved).Count(),
                //            Slug = y.Slug
                //        })
            };

            ViewBag.SectionId = responseFilterSectionId;
            ViewBag.CategoryId = responseFilterCategoryId;
            ViewBag.ThemeId = responseFilterThemeId;
            ViewBag.TypeId = responseFilterWorkTypeId;

            return View(catalogModel);
        }
    }
}