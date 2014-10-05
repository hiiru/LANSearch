using LANSearch.Data;
using LANSearch.Models.Admin;
using LANSearch.Modules.BaseClasses;
using Nancy;
using Nancy.Security;

namespace LANSearch.Modules.Admin
{
    public class FeedbackModule : AdminModule
    {
        public FeedbackModule()
        {
            Get["/Feedback"] = x =>
            {
                var model = GetFeedbackModel(Request);
                return View["Admin/Feedback.cshtml", model];
            };
            Post["/Feedback"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("CSRF Token is invalid.").WithStatusCode(403);
                }
                int id;
                if (int.TryParse(Request.Form.delete, out id))
                {
                    Ctx.FeedbackManager.SetDeleted(id);
                }
                return Response.AsRedirect("/Admin/Feedback");
            };
        }

        public FeedbackModel GetFeedbackModel(Request request)
        {
            var feedback = new FeedbackModel(new UrlBuilder(Request.Url));
            bool showDeleted = false, showOnlyNew = false;
            foreach (var qs in request.Query)
            {
                var qsKey = qs as string;
                if (qsKey == null) continue;
                switch (qsKey)
                {
                    case "p":
                        int page;
                        if (int.TryParse(request.Query["p"], out page))
                            feedback.Page = page;
                        break;

                    case "ps":
                        int pagesize;
                        if (int.TryParse(request.Query["ps"], out pagesize))
                        {
                            feedback.PageSize = pagesize;
                        }
                        break;

                    case "sn":
                        showOnlyNew = true;
                        break;

                    case "sd":
                        showDeleted = true;
                        break;
                }
            }

            if (feedback.Page < 0)
                feedback.Page = 0;
            if (feedback.PageSize < 20)
                feedback.PageSize = 20;

            int count = 0;
            feedback.Feedbacks = Ctx.FeedbackManager.GetPaged(feedback.Page, feedback.PageSize, out count, showOnlyNew, showDeleted);
            feedback.Count = count;

            return feedback;
        }
    }
}