using MvcSiteMapProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.Practices.Unity;
using SW.Core.DataLayer.Documents;
using SW.Frontend.App_Start;
using SW.Core.DataLayer.Account;

namespace SW.Frontend.Helpers
{
    public class AuthorSlugDynamicNodeProvider : DynamicNodeProviderBase
        {
        private IAccountUOW _accountUow;

            public override IEnumerable<DynamicNode> GetDynamicNodeCollection(ISiteMapNode node)
            {
                _accountUow = UnityConfig.GetConfiguredContainer().Resolve<IAccountUOW>();

                var authors = _accountUow.UsersRepository.GetAll()
                    .Select(x => new
                    {
                        UserName = x.UserName,
                        Id = x.Id,
                        Slug = x.Slug
                    });

                foreach (var author in authors)
                {
                    DynamicNode dynamicNode = new DynamicNode();
                    dynamicNode.Title = author.UserName.Length > 60 ? author.UserName.Substring(0, 57) + "..." : author.UserName;
                    dynamicNode.Description = author.UserName;
                    dynamicNode.ParentKey = "authors";
                    dynamicNode.RouteValues.Add("id", author.Slug);

                    yield return dynamicNode;
                }
            }
    }
}