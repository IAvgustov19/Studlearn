using MvcSiteMapProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Practices.Unity;
using SW.Core.DataLayer.Documents;
using SW.Frontend.App_Start;

namespace SW.Frontend.Helpers
{
    public class SectionDynamicNodeProvider : DynamicNodeProviderBase
    {
        private IDocumentsUOW _documentsUow;

        public override IEnumerable<DynamicNode> GetDynamicNodeCollection(ISiteMapNode node)
        {
            _documentsUow = UnityConfig.GetConfiguredContainer().Resolve<IDocumentsUOW>();
            var sections = _documentsUow.SectionsRepository.GetAll()
                .Select(x => new
                {
                    Title = x.Title,
                    SectionId = x.SectionId
                });

            foreach (var section in sections)
            {
                DynamicNode dynamicNode = new DynamicNode();

                dynamicNode.Title = section.Title.Length > 60 ? section.Title.Substring(0, 57) + "..." : section.Title;
                dynamicNode.Description = section.Title;
                dynamicNode.Key = "sections_" + section.SectionId;
                yield return dynamicNode;
            }
        }
    }
}