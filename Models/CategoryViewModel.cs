using SW.Shared.Models.Documents;
using SW.Shared.Models.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class CategoryViewModel
    {
        public IEnumerable<DocumentPreviewEx> Documents { get; set; }
        public WorkFilterItem Filter { get; set; }
        public PagerModel PagerModel { get; set; }
        public SectionModel SectionModel { get; set; }
        public SorterModel SorterModel { get; set; }
        public IEnumerable<TypePreview> Types { get; set; }
        public IEnumerable<CategoryPreview> Categories { get; set; }
    }
}