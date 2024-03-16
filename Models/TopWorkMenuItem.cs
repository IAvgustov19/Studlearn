using SW.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW.Frontend.Models
{
    public class TopWorkMenuItem
    {
        public string Title { get; set; }
        public int Year { get; set; }
        public Season? Season { get; set; }
        public int? Month { get; set; }
        public bool Active { get; set; }

        public IList<TopWorkMenuItem> Children { get; set; } = new List<TopWorkMenuItem>();
       
    }
}
