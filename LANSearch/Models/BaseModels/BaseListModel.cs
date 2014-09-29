using LANSearch.Data;
using System.Collections.Generic;

namespace LANSearch.Models.BaseModels
{
    public class BaseListModel
    {
        protected readonly UrlBuilder _urlBuilder;

        public BaseListModel(UrlBuilder urlBuilder)
        {
            _urlBuilder = urlBuilder;
        }

        public long Count { get; set; }

        public bool HasPaging { get { return Count > PageSize; } }

        public int PageSize { get; set; }

        public int Page { get; set; }

        public List<PagingItem> GetPages()
        {
            if (!HasPaging) return null;
            var lastPage = PageSize > 0 ? Count / PageSize : 0;
            var pages = new List<PagingItem>();
            pages.Add(Page == 0
                ? new PagingItem("disabled", "#", "«")
                : new PagingItem("", _urlBuilder.Remove("p").ToString(), "«"));
            int firstPageNumber = Page - 3;
            int lastPageNumber = Page + 3;
            for (int i = firstPageNumber; i <= lastPage && i <= lastPageNumber; i++)
            {
                if (i < 0) continue;
                pages.Add(new PagingItem(i == Page ? "active" : "", i == Page ? "#" : _urlBuilder.Set("p", i.ToString()).ToString(), (i + 1).ToString()));
            }
            pages.Add(new PagingItem(Page == lastPage ? "disabled" : "", Page == lastPage ? "#" : _urlBuilder.Set("p", lastPage.ToString()).ToString(), "»"));
            return pages;
        }
    }
}