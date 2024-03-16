using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SW.Core.DataLayer;
using SW.Shared.Models.Documents;
using SW.Shared.Models.Account;

namespace SW.Frontend.Models
{
    public class HomePageModel
    {
        public IEnumerable<FeaturedWork> FeaturedFreeWorks { get; set; }
        public IEnumerable<FeaturedWork> FeaturedPayedWorks { get; set; }
        public IEnumerable<FeaturedWork> RecentWorks { get; set; }
        public IEnumerable<FeaturedAuthor> FeaturedAuthors { get; set; }
        public IEnumerable<Slider> Slides { get; set; }

        public int AuthorsCount { get; set; }
        public int PublishedWorksCount { get; set; }
        public List<Theme> Themes { get; internal set; }
    }
}