using SW.Shared.Models.ExternalWriters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class ExternalWritersSearchModel
    {
        public IEnumerable<ExternalWriterModel> Writers { get; set; }
        public PagerModel PagerModel { get; set; }

        public int TotalWriters { get; set; }
        public IEnumerable<ShortWriterInfo> RandomWriters { get; set; }
    }

    public class ShortWriterInfo
    {
        public string ImageLink { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }
}