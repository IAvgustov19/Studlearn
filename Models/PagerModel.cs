namespace SW.Frontend.Models
{
    public class PagerModel
    {
        public int Total { get; set; }
        public int Rows { get; set; }
        public string QueryString { get; set; }
        public int CurrentPage { get; set; }
        public int LastPage { get; set; }
        public int Count { get; set; }
    }
}