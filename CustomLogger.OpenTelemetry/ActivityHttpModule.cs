using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;


namespace CustomLogger.OpenTelemetry
{
    /// <summary>
    /// HttpModule para criar Activities automaticamente em requisições ASP.NET Framework.
    /// Opcional - registrar no web.config apenas se necessário.
    /// </summary>
    public sealed class ActivityHttpModule : System.Web.WebPages.
    {
        public void Init(HttpApplication context)
        {
            if (context == null)
                return;

            context.BeginRequest += OnBeginRequest;
            context.EndRequest += OnEndRequest;
        }

        private void OnBeginRequest(object sender, EventArgs e)
        {
            var context = ((HttpApplication)sender).Context;

            var activityName = $"HTTP {context.Request.HttpMethod} {context.Request.Path}";
            var activity = LoggerActivitySource.Source.StartActivity(activityName);

            if (activity != null)
            {
                activity.SetTag("http.method", context.Request.HttpMethod);
                activity.SetTag("http.url", context.Request.Url?.ToString());
                activity.SetTag("http.target", context.Request.Path);
            }
        }

        private void OnEndRequest(object sender, EventArgs e)
        {
            var context = ((HttpApplication)sender).Context;
            var activity = Activity.Current;

            if (activity != null)
            {
                activity.SetTag("http.status_code", context.Response.StatusCode);
                activity.Stop();
            }
        }

        public void Dispose()
        {
            // Nada a liberar
        }
    }
}
