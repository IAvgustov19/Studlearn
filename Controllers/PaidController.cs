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
    public class PaidController : Controller
    {
        private readonly IDocumentsUOW _documentsUow;

        public PaidController(IDocumentsUOW documentsUow, IAccountUOW accountUOW)
        {
            _documentsUow = documentsUow;
        }

        [CompressContent]
        public ActionResult Index(int page = 1, int rows = 50)
        {
            var worksQuery = _documentsUow.DocumentsRepository.GetAll()
                .Where(x => x.DocumentStateId == (int)SW.Shared.Constants.Documents.DocumentState.Approved);
            worksQuery = worksQuery.Where(x => x.Price != null && x.Price != 0);
            worksQuery = worksQuery.OrderBy(x => x.Rating).ThenByDescending(x => x.CreatedAt);

            var viewModel = new SearchViewModel
            {
                Documents = worksQuery
                    .Select(x => new SimilarDocument()
                    {
                        AuthorId = x.AuthorId,
                        DocumentId = x.DocumentId,
                        DocumentSlug = x.Slug,
                        DocumentTitle = x.Title,
                        Price = x.Price,
                        Category = x.Category.Title,
                        Rating = x.Rating,                        
                    })
                    .OrderByDescending(x => x.Rating)
                    .Skip((page - 1) * rows)
                    .Take(rows)
                    .ToList(),
                PagerModel = new PagerModel
                {
                    Rows = rows,
                    Total = worksQuery.Count(),
                    CurrentPage = page
                },

            };
            viewModel.PagerModel.LastPage = Convert.ToInt32(Math.Ceiling(viewModel.PagerModel.Total / (decimal)viewModel.PagerModel.Rows));
            viewModel.PagerModel.Count = viewModel.Documents.Count();
            ViewBag.ShowTitle = true;
            return View(viewModel);
        }
    }
}