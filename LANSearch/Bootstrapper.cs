﻿using LANSearch.Data.User;
using Nancy;
using Nancy.Authentication.Forms;
using Nancy.Bootstrapper;
using Nancy.Diagnostics;
using Nancy.TinyIoc;

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
    }
}