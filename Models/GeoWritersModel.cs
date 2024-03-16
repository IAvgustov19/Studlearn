using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class GeoWritersModel
    {
        public decimal Lat { get; set; }
        public decimal Long { get; set; }
        public string Title { get; set; }

        public string Slug { get; set; }

        public double Rating { get; set; }
    }
}