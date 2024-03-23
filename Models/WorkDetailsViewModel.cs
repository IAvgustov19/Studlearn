using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SW.Core.DataLayer;
using SW.Shared.Models.Documents;

namespace SW.Frontend.Models
{
    public class WorkDetailsViewModel
    {
        public DocumentPublic DocumentPublic { get; set; }

        public IEnumerable<FeaturedWork> FeaturedWorks { get; set; }

        public string AuthorImageUrl { get; set; }

        public int WorksCount { get; set; }

        public int AuthorRating { get; set; }

        public string AuthorDescription { get; set; }
    }
}