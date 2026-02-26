using CustomLogger.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace PaymentAPI
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services
            var logger = WebApiApplication.Global.LoggerFactory
                .CreateLogger("CustomLogger.WebApi.Request");

            config.MessageHandlers.Add(new CustomLoggerHttpHandler(logger));
            // Web API routes
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
