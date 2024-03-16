using SW.Frontend.Models;
using SW.Shared.Models.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SW.Frontend.Microdata
{
    public static class Adapter
    {
        public static Products.Models.Product ConvertToMicrodataProduct(this DocumentPublic work, decimal rating, int reviews)
        {
            Microdata.Products.Models.Product pr = new Microdata.Products.Models.Product();
            pr.Title = work.Title;
            if (work.HasImages)
            {
                pr.Image = work.Images.FirstOrDefault();
            }
            pr.Description = work.BriefDescription;
            pr.Price = work.Price ?? 0;
            pr.PriceCurrency = SW.Shared.Constants.Application.DefaultCurrency;
            pr.RatingValue = rating;
            pr.ReviewCount = reviews;
            return pr;
        }

        public static Products.Models.Product ConvertToMicrodataProduct(this WriterModel model)
        {
            Microdata.Products.Models.Product pr = new Microdata.Products.Models.Product();
            pr.Title = model.Name;
            if (!string.IsNullOrEmpty( model.Image))
            {
                pr.Image = model.Image;
            }
            pr.Description = model.Description;
            pr.RatingValue = (decimal)model.Rating;
            pr.ReviewCount = model.RatingCount;
            return pr;
        }
    }
}
