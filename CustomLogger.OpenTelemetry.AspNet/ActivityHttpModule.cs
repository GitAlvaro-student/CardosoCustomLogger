using System.Diagnostics;
using System.Web;

namespace CustomLogger.OpenTelemetry.AspNet
{
    public sealed class ActivityHttpModule : IHttpModule
    {
        public void Init(HttpApplication context)
        {
            context.BeginRequest += (sender, args) =>
            {
                var request = context.Context.Request;

                var activity = LoggerActivitySource.Source
                    .StartActivity($"HTTP {request.HttpMethod}");

                activity?.SetTag("http.method", request.HttpMethod);
                activity?.SetTag("http.url", request.Url?.ToString());
            };

            context.EndRequest += (sender, args) =>
            {
                Activity.Current?.Stop();
            };
        }

        public void Dispose()
        {
        }
    }
}
