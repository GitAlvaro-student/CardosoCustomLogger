using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace CustomLogger.AspNetCore
{
    public sealed class CustomLoggerHttpMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;

        public CustomLoggerHttpMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next ?? throw new ArgumentNullException(nameof(next));
            _logger = loggerFactory?.CreateLogger("CustomLogger.AspNetCore.HttpRequest")
                ?? throw new ArgumentNullException(nameof(loggerFactory));
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            Exception capturedException = null;
            int statusCode = 200;

            try
            {
                await _next(context);
                statusCode = context.Response.StatusCode;
            }
            catch (Exception ex)
            {
                capturedException = ex;
                statusCode = 500;
                throw;
            }
            finally
            {
                stopwatch.Stop();

                var method = context.Request.Method;
                var path = context.Request.Path.ToString();
                var durationMs = stopwatch.ElapsedMilliseconds;
                var clientIp = context.Connection.RemoteIpAddress?.ToString();
                var serverIp = context.Connection.LocalIpAddress?.ToString();
                var logLevel = DetermineLogLevel(statusCode, capturedException);
                var message = $"HTTP {method} {path} responded {statusCode} in {durationMs}ms";

                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["HttpMethod"] = method,
                    ["HttpPath"] = path,
                    ["HttpStatusCode"] = statusCode,
                    ["HttpDurationMs"] = durationMs,
                    ["ClientIpAddress"] = clientIp,
                    ["ServerIpAddress"] = serverIp
                }))
                {
                    _logger.Log(logLevel, capturedException, message);
                }
            }
        }

        private static LogLevel DetermineLogLevel(int statusCode, Exception exception)
        {
            if (exception != null) return LogLevel.Error;
            if (statusCode >= 500) return LogLevel.Error;
            if (statusCode >= 400) return LogLevel.Warning;
            return LogLevel.Information;
        }
    }
}