using Mizore.DataMappingHandler.Attributes;
using System;

namespace LANSearch.Data.Search.Solr
{
    public class File
    {
        [SolrIdField("id")]
        public string Id { get; set; }

        [SolrField("server")]
        public int ServerId { get; set; }

        [SolrField("path")]
        public string Path { get; set; }

        [SolrField("size")]
        public long Size { get; set; }

        [SolrField("fileExt")]
        public string Extension { get; set; }

        [SolrField("dateSeenFirst")]
        public DateTime DateFirstSeen { get; set; }

        [SolrField("dateSeenLast")]
        public DateTime DateLastSeen { get; set; }

        //public bool IsOnline { get; set; }
    }
}