using MvcSiteMapProvider;
using SW.Core.DataLayer.Documents;
using SW.Frontend.App_Start;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Practices.Unity;

namespace SW.Frontend.Helpers
{
    public class CategoryDynamicNodeProvider : DynamicNodeProviderBase
    {
        private IDocumentsUOW _documentsUow;

        public override IEnumerable<DynamicNode> GetDynamicNodeCollection(ISiteMapNode node)
        {
            _documentsUow = UnityConfig.GetConfiguredContainer().Resolve<IDocumentsUOW>();

            DynamicNode dynamicNodeEmpty = new DynamicNode();
            dynamicNodeEmpty.Title = "Поиск";
            dynamicNodeEmpty.ParentKey = "sections";
            yield return dynamicNodeEmpty;

            var categories = _documentsUow.CategoriesRepository.GetAll()
                .Select(x => new
                {
                    Title = x.Title,
                    CategoryId = x.CategoryId,
                    ParentSectionId = x.ParentSectionId
                });

            foreach (var category in categories)
            {
                DynamicNode dynamicNode = new DynamicNode();
                dynamicNode.Title = category.Title.Length > 60 ? category.Title.Substring(0, 57) + "..." : category.Title;
                dynamicNode.Description = category.Title;
                dynamicNode.ParentKey = "sections_" + category.ParentSectionId;
                dynamicNode.Key = "category_" + category.CategoryId;
                dynamicNode.RouteValues.Add("id", category.CategoryId);

                yield return dynamicNode;
            }
        }
    }
}