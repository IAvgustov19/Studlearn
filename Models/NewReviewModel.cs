using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class NewReviewModel
    {
        public int Id { get; set; }
        public int score { get; set; }
        public string Message { get; set; }
        public string Name { get; set; }
        public int parentComment { get; set; }
    }
}