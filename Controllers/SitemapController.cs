using SimpleMvcSitemap;
using SW.Core.DataLayer.Account;
using SW.Core.DataLayer.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Entity;
using System.Threading.Tasks;
using SW.Shared.Constants.Documents;
using SW.Shared.Constants.Storage;
using SW.Core.DataLayer.ExternalWriters;
using SW.Core.DataLayer.NewsUnit;

namespace SW.Frontend.Controllers
{
    public class SitemapController : Controller
    {
        public IDocumentsUOW DocumentsUOW { get; set; }
        public IAccountUOW AccountUOW { get; set; }
        public IExternalWritersUOW WritersUOW { get; set; }

        public INewsUOW NewsUOW { get; set; }

        public class Work
        {
            public long Id;
            public string Slug;
            public DateTime ModifiedAt;
        }

        public SitemapController(IDocumentsUOW documentsUOW, IAccountUOW accountUOW, IExternalWritersUOW writersUOW, INewsUOW newsUOW)
        {
            DocumentsUOW = documentsUOW;
            AccountUOW = accountUOW;
            WritersUOW = writersUOW;
            NewsUOW = newsUOW;
        }

        public async Task<ActionResult> Index()
        {
            List<SitemapNode> nodes = new List<SitemapNode>();
            // first level (main menu)
            nodes.Add(new SitemapNode(Url.Action("Index", "Home", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Daily, Priority = 1 });
            nodes.Add(new SitemapNode(Url.Action("Index", "Sections", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Weekly, Priority = 0.9M });
            nodes.Add(new SitemapNode(Url.Action("Index", "Authors", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Weekly, Priority = 0.5M });
            nodes.Add(new SitemapNode(Url.Action("About", "Home", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Weekly, Priority = 0.4M });
            nodes.Add(new SitemapNode(Url.Action("Faq", "Home", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Weekly, Priority = 0.4M });
            nodes.Add(new SitemapNode(Url.Action("Terms", "Home", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Monthly, Priority = 0.4M });
            nodes.Add(new SitemapNode(Url.Action("Income", "Home", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Monthly, Priority = 0.7M });
            nodes.Add(new SitemapNode(Url.Action("Contacts", "Home", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Monthly, Priority = 0.4M });
            //nodes.Add(new SitemapNode(Url.Action("Achievements", "Home", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Monthly, Priority = 0.2M });
            nodes.Add(new SitemapNode(Url.Action("Index", "News", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Monthly, Priority = 0.7M });

            //nodes.Add(new SitemapNode(Url.Action("Index", "Free", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Daily, Priority = 0.75M });
            //nodes.Add(new SitemapNode(Url.Action("Index", "Paid", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Daily, Priority = 0.75M });
            //nodes.Add(new SitemapNode(Url.Action("Index", "Recent", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Daily, Priority = 0.75M });            
            nodes.Add(new SitemapNode(Url.Action("Free", "TopWorks", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Daily, Priority = 0.6M });
            nodes.Add(new SitemapNode(Url.Action("Paid", "TopWorks", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Daily, Priority = 0.6M });
            nodes.Add(new SitemapNode(Url.Action("Recent", "TopWorks", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Daily, Priority = 0.6M });            
            // second level (categories)
            var categories = await DocumentsUOW.CategoriesRepository.GetAll()
                .Where(x => x.Hidden == false)
                .Select(x => new { x.CategoryId, x.Slug })
                .ToListAsync();
            foreach (var c in categories)
            {
                if (string.IsNullOrEmpty(c.Slug))
                {
                    nodes.Add(new SitemapNode(Url.Action("Details", "Category", new { id = c.CategoryId }, this.Request.Url.Scheme))
                    {
                        ChangeFrequency = ChangeFrequency.Daily, Priority = 0.6M
                    });
                }
                else
                {
                    nodes.Add(new SitemapNode(Url.Action("Index", "Category", new { id = c.Slug }, this.Request.Url.Scheme))
                    {
                        ChangeFrequency = ChangeFrequency.Daily,
                        Priority = 0.6M
                    });
                }
            }
            // third level (documents)
            var works = await DocumentsUOW.DocumentsRepository.GetAll()
                .Where(x => x.Category.Hidden == false && x.IsDeleted == false && x.DocumentStateId == (int)DocumentState.Approved)
                .Select(x => new { DocId = x.DocumentId, Slug = x.Slug, ModifiedAt = x.ModifiedAt })
                .ToListAsync();
            foreach (var w in works)
            {
                if (String.IsNullOrEmpty(w.Slug))
                {
                    nodes.Add(new SitemapNode(Url.Action("Index", "Works", new { id = w.DocId }, this.Request.Url.Scheme))
                    {
                        LastModificationDate = new DateTime(w.ModifiedAt.Year, w.ModifiedAt.Month, w.ModifiedAt.Day,
                            w.ModifiedAt.Hour, w.ModifiedAt.Minute, w.ModifiedAt.Second,
                            DateTimeKind.Local),
                        Priority = 0.8M,
                    });
                }
                else
                {
                    nodes.Add(new SitemapNode(Url.Action("Details", "Works", new { id = w.Slug }, this.Request.Url.Scheme))
                    {
                        LastModificationDate = new DateTime(w.ModifiedAt.Year, w.ModifiedAt.Month, w.ModifiedAt.Day,
                            w.ModifiedAt.Hour, w.ModifiedAt.Minute, w.ModifiedAt.Second,
                            DateTimeKind.Local),
                        Priority = 0.8M
                    });
                }
            }
            // third level (authors)
            var authors = await AccountUOW.UsersRepository.GetAll()
                .Where(x => x.LockoutEnabled == false)
                .Select(x => new { Id = x.Id, Slug = x.Slug })
                .ToListAsync();
            foreach (var a in authors)
            {
                if (String.IsNullOrEmpty(a.Slug))
                    nodes.Add(new SitemapNode(Url.Action("Profile", "Authors", new { id = a.Id }, this.Request.Url.Scheme)) { Priority = 0.3M });
                else
                    nodes.Add(new SitemapNode(Url.Action("Profile", "Authors", new { id = a.Slug }, this.Request.Url.Scheme)) { Priority = 0.3M });
            }

            // writers           
            nodes.Add(new SitemapNode(Url.Action("Index", "Writers", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Weekly, Priority = 0.8M });
            nodes.Add(new SitemapNode(Url.Action("New", "Writers", null, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Monthly, Priority = 0.8M });
            var slugs = await WritersUOW.ExternalWritersRepository.GetAll()
                .Where(x => x.IsDeleted == false)
                .Select(x => x.Slug)
                .ToListAsync();
            foreach (var s in slugs)
            {
                nodes.Add(new SitemapNode(Url.Action("Profile", "Writers", new { id = s }, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Daily, Priority = 0.7M });
            }
            // news
            var news = await NewsUOW.NewsRepository.GetAll()
                .Where(x => x.Slug != null)
                .Select(x => x.Slug)
                .ToListAsync();
            foreach(var s in news)
            {
                nodes.Add(new SitemapNode(Url.Action("Details", "News", new { id = s }, this.Request.Url.Scheme)) { ChangeFrequency = ChangeFrequency.Weekly, Priority = 0.7M });
            }

            return new SitemapProvider()
                .CreateSitemap(HttpContext, nodes);            
        }

        public async Task<ActionResult> Images()
        {
            List<SitemapNode> nodes = new List<SitemapNode>();
            // works public images
            var works = await DocumentsUOW.DocumentsRepository.GetAll()
                .Where(d => d.DocumentStateId == (int)DocumentState.Approved && !String.IsNullOrEmpty(d.Slug))
                .Where(d => d.UserFiles1.Any(x => x.Type == (short)FileType.Image && x.IsPublic && !String.IsNullOrEmpty(x.LinkToAzure)))
                .Select(x => new
                {
                    Slug = x.Slug,
                    Images = x.UserFiles1.Select(d => new
                    {
                        Url = d.PublicFullLinkToAzure,
                        Title = x.Title
                    })
                })
                .ToListAsync();
            foreach (var w in works)
            {
                var node = new SitemapNode(Url.Action("Details", "Works", new { id = w.Slug }, Request.Url.Scheme));
                node.Images = w.Images.Select(x => new SitemapImage(x.Url) { Title = x.Title }).ToList();
                nodes.Add(node);
            }
            // authors
            var authors = await AccountUOW.UsersRepository.GetAll()
                .Where(x => x.LockoutEnabled == false && !String.IsNullOrEmpty(x.Slug) && !String.IsNullOrEmpty(x.AvatartUrl))
                .Select(x => new
                {
                    Slug = x.Slug,
                    Avatar = x.AvatartUrl,
                    Title = x.UserName
                })
                .ToListAsync();

            foreach (var a in authors)
            {
                var node = new SitemapNode(Url.Action("Profile", "Authors", new { id = a.Slug }, Request.Url.Scheme))
                {
                    Images = new List<SitemapImage>()
                    {
                        new SitemapImage(a.Avatar) {Title = a.Title }
                    }
                };
                nodes.Add(node);
            }

            return new SitemapProvider()
                .CreateSitemap(HttpContext, nodes);
        }
    }
}