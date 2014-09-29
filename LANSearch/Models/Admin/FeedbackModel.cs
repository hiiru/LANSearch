using LANSearch.Data;
using LANSearch.Data.Feedback;
using LANSearch.Models.BaseModels;
using System.Collections.Generic;

namespace LANSearch.Models.Admin
{
    public class FeedbackModel : BaseListModel
    {
        public FeedbackModel(UrlBuilder urlBuilder)
            : base(urlBuilder)
        {
        }

        public List<Feedback> Feedbacks { get; set; }
    }
}