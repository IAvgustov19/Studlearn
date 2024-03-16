using System.Collections.Generic;
using SW.Shared.Models.Documents;

namespace SW.Frontend.Models
{
    public class SearchViewModel
    {
        public IEnumerable<SimilarDocument> Documents { get; set; }
        public PagerModel PagerModel { get; set; }
    }
}