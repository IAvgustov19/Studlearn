using SW.Core.DataLayer;
using SW.Shared.Models.Documents;
using SW.Shared.Models.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class AuthorsViewModel
    {
        public IEnumerable<FeaturedAuthor> Authors { get; set; }
        public PagerModel PagerModel { get; set; }
    }
}