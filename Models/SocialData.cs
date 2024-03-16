using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class SocialData
    {
        public String Title { get; set; }
        public String Description { get; set; }
        public String Image { get; set; }

        /// <summary>
        /// If you are using this class for url please call this function before
        /// </summary>
        public void UrlNormalization()
        {
            Title = HttpUtility.UrlEncode(Title ?? string.Empty);
            Description = HttpUtility.UrlEncode(Description ?? string.Empty);
            Image = HttpUtility.UrlEncode(Image ?? string.Empty);
        }
    }
}