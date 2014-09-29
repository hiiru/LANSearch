using System;
using System.Collections.Generic;

namespace LANSearch.Models.Search
{
    public class SearchFilter
    {
        public string Title { get; set; }

        public List<SearchFilterItem> Items { get; set; }

        public bool HasItems { get { return Items != null && Items.Count > 0; } }

        public struct SearchFilterItem
        {
            public string Title { get; set; }

            public Int64 Count { get; set; }

            public string Url { get; set; }

            public bool IsActive { get; set; }
        }
    }
}