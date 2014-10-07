using LANSearch.Data.User;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Conventions;
using Nancy.Diagnostics;
using Nancy.TinyIoc;
using SquishIt.Framework;

namespace LANSearch
{
    public class Bootstrapper : DefaultNancyBootstrapper
    {
        protected AppContext Ctx { get { return AppContext.GetContext(); } }

        protected override void ConfigureRequestContainer(TinyIoCContainer container, NancyContext context)
        {
            base.ConfigureRequestContainer(container, context);
            container.Register<IUserMapper, UserMapper>();
        }

        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            var formsAuthenticationConfiguration = AuthenticationConfiguration.GetContext().FormsAuthenticationConfiguration;
            formsAuthenticationConfiguration.UserMapper = container.Resolve<IUserMapper>();
            FormsAuthentication.Enable(pipelines, formsAuthenticationConfiguration);
            StaticConfiguration.DisableErrorTraces = false;
            Nancy.Security.Csrf.Enable(pipelines);

            Bundle.Css()
                .Add("~/Content/bootstrap.css")
                .Add("~/Content/bootstrap-theme.css")
                .Add("~/Content/bootstrap-dialog.css")
                .Add("~/Content/lansearch.css")
                .ForceRelease()
                .AsCached("css", "/static/css");

            Bundle.JavaScript()
                .Add("~/Scripts/jquery-1.9.0.js")
                .Add("~/Scripts/jquery.signalR-2.1.2.js")
                .Add("~/Scripts/bootstrap.js")
                .Add("~/Scripts/bootstrap-dialog.js")
                .Add("~/Scripts/lansearch.js")
                .ForceRelease()
                .AsCached("js", "/static/js");
        }

        protected override DiagnosticsConfiguration DiagnosticsConfiguration
        {
            get
            {
                return new DiagnosticsConfiguration
                {
                    Password = Ctx.Config.NancyDiagnosticsPassword,
                    Path = "/Admin/Diagnostics",
                };
            }
        }

        protected override void ConfigureConventions(NancyConventions nancyConventions)
        {
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/Scripts", "Scripts"));
            nancyConventions.StaticContentsConventions.Add(StaticContentConventionBuilder.AddDirectory("/Fonts", "Fonts"));
            base.ConfigureConventions(nancyConventions);
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            pipelines.AfterRequest += (ctx) =>
            {
                ctx.Response.Headers["Server"] = "LANSearch";
            };
            base.RequestStartup(container, pipelines, context);
        }
    }
}