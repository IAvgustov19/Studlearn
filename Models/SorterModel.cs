namespace SW.Frontend.Models
{
    public class SorterModel
    {
        public SortFieldsEnum SortedField { get; set; }
        public bool Ascending { get; set; }

        public SortFieldsEnum[] SortFields {
            get
            {
                return new SortFieldsEnum[] {
                    SortFieldsEnum.Rating,
                    SortFieldsEnum.Date,
                    SortFieldsEnum.Price,
                    SortFieldsEnum.Title
                };
            }
        }
        
        public override string ToString()
        {
            return ((SortedField == SortFieldsEnum.Rating ? "" : "sort=" + SortedField.ToString()) +
                (!Ascending ? "" : "&direct=" + Ascending.ToString().ToLower())).Trim(new char[] {'&'});
        }
    }

    public enum SortFieldsEnum
    {
        Rating = 1,
        Date = 2,
        Price = 3,
        Title = 4,
        Category = 5
    }
}