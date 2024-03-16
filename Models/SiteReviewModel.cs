using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class SiteReviewModel
    {
        public string Comment { get; set; }
        public string Emotion { get; set; }
        public byte Points { get; set; }
    }
}