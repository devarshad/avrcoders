using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace AVR
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            BundleTable.EnableOptimizations = true;
            log4net.Config.XmlConfigurator.Configure(new System.IO.FileInfo(Server.MapPath("~/Log4Net.config")));
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = Server.GetLastError();
            Response.Clear();

            HttpException httpException = exception as HttpException;

            string _action = string.Empty;

            if (httpException != null)
            {
                switch (httpException.GetHttpCode())
                {
                    case 400:
                        _action = "BadRequest";
                        break;

                    case 401:
                        _action = "Unauthorized";
                        break;

                    case 403:
                        _action = "Forbidden";
                        break;

                    case 404:
                        _action = "PageNotFound";
                        break;

                    case 500:
                        _action = "ServerError";
                        break;

                    default:
                        _action = "Index";
                        break;
                }
            }
            else
            {
                _action = "Index";
            }
            // clear error on server
            Server.ClearError();

            exception.HelpLink = "It can be due to wrong invalid view to render or invalid razor syntax or accessing invalid/inaccessible model or propoerty.";
            exception.Data.Add("Location : ", "Exception occured while rendering view.");
            exception.Data.Add("Applpication Tier : ", "1. AVR App");
            exception.Data.Add("Class : ", "Global.asax");
            exception.Data.Add("Method : ", "Application_Error");
            Logs.LogError("Error at Application error", exception);

            Response.Redirect(String.Format("~/Error/{0}/?message={1}", _action, ""));
        }
    }
}
