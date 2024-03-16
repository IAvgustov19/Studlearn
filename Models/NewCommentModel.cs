using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class NewCommentModel
    {
        public string WorkName { get; set; }
        public int NewAmount { get; set; }
        public string WorkLink { get; set; }
        public string ImageLink { get; set; }

        public string Message { get; set; }
        public string UserName { get; set; }
        public string UserImageLink { get; set; }
        public string UserLink { get; set; }
    }
}