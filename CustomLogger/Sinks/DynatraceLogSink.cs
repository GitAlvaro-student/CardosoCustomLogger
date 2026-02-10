using CustomLogger.Abstractions;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Sinks
{
    public sealed class DynatraceLogSink : ILogSink//, IAsyncLogSink, IBatchLogSink, IAsyncBatchLogSink, IDisposable
    {
        private readonly ILogFormatter _formatter;
        private readonly string _endpoint;
        private readonly string _apiToken;
        private readonly HttpClient _httpClient;
        private readonly bool _shouldDisposeClient;

        public DynatraceLogSink(string endpoint, string apiToken, HttpClient httpClient, ILogFormatter formatter)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            _apiToken = apiToken ?? throw new ArgumentNullException(nameof(apiToken));
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _httpClient = httpClient ?? CreateDefaultHttpClient();
            _shouldDisposeClient = httpClient == null;
        }

        private static HttpClient CreateDefaultHttpClient()
        {
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(3)
            };
        }

        public void Write(ILogEntry entry)
        {
            // Validação mínima - falha silenciosa se não há State
            if (entry?.State == null)
            {
                return;
            }
            var json = _formatter.Format(entry);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            // Criar requisição
            var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            request.Headers.Add("Authorization", $"Api-Token {_apiToken}");

            try
            {
                // Iniciar envio e observar apenas falhas IMEDIATAS
                var sendTask = _httpClient.SendAsync(request);

                // Configurar continuação para observar falhas sem bloqueio
                // Mas apenas para evitar exceções não observadas
                sendTask.ContinueWith(
                    t => { var _ = t.Exception; }, // Observar exceção sem propagar
                    TaskContinuationOptions.OnlyOnFaulted |
                    TaskContinuationOptions.ExecuteSynchronously
                );


            }
            catch (HttpRequestException)
            {
                // Exceção síncrona do SendAsync (endpoint inválido, rede, etc.)
                // Deve ser lançada para DegradableLogSink marcar degradação
                throw;
            }
            catch (TaskCanceledException)
            {
                // Timeout síncrono do HttpClient
                throw;
            }
            catch (InvalidOperationException)
            {
                // HttpClient já disposado, endpoint vazio, etc.
                throw;
            }
            catch (Exception)
            {
                // Qualquer outra exceção síncrona
                // Não engolir - lançar para degradação
                throw;
            }
            finally
            {
                // Liberar recursos da requisição
                //request.Dispose();
            }
        }

        //// Espelho assíncrono de Write - mesma semântica, versão async
        //public Task WriteAsync(ILogEntry entry, CancellationToken cancellationToken = default)
        //{
        //    if (entry?.State == null)
        //        return Task.CompletedTask;

        //    var json = _formatter.Format(entry);
        //    if (string.IsNullOrWhiteSpace(json))
        //        return Task.CompletedTask;

        //    var request = new HttpRequestMessage(HttpMethod.Post, _endpoint)
        //    {
        //        Content = new StringContent(json, Encoding.UTF8, "application/json")
        //    };
        //    request.Headers.Add("Authorization", $"Api-Token {_apiToken}");

        //    var sendTask = _httpClient.SendAsync(request, cancellationToken);

        //    sendTask.ContinueWith(
        //        t => { var _ = t.Exception; },
        //        TaskContinuationOptions.OnlyOnFaulted |
        //        TaskContinuationOptions.ExecuteSynchronously
        //    );

        //    return Task.CompletedTask;
        //}


        //// Iteração simples sobre entries - não é batch HTTP real
        //public void WriteBatch(IEnumerable<ILogEntry> entries)
        //{
        //    if (entries == null)
        //        return;

        //    // Chama Write para cada entry sequencialmente
        //    foreach (var entry in entries)
        //    {
        //        Write(entry);
        //    }
        //}

        //// Espelho assíncrono de WriteBatch - iteração sequencial
        //public async Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default)
        //{
        //    if (entries == null)
        //        return;

        //    // Chama WriteAsync para cada entry sequencialmente
        //    foreach (var entry in entries)
        //    {
        //        await WriteAsync(entry, cancellationToken);
        //    }
        //}

        public void Dispose()
        {
            if (_shouldDisposeClient)
            {
                _httpClient?.Dispose();
            }
        }
    }
}