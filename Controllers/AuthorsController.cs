using Newtonsoft.Json;
using SW.Core.DataLayer;
using SW.Core.DataLayer.Account;
using SW.Core.DataLayer.Documents;
using SW.Workflow.Coordinator;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using DB = SW.Core.DataLayer;
using DTO = SW.Shared.Models;
using System.Data.Entity;
using SW.Shared.Constants.Achivment;
using SW.Frontend.Models;
using DocumentState = SW.Shared.Constants.Documents.DocumentState;

namespace SW.Frontend.Controllers
{
    public class AuthorsController : Controller
    {
        public IAccountUOW _accountUOW;
        public IDocumentsUOW _documentsUOW;

        public AuthorsController(IAccountUOW accountUOW, IDocumentsUOW documentsUOW)
        {
            _accountUOW = accountUOW;
            _documentsUOW = documentsUOW;
        }

        [CompressContent]
        [OutputCache(CacheProfile = "TopLevelOneHour")]
        public async Task<ActionResult> Index(int page = 1, int rows = 36, bool showAll = false)
        {
            int total = await _accountUOW.UsersRepository.GetAll().Where(x => !x.LockoutEnabled && x.Documents.Where(y => y.DocumentStateId == 2).Count() > 0).CountAsync();
            //для поисковых ботов для реализации rel=canonical https://sitechecker.pro/ru/pagination/
            if (showAll)
            {
                rows = total;
            }

            var authors = await _accountUOW.UsersRepository
                ._context.Database
                .SqlQuery<FeaturedAuthor>("GetFeaturedAuthorsWithWorks @p0, @p1",
                    new SqlParameter("p0", rows),
                    new SqlParameter("p1", (page - 1)))
                .ToListAsync();
            PagerModel pagerModel = new PagerModel()
            {
                Rows = rows,
                Total = total,
                CurrentPage = page
            };
            pagerModel.LastPage = Convert.ToInt32(Math.Ceiling(pagerModel.Total / (decimal)pagerModel.Rows));
            pagerModel.Count = authors.Count;
            foreach (FeaturedAuthor featuredAuthor in authors)
            {
                FeaturedAuthor author = featuredAuthor;
                int worksCount = _documentsUOW.DocumentsRepository
                    .GetAll()
                    .Where(x => x.DocumentStateId == 2)
                    .Where(x => x.Category.Hidden == false)
                    .Count(x => x.AuthorId == author.Id);
                author.WorksPublishedCount = worksCount;
            }
            AuthorsViewModel result = new AuthorsViewModel()
            {
                Authors = authors,
                PagerModel = pagerModel
            };
            return View(result);

            //var authors = await _accountUOW.UsersRepository
            //    ._context.Database
            //    .SqlQuery<Core.DataLayer.FeaturedAuthor>("GetFeaturedAuthors @p0, @p1",
            //        new SqlParameter("p0", rows),
            //        new SqlParameter("p1", page - 1)
            //    )
            //    .ToListAsync();

            //var total = await _accountUOW.UsersRepository.GetAll().Where(x => !x.LockoutEnabled).CountAsync();

            //var pagerModel = new PagerModel()
            //{
            //    Rows = rows,
            //    Total = total,
            //    CurrentPage = page
            //};
            //pagerModel.LastPage = Convert.ToInt32(Math.Ceiling(pagerModel.Total / (decimal)pagerModel.Rows));
            //pagerModel.Count = authors.Count;
            ////работы автора
            //foreach (var author in authors)
            //{
            //    var worksCount = _documentsUOW.DocumentsRepository
            //        .GetAll()
            //        .Where(x => x.DocumentStateId == (int)DocumentState.Approved)
            //        .Where(x => x.Category.Hidden == false).Count(x => x.AuthorId == author.Id);
            //    author.WorksPublishedCount = worksCount;
            //}

            ////конец работы автора
            //var result = new AuthorsViewModel()
            //{
            //    Authors = authors,
            //    PagerModel = pagerModel
            //};

            //return View(result);
        }

        [CompressContent]
        //[DonutOutputCache(CacheProfile = "TopLevelOneHour")]
        public async Task<ActionResult> Details(String id)
        {
            var dbAuthor = _accountUOW.UsersRepository.GetByID(id);
            if (dbAuthor == null || dbAuthor.LockoutEnabled || String.IsNullOrEmpty(dbAuthor.Slug))
                return RedirectToAction("Error", "Authors");
            return RedirectToAction("Profile", "Authors", new { id = dbAuthor.Slug });
        }

        [CompressContent]
        //[DonutOutputCache(CacheProfile = "TopLevelOneHour")]
        public async Task<ActionResult> Profile(String id)
        {
            var dbAuthor = await _accountUOW.UsersRepository.GetAll().FirstOrDefaultAsync(x => x.Slug == id);
            if (dbAuthor == null || dbAuthor.LockoutEnabled)
                return RedirectToAction("Error", "Authors");
            var dtoAuthor = MapperManager.Map<DB.AspNetUser, DTO.Account.UserDetails>(dbAuthor);

            #region Author's stats

            dtoAuthor.FreeWorksCount = await _documentsUOW.DocumentsRepository.GetAll()
                .CountAsync(x => x.AuthorId == dbAuthor.Id && (!x.Price.HasValue || x.Price.Value == 0) && !x.IsDeleted && x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.Approved);
            dtoAuthor.PaidWorksCount = await _documentsUOW.DocumentsRepository.GetAll()
                .CountAsync(x => x.AuthorId == dbAuthor.Id && (x.Price.HasValue && x.Price.Value > 0) && !x.IsDeleted && x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.Approved);
            dtoAuthor.NewWorksCount = await _documentsUOW.DocumentsRepository.GetAll()
               .CountAsync(x => x.AuthorId == dbAuthor.Id && !x.IsDeleted && (x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.Denied || x.DocumentStateId == (int)Shared.Constants.Documents.DocumentState.OnReview));

            #endregion

            #region Author's achievements

            dtoAuthor.Achivments = dbAuthor.AspNetUserAchivments.ToList()
                .Select(x => new DTO.Account.AchivmentsInfo()
                {
                    Icon = ((AchivmentRank)x.Rank).Icon(),
                    Title = x.Title,
                    Description = x.Description,
                    Color = ((AchivmentRank)x.Rank).Color()
                });

            #endregion

            #region Rating chart's data

            var ratingItems = await _accountUOW.RefillsRepository._context.Database
                .SqlQuery<HistoryStat>("GetAuthorRatingChanges @authorId, @days",
                    new SqlParameter("authorId", dbAuthor.Id),
                    new SqlParameter("days", SW.Shared.Constants.Application.MaxAuthorRatingDays)
                )
                .ToListAsync();
            ViewBag.RatingItems = await Task.Factory.StartNew(() => JsonConvert.SerializeObject(ratingItems, Formatting.None));

            #endregion

            return View("Details", dtoAuthor);
        }



        public ActionResult Error()
        {
            return View();
        }

        [ChildActionOnly]
        public ActionResult AuthorFeaturedComments(String id)
        {
            var commentOption = new DTO.Documents.CommentOptions
            {
                AvatarEnabled = false,
                RatingEnabled = true,
                ReplyEnabled = false
            };
            var comments = _documentsUOW.CommentsRepository.GetAll()
                .Where(x => x.AuthorId == id)
                .OrderByDescending(x => x.Rating)
                .Take(SW.Shared.Constants.Application.MaxAuthorFeaturedComments)
                .ToList()
                .Select(x => MapperManager.Map<DB.Comment, DTO.Documents.CommentPreview>(x))
                .ToList();
            comments
                .ForEach(x => x.Options = commentOption);
            return PartialView(comments);
        }

        [ChildActionOnly]
        public ActionResult AuthorFeaturedWorks(String id, string userName, string userSlug = null, bool unbuyed = false)
        {
            var worksQuery = _documentsUOW.DocumentsRepository.GetAll()
                .Where(x => x.AuthorId == id)
                .Where(x => x.Category.Hidden == false)
                .OrderByDescending(x => x.Rating)
                .Where(x => x.IsDeleted == false && x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved && !x.Category.Hidden);

            if (unbuyed)
            {
                var unbuyedQuery = from docs in worksQuery
                                   from sales in _documentsUOW.DocumentSalesRepository._context.DocumentSales
                                              .Where(x => x.DocumentId == docs.DocumentId && x.IsCompleted).DefaultIfEmpty()
                                   where docs.IsFree==false
                                   select new
                                   {
                                       d = docs,
                                       s = sales
                                   };
                worksQuery = unbuyedQuery.Where(x => x.s == null).Select(x => x.d);
            }

            var works = worksQuery.ToList()
                .Select(x => MapperManager.Map<Document, DTO.Documents.DocumentPreviewEx>(x));

            //var works = _documentsUOW.DocumentsRepository.GetAll()
            //    .Where(x => x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved)
            //    .Where(x => x.Category.Hidden == false)
            //    .Where(x => x.AuthorId == id)
            //    .OrderByDescending(x => x.Rating)
            //    .Take(SW.Shared.Constants.Application.MaxAuthorFeaturedWorks)
            //    .Select(x => new DTO.Documents.SimilarDocument
            //    {
            //        DocumentId = x.DocumentId,
            //        DocumentTitle = x.Title,
            //        CreatedAt = x.CreatedAt,
            //        Rating = x.Rating,
            //        AuthorId = x.AuthorId,
            //        AuthorUsername = userName,
            //        AuthorSlug = userSlug,
            //        Category = x.Category.Title,
            //        CategoryId = x.CategoryId,
            //        BriefDescription = x.BriefDescription,
            //        Price = x.Price,
            //        Type = x.DocumentType.Title,
            //        DocumentSlug = x.Slug
            //    })
            //    .ToList().AsEnumerable();
            return PartialView(works);
        }
    }
}