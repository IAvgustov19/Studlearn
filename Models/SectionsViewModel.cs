using SW.Shared.Models.Documents;
using SW.Shared.Models.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class SectionsViewModel
    {
        public string Title { get; set; }
        public string Slug { get; set; }

        public IEnumerable<SectionModel> Sections { get; set; }

        public IEnumerable<CategoriesModel> Categories { get; set; }

        public IEnumerable<TypePreview> Types { get; set; }

        public IEnumerable<DocumentPreviewEx> Documents { get; set; }
        public WorkFilterItem Filter { get; set; }
        public PagerModel PagerModel { get; set; }
        public SorterModel SorterModel { get; set; }
    }
}