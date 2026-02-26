using Microsoft.Extensions.DependencyInjection;

namespace CustomLogger.Http
{
    /// <summary>
    /// Métodos de extensão para registrar o CustomLoggerHttpHandler.
    /// </summary>
    public static class CustomLoggerHttpHandlerExtensions
    {
        /// <summary>
        /// Adiciona o CustomLoggerHttpHandler ao pipeline do HttpClient.
        /// </summary>
        /// <param name="builder">HttpClient builder</param>
        /// <returns>HttpClient builder para encadeamento</returns>
        public static IHttpClientBuilder AddCustomLogger(this IHttpClientBuilder builder)
        {
            return builder.AddHttpMessageHandler<CustomLoggerHttpHandler>();
        }
    }
}