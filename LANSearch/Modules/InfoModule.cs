using LANSearch.Data.Feedback;
using LANSearch.Models;
using LANSearch.Modules.BaseClasses;
using Nancy;
using Nancy.Security;
using System;

namespace LANSearch.Modules
{
    public class InfoModule : AppModule
    {
        public InfoModule()
        {
            Get["/Info"] = x => View["info.cshtml", new InfoModel()];
            Get["/Info/Success"] = x =>
            {
                var info = new InfoModel
                {
                    IsSuccess = true
                };
                return View["info.cshtml", info];
            };

            Post["/Info"] = x =>
            {
                try
                {
                    this.ValidateCsrfToken();
                }
                catch (CsrfValidationException)
                {
                    return Response.AsText("CSRF Token is invalid.").WithStatusCode(403);
                }
                var info = new InfoModel
                {
                    Name = Request.Form.name,
                    Email = Request.Form.email,
                    Location = Request.Form.location,
                    Text = Request.Form.text
                };
                if (info.Validate())
                {
                    Ctx.FeedbackManager.Save(new Feedback
                    {
                        Name = info.Name,
                        Email = info.Email,
                        Location = info.Location,
                        Text = info.Text,
                        Created = DateTime.Now
                    });

                    return Response.AsRedirect("/Info/Success");
                }

                return View["info.cshtml", info];
            };
        }
    }
}