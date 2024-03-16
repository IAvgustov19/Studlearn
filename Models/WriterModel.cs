using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class WriterModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Rating { get; set; }
        public int RatingCount { get; set; }
        public string Image { get; set; }
        public string Description { get; set; }
        public string Website { get; set; }
        public string VkUrl { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public bool IsUserVoted { get; set; }
        public double UserRating { get; set; }

        public bool IsMalefactor { get; set; }
        public IEnumerable<string> Tags { get; set; }
        public NewReviewModel NewReview { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public bool ShowEdit { get; set; }
    }
}