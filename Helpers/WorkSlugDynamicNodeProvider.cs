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
    public class WorkSlugDynamicNodeProvider : DynamicNodeProviderBase
    {
        private IDocumentsUOW _documentsUow;

        public override IEnumerable<DynamicNode> GetDynamicNodeCollection(ISiteMapNode node)
        {
            _documentsUow = UnityConfig.GetConfiguredContainer().Resolve<IDocumentsUOW>();
            var documents = _documentsUow.DocumentsRepository.GetAll()
                .Select(x => new
                {
                    Id = x.DocumentId,
                    Title = x.Title,
                    CategoryId = x.CategoryId,
                    Slug = x.Slug
                });

            foreach (var document in documents)
            {
                DynamicNode dynamicNode = new DynamicNode();
                dynamicNode.Title = document.Title.Length > 60 ? document.Title.Substring(0, 57) + "..." : document.Title;
                dynamicNode.Description = document.Title;
                dynamicNode.ParentKey = "category_" + document.CategoryId;
                dynamicNode.Key = "work_" + document.Id + "_" + document.Slug;
                dynamicNode.RouteValues.Add("id", document.Slug);                
                yield return dynamicNode;

                DynamicNode paymentDynamicNode = new DynamicNode();
                paymentDynamicNode.Action = "Payment";
                paymentDynamicNode.Title = "Покупка";
                paymentDynamicNode.Description = "Покупка";
                paymentDynamicNode.ParentKey = "work_" + document.Id + "_" + document.Slug;
                paymentDynamicNode.RouteValues.Add("id", document.Slug);
                yield return paymentDynamicNode;

                DynamicNode downloadDynamicNode = new DynamicNode();
                downloadDynamicNode.Action = "File";
                downloadDynamicNode.Title = "Скачивание";
                downloadDynamicNode.Description = "Скачивание";
                downloadDynamicNode.ParentKey = "work_" + document.Id + "_" + document.Slug;
                downloadDynamicNode.RouteValues.Add("documentId", document.Id);
                yield return downloadDynamicNode;
            }
        }
    }
}