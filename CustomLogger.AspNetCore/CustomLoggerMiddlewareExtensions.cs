using Microsoft.AspNetCore.Builder;

namespace CustomLogger.AspNetCore
{
    /// <summary>
    /// Métodos de extensão para registrar o middleware CustomLogger.
    /// </summary>
    public static class CustomLoggerMiddlewareExtensions
    {
        /// <summary>
        /// Adiciona o middleware CustomLogger HTTP ao pipeline de requisição.
        /// IMPORTANTE: Deve ser chamado ANTES de UseRouting() para capturar todas as requisições.
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <returns>Application builder para encadeamento</returns>
        public static IApplicationBuilder UseCustomLoggerHttp(this IApplicationBuilder app)
        {
            return app.UseMiddleware<CustomLoggerHttpMiddleware>();
        }

        /// <summary>
        /// Adiciona o middleware CustomLogger HTTP ao pipeline com configuração customizada.
        /// </summary>
        /// <param name="app">Application builder</param>
        /// <param name="serviceName">Nome do serviço (sobrescreve configuração padrão)</param>
        /// <param name="environment">Ambiente (sobrescreve configuração padrão)</param>
        /// <returns>Application builder para encadeamento</returns>
        public static IApplicationBuilder UseCustomLoggerHttp(
            this IApplicationBuilder app,
            string serviceName,
            string environment)
        {
            return app.UseMiddleware<CustomLoggerHttpMiddleware>(serviceName, environment);
        }
    }
}