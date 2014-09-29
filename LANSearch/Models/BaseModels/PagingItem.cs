namespace LANSearch.Models.BaseModels
{
    public class PagingItem
    {
        public PagingItem(string css, string url, string lbl)
        {
            CssClass = css;
            Url = url;
            Label = lbl;
        }

        public string CssClass;
        public string Url;
        public string Label;
    }
}