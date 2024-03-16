using SW.Core.DataLayer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SW.Shared.Helpers.Essential;
using SW.Shared.Models.Account;

namespace SW.Frontend.Models
{
    public class TopWork : FeaturedWork
    {
        public string BriefDescription { get; set; }

        public int Visits { get; set; }

        public string TypeTitle { get; set; }

        public string CategoryTitle { get; set; }

        public string Color { get; set; }

        public UserPreview Author { get; set; } = new UserPreview();
        public DateTime CreatedAt { get; set; }

        public List<Shared.Models.Documents.DocumentThemePreview> Themes { get; set; }
    }
}
