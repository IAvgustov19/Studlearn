using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SW.Frontend.Models
{
    public class EditWiterFieldValueModel
    {
        public int WriterId { get; set; }
        public string Field { get; set; }

        public string Value { get; set; }
    }
}