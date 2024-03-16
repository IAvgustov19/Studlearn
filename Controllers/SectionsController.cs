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
using System.Collections.Generic;
using SW.Shared.Models.Documents;

namespace SW.Frontend.Controllers
{
    public class SectionsController : Controller
    {
        private readonly IDocumentsUOW _documentsUow;

        public SectionsController(IDocumentsUOW documentsUow)
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
                .Select(x => new ThemePreview() { Id = x.Key.Id, Name = x.Key.Name, DocumentsCount = x.Count() });
            ViewBag.Themes = themes.ToList();

            return View(sections);
        }

        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        public ActionResult Details(string slug, bool free = false, string minmax = null, bool unbuyed = false, bool recent = false, int category = 0, int[] type = null, int theme = 0, TimePeriodEnum period = TimePeriodEnum.Any, int page = 1, int rows = 50, SortFieldsEnum sort = SortFieldsEnum.Category, bool direct = false)
        {
            var section = _documentsUow.SectionsRepository.GetAll().FirstOrDefault(x => x.Slug.ToLower() == slug.ToLower());

            if (section == null)
                throw new InvalidOperationException("Данного раздела больше не существует на нашем сайте :(");

            var worksQuery = _documentsUow.DocumentsRepository.GetAll()
              .Where(x => x.Category.ParentSectionId == section.SectionId)
              .Where(x => x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden);

            //filtering
            int min = 0;
            int max = 10000;
            if (!string.IsNullOrEmpty(minmax) && minmax.Split(new char[] { ';' }).Length == 2)
            {
                if (!int.TryParse(minmax.Split(new char[] { ';' })[0], out min))
                    throw new InvalidOperationException("Bad request");
                if (!int.TryParse(minmax.Split(new char[] { ';' })[1], out max))
                    throw new InvalidOperationException("Bad request");
            }
            if (type != null)
                worksQuery = worksQuery
                .Where(x => type.Contains(x.TypeId));
            if (category != 0)
                worksQuery = worksQuery
                .Where(x => x.CategoryId == category);
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
            //var types = _documentsUow.DocumentTypesRepository.GetAll().Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.DocumentTypeId, Title = x.Title });
            var types = worksQuery.GroupBy(x => x.DocumentType).Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.Key.DocumentTypeId, Title = x.Key.Title, DocumentsCount = x.Count() }).ToList();

            var siblings = _documentsUow.CategoriesRepository.GetAll()
                .Where(x => x.Hidden == false)
                .Where(x => x.ParentSectionId == section.SectionId)
                .ToList()
                .Select(x => MapperManager.Map<DB.Category, DTO.Documents.CategoryPreview>(x));

            var sectionModel = new SectionsViewModel()
            {
                Documents = sortedWorksQuery
                .Skip((page - 1) * rows)
                .ToList()
                .Select(x => MapperManager.Map<Document, DTO.Documents.DocumentPreviewEx>(x)),
                Title = section.Title,
                Slug = section.Slug,
                PagerModel = new PagerModel
                {
                    Rows = rows,
                    Total = 10,// worksQuery.Count(),
                    CurrentPage = page
                },
                Filter = new DTO.Filter.WorkFilterItem()
                {
                    Section = 2,
                    Category = new int[] { category },
                    Free = free,
                    PriceRangeMin = min,
                    PriceRangeMax = max,
                    Recent = recent,
                    Unbuyed = unbuyed,
                    Types = type,
                    Theme = theme,
                    SelectedPeriod = period
                },
                SorterModel = new SorterModel()
                {
                    SortedField = sort,
                    Ascending = direct
                },
                Types = types,
                Categories = section.Categories
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
            };

            ViewBag.SectionId = section.SectionId;

            return View(sectionModel);
        }


        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        [Route("sections/programmirovanie/{worktype}")]
        public ActionResult DetailsByType(string slug, string worktype, bool free = false, string minmax = null, bool unbuyed = false, bool recent = false, int category = 0, int[] type = null, int theme = 0, TimePeriodEnum period = TimePeriodEnum.Any, int page = 1, int rows = 50, SortFieldsEnum sort = SortFieldsEnum.Category, bool direct = false)
        {
            var section = _documentsUow.SectionsRepository.GetAll().FirstOrDefault(x => x.Slug.ToLower() == slug.ToLower());
            if (section == null)
                throw new InvalidOperationException("Данного раздела больше не существует на нашем сайте :(");
            var wtype = _documentsUow.DocumentTypesRepository.GetAll().FirstOrDefault(x => x.Slug.ToLower() == worktype.ToLower());
            if (wtype == null)
                throw new InvalidOperationException("Данного типа работ больше не существует на нашем сайте :(");

            var title = "";
            var seoDescription = "";
            type = new int[] { wtype.DocumentTypeId };
            switch (worktype)
            {
                case "diplomnye-raboty":
                    title = "Дипломные работы по программированию";
                    seoDescription = "Уникальные дипломные работы по программированию и их исходники. Справочные материалы для скачивания и написания научных работ. Работаем с 2015 года."; break;
                case "laboratornye-raboty":
                    title = "Лабораторные работы по программированию";
                    seoDescription = "Лабораторные работы по программированию и их исходники. Справочные материалы для скачивания и написания научных работ. Работаем с 2015 года. Актуальные готовые варианты.";
                    break;
                case "kursovye-raboty":
                    title = "Курсовые работы по программированию";
                    seoDescription = "Уникальные курсовые работы по программированию и их исходники. Справочные материалы для скачивания и написания научных работ. Работаем с 2015 года.";
                    break;
                default:
                    title = $"{wtype.Title} по " + (section.Title == "Программирование" ? "программированию" : section.Title);
                    seoDescription = $"Уникальные {wtype.Title} по {(section.Title == "Программирование" ? "программированию" : section.Title)} и их исходники. Справочные материалы для скачивания и написания научных работ. Работаем с 2015 года.";
                    break;
            }

            var worksQuery = _documentsUow.DocumentsRepository.GetAll()
              .Where(x => x.Category.ParentSectionId == section.SectionId)
              .Where(x => x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden);

            //filtering
            int min = 0;
            int max = 10000;
            if (!string.IsNullOrEmpty(minmax) && minmax.Split(new char[] { ';' }).Length == 2)
            {
                if (!int.TryParse(minmax.Split(new char[] { ';' })[0], out min))
                    throw new InvalidOperationException("Bad request");
                if (!int.TryParse(minmax.Split(new char[] { ';' })[1], out max))
                    throw new InvalidOperationException("Bad request");
            }
            if (type != null)
                worksQuery = worksQuery
                .Where(x => type.Contains(x.TypeId));
            if (category != 0)
                worksQuery = worksQuery
                .Where(x => x.CategoryId == category);
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
            // var types = _documentsUow.DocumentTypesRepository.GetAll().Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.DocumentTypeId, Title = x.Title });
            //var types = worksQuery.GroupBy(x => x.DocumentType).Select(x => new SW.Shared.Models.Documents.TypePreview() { TypeId = x.Key.DocumentTypeId, Title = x.Key.Title, DocumentsCount = x.Count() });

            var siblings = _documentsUow.CategoriesRepository.GetAll()
                .Where(x => x.Hidden == false)
                .Where(x => x.ParentSectionId == section.SectionId)
                .ToList()
                .Select(x => MapperManager.Map<DB.Category, DTO.Documents.CategoryPreview>(x));

            var sectionModel = new SectionsViewModel()
            {
                Documents = sortedWorksQuery
                .Skip((page - 1) * rows)
                .ToList()
                .Select(x => MapperManager.Map<Document, DTO.Documents.DocumentPreviewEx>(x)),
                Title = title,// section.Title,
                Slug = section.Slug,
                PagerModel = new PagerModel
                {
                    Rows = rows,
                    Total = 10,// worksQuery.Count(),
                    CurrentPage = page
                },
                Filter = new DTO.Filter.WorkFilterItem()
                {
                    Section = 2,
                    Category = new int[] { category },
                    Free = free,
                    PriceRangeMin = min,
                    PriceRangeMax = max,
                    Recent = recent,
                    Unbuyed = unbuyed,
                    Types = type,
                    Theme = theme,
                    SelectedPeriod = period
                },
                SorterModel = new SorterModel()
                {
                    SortedField = sort,
                    Ascending = direct
                },
                Types = new List<DTO.Documents.TypePreview>(),//types,
                Categories = section.Categories
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
            };

            ViewBag.SectionId = section.SectionId;
            ViewBag.SectionSEODescription = seoDescription;
            return View("Details", sectionModel);
        }
    }
}