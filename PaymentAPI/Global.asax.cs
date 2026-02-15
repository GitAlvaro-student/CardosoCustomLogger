using CustomLogger.OpenTelemetry;
using CustomLogger.Providers;
using Microsoft.Extensions.Logging;
using PaymentAPI.Services;
using System.Web.Http;
using Unity;
using Unity.AspNet.WebApi;

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

            // OpenTelemetry
            OpenTelemetryBootstrapper.InitializeFromConfig();
        }

        protected void Application_End()
        {
            OpenTelemetryBootstrapper.Shutdown();
        }

        public static class Global
        {
            public static ILoggerFactory LoggerFactory { get; set; }
        }
    }
}

