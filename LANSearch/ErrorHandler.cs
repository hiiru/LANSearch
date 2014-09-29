using LANSearch.Data.User;
using LANSearch.Models;
using Nancy;
using Nancy.ErrorHandling;
using Nancy.Security;
using Nancy.ViewEngines;

namespace LANSearch
{
    public class ErrorHandler : IStatusCodeHandler
    {
        private readonly IViewRenderer viewRenderer;

        public ErrorHandler(IViewRenderer viewRenderer)
        {
            this.viewRenderer = viewRenderer;
        }

        public bool HandlesStatusCode(HttpStatusCode statusCode, NancyContext context)
        {
            switch (statusCode)
            {
                case HttpStatusCode.NotFound:
                case HttpStatusCode.InternalServerError:
                case HttpStatusCode.NotImplemented:
                    return true;

                default:
                    return false;
            }
        }

        public void Handle(HttpStatusCode statusCode, NancyContext context)
        {
            if (InitConfig.SetupIps.Contains(context.Request.UserHostAddress) || context.CurrentUser.HasClaim(UserRoles.ADMIN))
            {
                return;
            }
            var model = new ErrorModel();
            switch (statusCode)
            {
                case HttpStatusCode.NotFound:
                    model.StatusCode = 404;
                    model.ErrorType = "info";
                    model.ErrorTitle = "File not Found";
                    model.ErrorMessage = string.Format("The requested page {0} could not be found.", context.Request.Url.Path);
                    break;

                default:
                    model.StatusCode = 500;
                    model.ErrorType = "danger";
                    model.ErrorTitle = "Unexpected problem ocured.";
                    model.ErrorMessage = "There was an unexpected error, please try again.<br>If the Problem persists, please use the feedback form to inform me or inform the admin directly.";
                    break;
            }

            var response = viewRenderer.RenderView(context, "Views/Error.cshtml", model);
            response.StatusCode = statusCode;
            context.Response = response;
        }
    }
}