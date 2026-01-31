using CustomLogger.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CustomLogger.Sinks
{
    /// <summary>
    /// Sink simples para escrita de logs no console.
    /// Usado apenas para validação do pipeline.
    /// </summary>
    public sealed class ConsoleLogSink : IAsyncBatchLogSink, IDisposable
    {
        private readonly ILogFormatter _formatter;

        public ConsoleLogSink(ILogFormatter formatter)
        {
            _formatter = formatter;
        }

        public void Write(ILogEntry entry)
        {
            if (entry == null)
                return;

            try
            {
                // ✅ MODIFICADO: Usar ConsoleColorManager para cores
                if (ConsoleColorManager.IsOutputToRealConsole())
                {
                    string formattedMessage = _formatter.Format(entry);
                    ConsoleColorManager.WriteColoredLine(formattedMessage, entry.LogLevel);
                }
                else
                {
                    // ✅ COMPATIBILIDADE: Se output redirecionado, comportamento original
                    Console.WriteLine(_formatter.Format(entry));
                }
            }
            catch
            {
                // Absorve falha (mantido do código original)
            }
        }

        // ✅ NOVO: Escrita em lote
        public void WriteBatch(IEnumerable<ILogEntry> entries)
        {
            if (entries == null)
                return;

            try
            {
                bool isRealConsole = ConsoleColorManager.IsOutputToRealConsole();

                foreach (var entry in entries)
                {
                    if (isRealConsole)
                    {
                        // ✅ Cada entrada com sua própria cor baseada no LogLevel
                        string formattedMessage = _formatter.Format(entry);
                        ConsoleColorManager.WriteColoredLine(formattedMessage, entry.LogLevel);
                    }
                    else
                    {
                        // ✅ COMPATIBILIDADE: Comportamento original
                        Console.WriteLine(_formatter.Format(entry));
                    }
                }

                // ✅ Mantém o flush único (performance)
                Console.Out.Flush();
            }
            catch
            {
                // Absorve falha (mantido do código original)
            }
        }

        // ✅ NOVO: Write assíncrono
        public async Task WriteAsync(ILogEntry entry, CancellationToken cancellationToken = default)
        {
            if (entry == null)
                return;

            try
            {
                await Console.Out.WriteLineAsync(_formatter.Format(entry));
            }
            catch
            {
                // Absorve falha
            }
        }

        // ✅ NOVO: WriteBatch assíncrono
        public async Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default)
        {
            if (entries == null)
                return;

            try
            {
                foreach (var entry in entries)
                {
                    await Console.Out.WriteLineAsync(_formatter.Format(entry));
                }
                await Console.Out.FlushAsync();
            }
            catch
            {
                // Absorve falha
            }
        }

        public void Dispose()
        {
            try
            {
                Console.Out.Flush();
            }
            catch
            {
                // Absorve falha
            }
        }

        #region ConsoleColorManager
        private static class ConsoleColorManager
        {
            // Tabela fixa de cores (hardcoded conforme regras)
            private static readonly ConsoleColor[] _logLevelColors = new ConsoleColor[]
            {
                ConsoleColor.DarkGray,    // Trace
                ConsoleColor.Gray,        // Debug
                ConsoleColor.Green,       // Information
                ConsoleColor.Yellow,      // Warning
                ConsoleColor.Red,         // Error
                ConsoleColor.DarkRed,     // Critical
                ConsoleColor.White        // Default (None)
            };

            // Cache de strings ANSI para melhor performance
            private static readonly string[] _ansiColorCodes;
            private const string ResetCode = "\x1b[0m";

            static ConsoleColorManager()
            {
                // Inicializa cache de códigos ANSI uma vez
                _ansiColorCodes = new string[_logLevelColors.Length];
                for (int i = 0; i < _logLevelColors.Length; i++)
                {
                    _ansiColorCodes[i] = GetAnsiCode(_logLevelColors[i]);
                }
            }

            /// <summary>
            /// Escreve texto colorido de forma síncrona
            /// </summary>
            public static void WriteColored(string text, LogLevel logLevel)
            {
                if (string.IsNullOrEmpty(text))
                    return;

                var originalColor = Console.ForegroundColor;

                try
                {
                    // 1. Aplica cor baseada no nível
                    ApplyColor(logLevel);

                    // 2. Escreve o texto
                    Console.Write(text);
                }
                finally
                {
                    // 3. Restaura cor original (garantido!)
                    Console.ForegroundColor = originalColor;
                }
            }

            /// <summary>
            /// Escreve linha colorida de forma síncrona (texto + newline)
            /// </summary>
            public static void WriteColoredLine(string text, LogLevel logLevel)
            {
                if (string.IsNullOrEmpty(text))
                {
                    Console.WriteLine();
                    return;
                }

                var originalColor = Console.ForegroundColor;

                try
                {
                    // 1. Aplica cor baseada no nível
                    ApplyColor(logLevel);

                    // 2. Escreve o texto
                    Console.WriteLine(text);
                }
                finally
                {
                    // 3. Restaura cor original (garantido!)
                    Console.ForegroundColor = originalColor;
                }
            }

            /// <summary>
            /// Aplica a cor do console baseada no LogLevel
            /// </summary>
            private static void ApplyColor(LogLevel logLevel)
            {
                int levelIndex = MapLogLevelToIndex(logLevel);
                Console.ForegroundColor = _logLevelColors[levelIndex];
            }

            /// <summary>
            /// Mapeia LogLevel para índice na tabela de cores
            /// </summary>
            private static int MapLogLevelToIndex(LogLevel logLevel)
            {
                switch (logLevel)
                {
                    case LogLevel.Trace:
                        return 0;
                    case LogLevel.Debug:
                        return 1;
                    case LogLevel.Information:
                        return 2;
                    case LogLevel.Warning:
                        return 3;
                    case LogLevel.Error:
                        return 4;
                    case LogLevel.Critical:
                        return 5;
                    default:
                        return 6; // None ou desconhecido
                }
            }

            /// <summary>
            /// Converte ConsoleColor para código ANSI (para uso futuro)
            /// </summary>
            private static string GetAnsiCode(ConsoleColor color)
            {
                switch (color)
                {
                    case ConsoleColor.Black:
                        return "\x1b[30m";
                    case ConsoleColor.DarkBlue:
                        return "\x1b[34m";
                    case ConsoleColor.DarkGreen:
                        return "\x1b[32m";
                    case ConsoleColor.DarkCyan:
                        return "\x1b[36m";
                    case ConsoleColor.DarkRed:
                        return "\x1b[31m";
                    case ConsoleColor.DarkMagenta:
                        return "\x1b[35m";
                    case ConsoleColor.DarkYellow:
                        return "\x1b[33m";
                    case ConsoleColor.Gray:
                        return "\x1b[37m";
                    case ConsoleColor.DarkGray:
                        return "\x1b[90m";
                    case ConsoleColor.Blue:
                        return "\x1b[94m";
                    case ConsoleColor.Green:
                        return "\x1b[92m";
                    case ConsoleColor.Cyan:
                        return "\x1b[96m";
                    case ConsoleColor.Red:
                        return "\x1b[91m";
                    case ConsoleColor.Magenta:
                        return "\x1b[95m";
                    case ConsoleColor.Yellow:
                        return "\x1b[93m";
                    case ConsoleColor.White:
                        return "\x1b[97m";
                    default:
                        return "\x1b[97m"; // White como fallback
                }

            }

            /// <summary>
            /// Verifica se a saída é para um console real (não redirecionada)
            /// </summary>
            public static bool IsOutputToRealConsole()
            {
                try
                {
                    return Console.IsOutputRedirected == false;
                }
                catch
                {
                    return false;
                }
            }
        }
        #endregion
    }
}
