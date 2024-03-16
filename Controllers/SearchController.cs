using System;
using System.Linq;
using System.Web.Mvc;
using SW.Core.DataLayer;
using SW.Core.DataLayer.Documents;
using SW.Frontend.Models;
using SW.Shared.Models.Documents;
using SW.Workflow.Coordinator;
using SW.Workflow.Components.TextSearch;
using System.Configuration;
using SW.Shared.Constants.Search;
using Microsoft.Practices.Unity;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SW.Frontend.Utilities;

namespace SW.Frontend.Controllers
{
    public class SearchController : UnityController
    {
        private readonly IDocumentsUOW _documentsUow;
        private const int SearchResultsLimit = 100;

        public SearchController(IDocumentsUOW documentsUow)
        {
            _documentsUow = documentsUow;
        }

        public ActionResult Index(string q = "", int page = 1, int rows = 10)
        {
            var list = Enumerable.Empty<SimilarDocument>();
            int total = 0;

            //log search request            
            System.Net.IPAddress ip;
            _documentsUow.SearchRequestsRepositoryRepository.Insert(new SearchRequest()
            {
                CreatedAt = DateTime.UtcNow,
                Text = q,
                IP = System.Net.IPAddress.TryParse(Request.UserHostAddress, out ip) && ip.ToString() != "::1" ? ip.GetAddressBytes() : null
            });
            _documentsUow.Commit();

            bool lucenEnabled = String.Equals(ConfigurationManager.AppSettings["LuceneEnabled"], "true", StringComparison.OrdinalIgnoreCase);
            ViewBag.SearchEnabled = lucenEnabled;
            if (lucenEnabled)
            {
                string userName = null;
                string searchString = q;
                var userNameMatch = Regex.Match(q, @"author:(?'author'[\S]+)(\s|$)");
                if (userNameMatch.Success)
                {
                    userName = userNameMatch.Groups["author"].Value;
                    searchString = Regex.Replace(searchString, @"author:(?'author'[\S]+)(\s|$)", "");
                }
                searchString.Replace("author:", "");

                ViewBag.SearchTitle = searchString;

                ISearchText searchComponent = Unity.Resolve<ISearchText>("lucene");
                var results = searchComponent.SearchByTitleAndBriefDescription(searchString, "", SearchType.Approved, take: SearchResultsLimit, userName: userName);
                total = results.Count();
                if (total != 0)
                {
                    var query = results;
                    var Q = Request.QueryString;
                    if (String.Equals(Q["i"], "yes", StringComparison.OrdinalIgnoreCase))
                        query = query.Where(x => x.HasImage == true);
                    if (String.Equals(Q["i"], "no", StringComparison.OrdinalIgnoreCase))
                        query = query.Where(x => x.HasImage == false);
                    if (String.Equals(Q["v"], "yes", StringComparison.OrdinalIgnoreCase))
                        query = query.Where(x => x.HasVideo == true);
                    if (String.Equals(Q["v"], "no", StringComparison.OrdinalIgnoreCase))
                        query = query.Where(x => x.HasVideo == false);
                    if (String.Equals(Q["p"], "yes", StringComparison.OrdinalIgnoreCase))
                        query = query.Where(x => x.Price.HasValue && x.Price > 0);
                    if (String.Equals(Q["p"], "no", StringComparison.OrdinalIgnoreCase))
                        query = query.Where(x => !x.Price.HasValue || x.Price.Value == 0);
                    if (String.Equals(Q["o"], "organic", StringComparison.OrdinalIgnoreCase))
                    {
                        var maxRating = results.Max(x => x.Rating);
                        var minRating = results.Min(x => x.Rating);
                        query = query.OrderByDescending(x => (maxRating - minRating) == 0 ? x.Similarity : ((x.Rating - minRating) / (maxRating - minRating) + x.Similarity));
                    }
                    if (String.Equals(Q["o"], "rating", StringComparison.OrdinalIgnoreCase))
                        query = query.OrderByDescending(x => x.Rating);
                    if (String.Equals(Q["o"], "price", StringComparison.OrdinalIgnoreCase))
                        query = query.OrderByDescending(x => x.Price.HasValue).ThenByDescending(x => x.Price);
                    if (String.Equals(Q["o"], "date", StringComparison.OrdinalIgnoreCase))
                        query = query.OrderByDescending(x => x.CreatedAt);

                    list = query
                        .Skip((page - 1) * rows)
                        .Take(rows);
                }
            }
            var viewModel = new SearchViewModel
            {
                Documents = list,
                PagerModel = new PagerModel
                {
                    QueryString = q,
                    Rows = rows,
                    Total = total,
                    CurrentPage = page
                }

            };
            viewModel.PagerModel.LastPage = Convert.ToInt32(Math.Ceiling(viewModel.PagerModel.Total / (decimal)viewModel.PagerModel.Rows));
            viewModel.PagerModel.Count = viewModel.Documents.Count();

            return View(viewModel);
        }

        [HttpPost]
        public JsonResult AutoComplete(string Prefix)
        {
            return Json(_documentsUow.DocumentsRepository
                .GetAll()
                .Where(x => !(x.IsDeleted || x.DocumentStateId != (int)SW.Shared.Constants.Documents.DocumentState.Approved || x.Category.Hidden) && x.Title.ToLower().Contains(Prefix.ToLower()))
                .Select(x => new
                {
                    DocumentTitle = x.Title,
                    Url = x.Slug
                }).Take(20), JsonRequestBehavior.AllowGet);
        }
    }
}