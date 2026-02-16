using CustomLogger.Abstractions;
using CustomLogger.HealthChecks.Abstractions;
using CustomLogger.HealthChecks.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CustomLogger.HealthChecks
{
    /// <summary>
    /// Monitor de saúde do logger com execução periódica.
    /// 
    /// PROPÓSITO:
    /// - Executar avaliação de saúde em intervalo regular
    /// - Armazenar último relatório para consulta
    /// - Compatível com .NET Framework 4.6.1+
    /// 
    /// USO TÍPICO:
    /// - Aplicações .NET Framework
    /// - WebApi sem ASP.NET Core
    /// - WCF Services
    /// - Windows Services
    /// 
    /// IMPORTANTE:
    /// Não usa BackgroundService ou HostedService (ASP.NET Core).
    /// Usa apenas System.Threading.Timer (compatível com .NET Framework).
    /// </summary>
    public sealed class LoggingHealthMonitor : IDisposable
    {
        private readonly ILoggingHealthEvaluator _evaluator;
        private readonly ILoggingHealthState _healthState;
        private readonly Timer _timer;
        private readonly object _lock = new object();

        private LoggingHealthReport _latestReport;
        private bool _disposed;

        /// <summary>
        /// Construtor com intervalo de avaliação configurável.
        /// </summary>
        /// <param name="evaluator">Avaliador de saúde</param>
        /// <param name="healthState">Estado do logger</param>
        /// <param name="evaluationIntervalSeconds">Intervalo entre avaliações (padrão: 60s)</param>
        /// <exception cref="ArgumentNullException">Se evaluator ou healthState forem null</exception>
        /// <exception cref="ArgumentOutOfRangeException">Se intervalo for &lt;= 0</exception>
        public LoggingHealthMonitor(
            ILoggingHealthEvaluator evaluator,
            ILoggingHealthState healthState,
            int evaluationIntervalSeconds = 60)
        {
            if (evaluationIntervalSeconds <= 0)
                throw new ArgumentOutOfRangeException(nameof(evaluationIntervalSeconds), "Interval must be > 0");

            _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
            _healthState = healthState ?? throw new ArgumentNullException(nameof(healthState));

            // Executar avaliação inicial
            EvaluateHealth(null);

            // Configurar timer para execução periódica
            var intervalMs = evaluationIntervalSeconds * 1000;
            _timer = new Timer(EvaluateHealth, null, intervalMs, intervalMs);
        }

        /// <summary>
        /// Obtém o último relatório de saúde disponível.
        /// 
        /// THREAD-SAFETY:
        /// Método thread-safe, pode ser chamado de múltiplas threads.
        /// 
        /// RETORNO:
        /// Nunca null. Se avaliação ainda não ocorreu ou falhou,
        /// retorna relatório Unknown.
        /// </summary>
        public LoggingHealthReport GetLatestReport()
        {
            lock (_lock)
            {
                return _latestReport ?? LoggingHealthReport.CreateUnknown("No evaluation performed yet");
            }
        }

        /// <summary>
        /// Força execução imediata de avaliação de saúde.
        /// 
        /// USO:
        /// Para obter relatório atualizado sob demanda,
        /// sem esperar próximo ciclo do timer.
        /// </summary>
        public void ForceEvaluation()
        {
            EvaluateHealth(null);
        }

        /// <summary>
        /// Callback executado pelo timer.
        /// 
        /// PROTEÇÃO:
        /// Nunca lança exceção, armazena Unknown em caso de falha.
        /// </summary>
        private void EvaluateHealth(object state)
        {
            try
            {
                var report = _evaluator.Evaluate(_healthState);

                lock (_lock)
                {
                    _latestReport = report;
                }
            }
            catch (Exception ex)
            {
                // Fallback defensivo
                lock (_lock)
                {
                    _latestReport = LoggingHealthReport.CreateUnknown(
                        $"Health evaluation failed: {ex.Message}"
                    );
                }
            }
        }

        /// <summary>
        /// Libera recursos (timer).
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _timer?.Dispose();
            _disposed = true;
        }
    }
}
