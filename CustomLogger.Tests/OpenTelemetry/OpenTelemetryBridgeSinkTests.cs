using CustomLogger.Abstractions;
using CustomLogger.Buffering;
using CustomLogger.OpenTelemetry;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CustomLogger.Tests.OpenTelemetry
{
    public sealed class OpenTelemetryBridgeSinkTests : IDisposable
    {
        private readonly ActivityListener _listener;

        public OpenTelemetryBridgeSinkTests()
        {
            // Garante que não há Activity ativa no início de cada teste
            Activity.Current?.Stop();

            // CORREÇÃO: Cria listener para que StartActivity não retorne null
            _listener = new ActivityListener
            {
                ShouldListenTo = source => source.Name == "MyLogger",
                Sample = (ref ActivityCreationOptions<ActivityContext> options) => ActivitySamplingResult.AllData
            };
            ActivitySource.AddActivityListener(_listener);
        }

        public void Dispose()
        {
            // Limpa Activity após cada teste (isolamento)
            Activity.Current?.Stop();
            _listener?.Dispose();
        }

        // ────────────────────────────────────────
        // Teste 1: Bridge adiciona evento na Activity
        // ────────────────────────────────────────

        [Fact]
        public void Write_ComActivityAtiva_AdicionaEvento()
        {
            // Arrange
            var sink = new OpenTelemetryBridgeSink();
            var entry = CriarLogEntry("Mensagem de teste");

            using var activity = LoggerActivitySource.Source.StartActivity("TestOperation");
            Assert.NotNull(activity); // Agora não será null

            // Act
            sink.Write(entry);

            // Assert
            var eventos = activity.Events.ToList();
            Assert.Single(eventos);
            Assert.Equal("Mensagem de teste", eventos[0].Name);
        }

        [Fact]
        public void Write_ComActivityAtiva_EventoContemNomeCorreto()
        {
            // Arrange
            var sink = new OpenTelemetryBridgeSink();
            var entry = CriarLogEntry("Log de processamento");

            using var activity = LoggerActivitySource.Source.StartActivity("Process");
            Assert.NotNull(activity);

            // Act
            sink.Write(entry);

            // Assert
            var evento = activity.Events.First();
            Assert.Equal("Log de processamento", evento.Name);
        }

        [Fact]
        public void Write_MultiplosLogs_AdicionaMultiplosEventos()
        {
            // Arrange
            var sink = new OpenTelemetryBridgeSink();

            using var activity = LoggerActivitySource.Source.StartActivity("MultiLog");
            Assert.NotNull(activity);

            // Act
            sink.Write(CriarLogEntry("Log 1"));
            sink.Write(CriarLogEntry("Log 2"));
            sink.Write(CriarLogEntry("Log 3"));

            // Assert
            var eventos = activity.Events.ToList();
            Assert.Equal(3, eventos.Count);
            Assert.Equal("Log 1", eventos[0].Name);
            Assert.Equal("Log 2", eventos[1].Name);
            Assert.Equal("Log 3", eventos[2].Name);
        }

        // ────────────────────────────────────────
        // Teste 2: Funciona sem Activity
        // ────────────────────────────────────────

        [Fact]
        public void Write_SemActivity_NaoLancaExcecao()
        {
            // Arrange
            var sink = new OpenTelemetryBridgeSink();
            var entry = CriarLogEntry("Log sem activity");

            // Garante que não há Activity
            Assert.Null(Activity.Current);

            // Act & Assert (não deve lançar exceção)
            var exception = Record.Exception(() => sink.Write(entry));
            Assert.Null(exception);
        }

        [Fact]
        public void Write_SemActivity_NaoAlteraEstadoGlobal()
        {
            // Arrange
            var sink = new OpenTelemetryBridgeSink();
            var entry = CriarLogEntry("Log isolado");

            // Garante que não há Activity
            Assert.Null(Activity.Current);

            // Act
            sink.Write(entry);

            // Assert - Activity.Current continua null
            Assert.Null(Activity.Current);
        }

        [Fact]
        public void Write_SemActivity_MultiplasChamas_NaoLancaExcecao()
        {
            // Arrange
            var sink = new OpenTelemetryBridgeSink();

            Assert.Null(Activity.Current);

            // Act & Assert
            var exception = Record.Exception(() =>
            {
                sink.Write(CriarLogEntry("Log 1"));
                sink.Write(CriarLogEntry("Log 2"));
                sink.Write(CriarLogEntry("Log 3"));
            });

            Assert.Null(exception);
        }

        // ────────────────────────────────────────
        // Teste 3: Funciona sem configuração OpenTelemetry
        // ────────────────────────────────────────

        [Fact]
        public void Write_SemBootstrapper_FuncionaIsoladamente()
        {
            // Arrange
            // NÃO inicializa Bootstrapper
            // NÃO configura DI
            var sink = new OpenTelemetryBridgeSink();
            var entry = CriarLogEntry("Log sem bootstrap");

            using var activity = LoggerActivitySource.Source.StartActivity("TestNoBootstrap");
            Assert.NotNull(activity);

            // Act & Assert (não deve lançar exceção)
            var exception = Record.Exception(() => sink.Write(entry));
            Assert.Null(exception);

            // Valida que evento foi adicionado mesmo sem bootstrap
            Assert.Single(activity.Events);
        }

        [Fact]
        public void Write_InstanciaSinkDiretamente_Funciona()
        {
            // Arrange - instancia diretamente sem DI
            var sink = new OpenTelemetryBridgeSink();

            using var activity = LoggerActivitySource.Source.StartActivity("DirectInstantiation");
            Assert.NotNull(activity);

            // Act
            sink.Write(CriarLogEntry("Teste instância direta"));

            // Assert
            Assert.Single(activity.Events);
        }

        // ────────────────────────────────────────
        // Teste 4: Compatível com múltiplos sinks
        // ────────────────────────────────────────

        [Fact]
        public void Write_ComMultiplosSinks_AmbosRecebemLog()
        {
            // Arrange
            var bridgeSink = new OpenTelemetryBridgeSink();
            var inMemorySink = new InMemorySink();
            var entry = CriarLogEntry("Log para múltiplos sinks");

            using var activity = LoggerActivitySource.Source.StartActivity("MultiSink");
            Assert.NotNull(activity);

            // Act
            bridgeSink.Write(entry);
            inMemorySink.Write(entry);

            // Assert
            // Bridge adicionou evento
            Assert.Single(activity.Events);
            Assert.Equal("Log para múltiplos sinks", activity.Events.First().Name);

            // InMemory capturou log
            Assert.Single(inMemorySink.Logs);
            Assert.Equal("Log para múltiplos sinks", inMemorySink.Logs[0].Message);
        }

        [Fact]
        public void Write_BridgeNaoInterfereEmOutroSink()
        {
            // Arrange
            var bridgeSink = new OpenTelemetryBridgeSink();
            var inMemorySink = new InMemorySink();
            var entry = CriarLogEntry("Teste isolamento");

            using var activity = LoggerActivitySource.Source.StartActivity("Isolation");
            Assert.NotNull(activity);

            // Act
            bridgeSink.Write(entry);
            inMemorySink.Write(entry);

            // Assert
            // InMemory deve ter exatamente 1 log (Bridge não duplica)
            Assert.Single(inMemorySink.Logs);

            // Activity deve ter exatamente 1 evento (InMemory não duplica)
            Assert.Single(activity.Events);
        }

        [Fact]
        public void Write_OrdemDeExecucao_NaoAlteraComportamento()
        {
            // Arrange
            var bridgeSink = new OpenTelemetryBridgeSink();
            var inMemorySink = new InMemorySink();
            var entry1 = CriarLogEntry("Teste ordem 1");
            var entry2 = CriarLogEntry("Teste ordem 2");

            using var activity = LoggerActivitySource.Source.StartActivity("Order");
            Assert.NotNull(activity);

            // Act - ordem 1: Bridge primeiro
            bridgeSink.Write(entry1);
            inMemorySink.Write(entry1);

            var eventosAposOrdem1 = activity.Events.Count();
            var logsAposOrdem1 = inMemorySink.Logs.Count;

            // Act - ordem 2: InMemory primeiro (mesmo teste, entrada diferente)
            inMemorySink.Write(entry2);
            bridgeSink.Write(entry2);

            // Assert - resultados devem ser consistentes
            Assert.Equal(2, activity.Events.Count());
            Assert.Equal(2, inMemorySink.Logs.Count);
        }

        // ────────────────────────────────────────
        // Helper Methods
        // ────────────────────────────────────────

        private static ILogEntry CriarLogEntry(string message)
        {
            return new BufferedLogEntry(
                timestamp: DateTimeOffset.UtcNow,
                category: "TestCategory",
                logLevel: LogLevel.Information,
                eventId: new EventId(1, "TestEvent"),
                message: message,
                exception: null,
                state: null,
                scopes: new Dictionary<string, object>()
            );
        }

        // ────────────────────────────────────────
        // Sink Fake para Testes
        // ────────────────────────────────────────

        private sealed class InMemorySink : ILogSink
        {
            public List<ILogEntry> Logs { get; } = new List<ILogEntry>();

            public void Write(ILogEntry entry)
            {
                Logs.Add(entry);
            }

            public void Clear()
            {
                Logs.Clear();
            }
        }
    }
}
