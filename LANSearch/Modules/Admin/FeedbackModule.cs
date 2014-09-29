using LANSearch.Data;
using LANSearch.Models.Admin;
using Nancy;

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
                int id;
                if (int.TryParse(Request.Form.delete, out id))
                {
                    Ctx.FeedbackManager.SetDeleted(id);
                }
                return Response.AsRedirect("/Admin/Feedback");
            };

            //Get["/Admin/Feedback/TEST"] = x =>
            //{
            //    for (int i = 0; i < 1000; i++)
            //    {
            //        AppContext.GetContext().FeedbackManager.Save(new Feedback
            //        {
            //            Name = "user " + i,
            //            Email = "moo." + i + "@mail.lan",
            //            Location = "A" + i,
            //            Text = "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed congue neque erat, at gravida felis malesuada vitae. Fusce finibus elit elit, ac porta sapien consectetur in. Sed ut eros ac tortor ornare condimentum. Aliquam eleifend ex nec nisl blandit tempor. Donec rhoncus lorem ex, sit amet fermentum metus convallis vitae. Quisque laoreet eget metus nec rutrum. In hac habitasse platea dictumst. Sed commodo id dolor vel fermentum. Integer maximus sem velit, sed posuere dolor ultricies quis.",
            //            Created = DateTime.Now.AddMinutes(-i)
            //        });
            //    }
            //    return "OK";
            //};
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