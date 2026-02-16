using CustomLogger.Abstractions;
using CustomLogger.HealthChecks;
using CustomLogger.HealthChecks.Models;
using CustomLogger.OpenTelemetry;
using CustomLogger.Providers;
using Microsoft.Extensions.Logging;
using PaymentAPI.Services;
using System;
using System.Web.Http;
using Unity;
using Unity.AspNet.WebApi;

namespace PaymentAPI
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        private static LoggingHealthMonitor _healthMonitor;

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);

            // Configurar Unity Container para DI
            var container = new UnityContainer();
            container.RegisterType<IPaymentService, PaymentService>();
            GlobalConfiguration.Configuration.DependencyResolver = new UnityDependencyResolver(container);

            // Habilitar CORS
            GlobalConfiguration.Configuration.EnableCors();

            //// Logging
            //var provider = InitializeCustomLogger();

            // OpenTelemetry
            OpenTelemetryBootstrapper.InitializeFromConfig();

            //// Inicializar health monitor
            //var evaluator = new DefaultLoggingHealthEvaluator();
            //_healthMonitor = new LoggingHealthMonitor(
            //    evaluator: evaluator,
            //    healthState: provider,
            //    evaluationIntervalSeconds: 60
            //);
        }

        protected void Application_End()
        {
            OpenTelemetryBootstrapper.Shutdown();
            _healthMonitor?.Dispose();
        }

        public static class Global
        {
            public static ILoggerFactory LoggerFactory { get; set; }
        }

        public static LoggingHealthReport GetCurrentHealth()
        {
            return _healthMonitor?.GetLatestReport()
                ?? LoggingHealthReport.CreateUnknown("Monitor not initialized");
        }

        //private ILoggingHealthState InitializeCustomLogger()
        //{
        //    Global.LoggerFactory = LoggerFactory.Create(builder => builder.AddCustomLogging());
        //    return Global.LoggerFactory;
        //}
    }
}

