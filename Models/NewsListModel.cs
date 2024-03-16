
using System.Collections.Generic;
using SW.Shared.Models.Documents;

namespace SW.Frontend.Models
{
    public class NewsListModel
    {
        public IEnumerable<SW.Shared.Models.News.NewsDetail> News { get; set; }
        public PagerModel PagerModel { get; set; }
    }
}