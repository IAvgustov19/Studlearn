using SW.Shared.Models.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class ExternalWritersReviewModel
    {
        public IEnumerable<CommentPreview> Reviews { get; set; }
        public PagerModel PagerModel { get; set; }
        public string WriterId { get; set; }
    }
}