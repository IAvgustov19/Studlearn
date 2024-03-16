using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class CategoriesModel
    {
        public int CategoryId { get; set; }
        public string Title { get; set; }
        public int Count { get; set; }
        public string Slug { get; set; }

        public String Url
        {
            get
            {
                return String.IsNullOrEmpty(Slug) ? "/category/details/" + CategoryId : "/category/" + Slug;
            }
        }
    }
}