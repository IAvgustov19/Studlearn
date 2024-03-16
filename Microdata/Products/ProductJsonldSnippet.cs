using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SW.Frontend.Microdata.Products.Models;
using Newtonsoft.Json.Linq;

namespace SW.Frontend.Microdata.Products
{
    class ProductJsonldSnippet : IProductRichSnippet
    {
        public string Build(Product model)
        {
            // validation
            // google error "Missing: best or worst rating"
            if (model.RatingValue == 0M || model.RatingValue == 0M)
                return string.Empty;

            JObject r = new JObject();
            r.Add("@context", "http://schema.org/");
            r.Add("@type", "Product");
            r.Add("name", model.Title);
            r.Add("image", model.Image);
            r.Add("description", model.Description);

            JObject rating = new JObject();
            rating.Add("@type", "AggregateRating");
            rating.Add("ratingValue", model.RatingValue);
            rating.Add("reviewCount", model.ReviewCount);
            r.Add("aggregateRating", rating);

            JObject offer = new JObject();
            offer.Add("@type", "Offer");
            offer.Add("priceCurrency", model.PriceCurrency);
            offer.Add("price", model.Price);
            r.Add("offers", offer);

            return string.Format("<script type=\"application/ld+json\">{0}</script>", 
                r.ToString(Newtonsoft.Json.Formatting.Indented));
        }
    }
}
