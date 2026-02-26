using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Http
{
    /// <summary>
    /// DelegatingHandler para capturar informações de requisições HTTP outbound.
    /// </summary>
    public sealed class CustomLoggerHttpHandler : DelegatingHandler
    {
        private readonly ILogger _logger;

        public CustomLoggerHttpHandler(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            HttpResponseMessage response = null;
            Exception capturedException = null;
            int statusCode = 500;

            try
            {
                response = await base.SendAsync(request, cancellationToken);
                statusCode = (int)response.StatusCode;
                return response;
            }
            catch (Exception ex)
            {
                capturedException = ex;
                statusCode = 500;
                throw; // Re-lança exceção (não engole)
            }
            finally
            {
                stopwatch.Stop();

                // Captura informações HTTP
                var method = request.Method.Method;
                var path = request.RequestUri?.AbsolutePath ?? "/";
                var durationMs = stopwatch.ElapsedMilliseconds;

                // Tenta obter Client IP via headers comuns (proxy/load balancer)
                var clientIp = GetClientIpAddress(request);

                // Server IP não disponível sem HttpContext (deixar null)
                string serverIp = null;

                // Determina LogLevel
                var logLevel = DetermineLogLevel(statusCode, capturedException);

                // Mensagem padrão
                var message = $"HTTP {method} {path} responded {statusCode} in {durationMs}ms";

                // Cria estado com propriedades HTTP
                var state = new Dictionary<string, object>
                {
                    ["HttpMethod"] = method,
                    ["HttpPath"] = path,
                    ["HttpStatusCode"] = statusCode,
                    ["HttpDurationMs"] = durationMs,
                    ["ClientIpAddress"] = clientIp,
                    ["ServerIpAddress"] = serverIp
                };

                using (_logger.BeginScope(state))
                {
                    // Log (ILogger do Microsoft.Extensions.Logging)
                    _logger.Log(logLevel, capturedException, message, state);
                };
            }
        }

        private static string GetClientIpAddress(HttpRequestMessage request)
        {
            if (request?.Headers == null)
                return null;

            try
            {
                // Tenta obter IP via headers comuns de proxy/load balancer
                // X-Forwarded-For: padrão de proxies (pode conter múltiplos IPs)
                if (request.Headers.TryGetValues("X-Forwarded-For", out var forwardedFor))
                {
                    var ips = forwardedFor.FirstOrDefault()?.Split(',');
                    if (ips != null && ips.Length > 0)
                    {
                        return ips[0].Trim(); // Primeiro IP = cliente original
                    }
                }

                // X-Real-IP: alternativa usada por alguns proxies (NGINX)
                if (request.Headers.TryGetValues("X-Real-IP", out var realIp))
                {
                    return realIp.FirstOrDefault()?.Trim();
                }

                // CF-Connecting-IP: usado por Cloudflare
                if (request.Headers.TryGetValues("CF-Connecting-IP", out var cfIp))
                {
                    return cfIp.FirstOrDefault()?.Trim();
                }

                // True-Client-IP: usado por Akamai
                if (request.Headers.TryGetValues("True-Client-IP", out var trueClientIp))
                {
                    return trueClientIp.FirstOrDefault()?.Trim();
                }
            }
            catch
            {
                // Falha ao extrair IP, retorna null
            }

            return null;
        }

        private static LogLevel DetermineLogLevel(int statusCode, Exception exception)
        {
            if (exception != null)
                return LogLevel.Error;

            if (statusCode >= 500)
                return LogLevel.Error;

            if (statusCode >= 400)
                return LogLevel.Warning;

            return LogLevel.Information;
        }
    }
}