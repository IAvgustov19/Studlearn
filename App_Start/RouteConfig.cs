using SW.Core.DataLayer.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace SW.Frontend
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.LowercaseUrls = true;

            routes.MapRoute(
              name: "About",
              url: "about",
              defaults: new { controller = "home", action = "about" }
         );
            routes.MapRoute(
                name: "Contacts",
                url: "contacts",
                defaults: new { controller = "home", action = "contacts" }
           );
            routes.MapRoute(
                        name: "Terms",
                        url: "terms",
                        defaults: new { controller = "home", action = "terms" }
           );
            routes.MapRoute(
                       name: "FAQ",
                       url: "faq",
                       defaults: new { controller = "home", action = "faq" }
          );
            routes.MapRoute(
           name: "Income",
           url: "income",
           defaults: new { controller = "home", action = "income" }
            );
            //routes.MapRoute(
            //name: "Achievements",
            //url: "achievements",
            //defaults: new { controller = "home", action = "achievements" }
            //);


            //для каталога (новая версия)

            var repository = new DocumentsUOW();
            var sections = repository.SectionsRepository.GetAll().Select(x => x.Slug.ToLower()).ToList().Where(x => !string.IsNullOrEmpty(x)).Select(x => $"({x})");
            var sectionsConstraint = string.Join("|", sections);
            var categories = repository.CategoriesRepository.GetAll().Select(x => x.Slug.ToLower()).ToList().Where(x => !string.IsNullOrEmpty(x)).Select(x => $"({x})");
            var categoriesConstraint = string.Join("|", categories);
            var types = repository.DocumentTypesRepository.GetAll().Select(x => x.Slug.ToLower()).ToList().Where(x => !string.IsNullOrEmpty(x)).Select(x => $"({x})");
            var typesConstraint = string.Join("|", types);
            var themes = repository.ThemesRepository.GetAll().Select(x => x.Slug.ToLower()).ToList().Where(x => !string.IsNullOrEmpty(x)).Select(x => $"({x})");
            var themesConstraint = string.Join("|", themes);
            routes.MapRoute(
                name: "CatalogRoute",
                url: "catalog/{sectionSlug}/{categorySlug}/{worktypeSlug}/{themeSlug}",
                defaults: new { controller = "catalog", action = "Details", categorySlug = UrlParameter.Optional, worktypeSlug = UrlParameter.Optional, themeSlug = UrlParameter.Optional },
                new
                {
                    sectionSlug = sectionsConstraint,
                    categorySlug = categoriesConstraint,
                    worktypeSlug = typesConstraint,
                    themeSlug = themesConstraint
                }
            );
            routes.MapRoute(
                 name: "CatalogRoute2",
                 url: "catalog/{sectionSlug}/{worktypeSlug}/{themeSlug}",
                 defaults: new { controller = "catalog", action = "Details", worktypeSlug = UrlParameter.Optional, themeSlug = UrlParameter.Optional },
                 new
                 {
                     sectionSlug = sectionsConstraint,
                     worktypeSlug = typesConstraint,
                     themeSlug = themesConstraint
                 }
             );
            routes.MapRoute(
                name: "CatalogRoute22",
                url: "catalog/{sectionSlug}/{worktypeSlug}",
                defaults: new { controller = "catalog", action = "Details", worktypeSlug = UrlParameter.Optional },
                new
                {
                    sectionSlug = sectionsConstraint,
                    worktypeSlug = typesConstraint
                }
                );
            routes.MapRoute(
                name: "CatalogRoute23",
                url: "catalog/{sectionSlug}/{themeSlug}",
                defaults: new { controller = "catalog", action = "Details", themeSlug = UrlParameter.Optional },
                new
                {
                    sectionSlug = sectionsConstraint,
                    themeSlug = themesConstraint
                }
            );
            routes.MapRoute(
                name: "CatalogRoute3",
                url: "catalog/{sectionSlug}/{categorySlug}/{themeSlug}",
                defaults: new { controller = "catalog", action = "Details", categorySlug = UrlParameter.Optional, themeSlug = UrlParameter.Optional },
                new
                {
                    sectionSlug = sectionsConstraint,
                    categorySlug = categoriesConstraint,
                    themeSlug = themesConstraint
                }
        );
            routes.MapRoute(
               name: "CatalogRoute32",
               url: "catalog/{sectionSlug}/{categorySlug}",
               defaults: new { controller = "catalog", action = "Details", categorySlug = UrlParameter.Optional },
               new
               {
                   sectionSlug = sectionsConstraint,
                   categorySlug = categoriesConstraint
               }
            );
            routes.MapRoute(
                name: "CatalogRoute4",
                url: "catalog/{sectionSlug}/{categorySlug}/{worktypeSlug}",
                defaults: new { controller = "catalog", action = "Details", categorySlug = UrlParameter.Optional, worktypeSlug = UrlParameter.Optional },
                new
                {
                    sectionSlug = sectionsConstraint,
                    categorySlug = categoriesConstraint,
                    worktypeSlug = typesConstraint
                }
            );
            routes.MapRoute(
              name: "CatalogRoute10",
              url: "catalog/{categorySlug}/{worktypeSlug}/{themeSlug}",
              defaults: new { controller = "catalog", action = "Details", categorySlug = UrlParameter.Optional, worktypeSlug = UrlParameter.Optional, themeSlug = UrlParameter.Optional },
              new
              {
                  categorySlug = categoriesConstraint,
                  worktypeSlug = typesConstraint,
                  themeSlug = themesConstraint
              }
          );
            routes.MapRoute(
                 name: "CatalogRoute11",
                 url: "catalog/{worktypeSlug}/{themeSlug}",
                 defaults: new { controller = "catalog", action = "Details", worktypeSlug = UrlParameter.Optional, themeSlug = UrlParameter.Optional },
                 new
                 {
                     worktypeSlug = typesConstraint,
                     themeSlug = themesConstraint
                 }
             );
            routes.MapRoute(
                name: "CatalogRoute12",
                url: "catalog/{worktypeSlug}",
                defaults: new { controller = "catalog", action = "Details", worktypeSlug = UrlParameter.Optional },
                new
                {
                    worktypeSlug = typesConstraint
                }
                );
            routes.MapRoute(
                name: "CatalogRoute13",
                url: "catalog/{themeSlug}",
                defaults: new { controller = "catalog", action = "Details", themeSlug = UrlParameter.Optional },
                new
                {
                    themeSlug = themesConstraint
                }
            );
            routes.MapRoute(
                name: "CatalogRoute14",
                url: "catalog/{categorySlug}/{themeSlug}",
                defaults: new { controller = "catalog", action = "Details", categorySlug = UrlParameter.Optional, themeSlug = UrlParameter.Optional },
                new
                {
                    categorySlug = categoriesConstraint,
                    themeSlug = themesConstraint
                }
        );
            routes.MapRoute(
               name: "CatalogRoute15",
               url: "catalog/{categorySlug}",
               defaults: new { controller = "catalog", action = "Details", categorySlug = UrlParameter.Optional },
               new
               {
                   categorySlug = categoriesConstraint
               }
            );
            routes.MapRoute(
                name: "CatalogRoute16",
                url: "catalog/{categorySlug}/{worktypeSlug}",
                defaults: new { controller = "catalog", action = "Details", categorySlug = UrlParameter.Optional, worktypeSlug = UrlParameter.Optional },
                new
                {
                    categorySlug = categoriesConstraint,
                    worktypeSlug = typesConstraint
                }
            );
            routes.MapRoute(
           name: "CatalogRoute17",
           url: "catalog/{sectionSlug}",
           defaults: new { controller = "catalog", action = "Details", sectionSlug = UrlParameter.Optional },
           new
           {
               sectionSlug = sectionsConstraint
           }
        );
            //для старого Каталога
            routes.MapRoute(
                name: "Section",
                url: "sections/{slug}",
                defaults: new { controller = "sections", action = "details" }
           );

            routes.MapRoute(
                name: "sectionWorkType",
                url: "sections/{slug}/{worktype}",
                defaults: new { controller = "sections", action = "DetailsByType", worktype = "diplomnye-raboty" },
                new { worktype = typesConstraint }
            );

            routes.MapRoute(
                name: "sectionCategory",
                url: "sections/{sectionSlug}/{id}",
                defaults: new { controller = "category", action = "index", id = UrlParameter.Optional }
           );

            //оставляем старый путь, чтобы при переходе из поисковика открывалась страница с работами категории (а там какое-то время будут старые ссылки)
            routes.MapRoute(
                 name: "Category",
                 url: "category/{id}",
                 defaults: new { controller = "category", action = "index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                name: "News",
                url: "news",
                defaults: new { controller = "news", action = "index", id = UrlParameter.Optional }
           );

            routes.MapRoute(
                 name: "News3",
                 url: "news/error",
                 defaults: new { controller = "news", action = "error" }
            );

            routes.MapRoute(
                 name: "News2",
                 url: "news/{id}",
                 defaults: new { controller = "news", action = "details", id = UrlParameter.Optional }
            );


            routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
            );

            routes.MapRoute(
                "404-PageNotFound",
                "{*url}",
                new { controller = "StaticContent", action = "PageNotFound" }
                );
        }
    }
}
