using Microsoft.Extensions.Logging;
using PaymentAPI.Services;
using System.Web.Http;
using Unity;
using Unity.AspNet.WebApi;
using CustomLogger.Providers;

namespace PaymentAPI
{
    public class WebApiApplication : System.Web.HttpApplication
    { 
        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            // Configurar Unity Container para DI
            var container = new UnityContainer();
            container.RegisterType<IPaymentService, PaymentService>();
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);

            // Habilitar CORS
            GlobalConfiguration.Configuration.EnableCors();

            // Logging
            Global.LoggerFactory = LoggerFactory.Create(builder => builder.AddCustomLogging());
        }

        public static class Global
        {
            public static ILoggerFactory LoggerFactory { get; set; }
        }
    }
}

