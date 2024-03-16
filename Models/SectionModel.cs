using SW.Shared.Models.Documents;
using SW.Shared.Models.Filter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class SectionModel
    {
        public int SectionId { get; set; }
        public string Title { get; set; }
        public int Count { get; set; }
        public string Slug { get; set; }
        public IEnumerable<CategoriesModel> CategoriesModels { get; set; }
        public IEnumerable<TypePreview> TypesModels { get; set; }
    }
}