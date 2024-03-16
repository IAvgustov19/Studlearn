using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW.Frontend.Microdata.Products.Models
{
    public class Product
    {
        public string Title { get; set; }
        public string Image { get; set; }
        public string Description { get; set; }
        public decimal RatingValue { get; set; }
        public int ReviewCount { get; set; }
        public decimal Price { get; set; }

        public string PriceCurrency { get; set; }

    }
}
