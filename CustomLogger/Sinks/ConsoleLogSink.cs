using CustomLogger.Abstractions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
                string formattedMessage = _formatter.Format(entry);
                ConsoleColorManager.WriteColoredLine(formattedMessage, entry.LogLevel);
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
                foreach (var entry in entries)
                {
                    string formattedMessage = _formatter.Format(entry);
                    ConsoleColorManager.WriteColoredLine(formattedMessage, entry.LogLevel);
                }

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
                string formattedMessage = _formatter.Format(entry);
                await ConsoleColorManager.WriteColoredLineAsync(formattedMessage, entry.LogLevel, cancellationToken);
            }
            catch
            {
                // Absorve falha (mantido)
            }
        }

        // ✅ MODIFICADO: WriteBatchAsync otimizado com cores
        public async Task WriteBatchAsync(IEnumerable<ILogEntry> entries, CancellationToken cancellationToken = default)
        {
            if (entries == null)
                return;

            try
            {
                var coloredEntries = entries
                    .Where(e => e != null)
                    .Select(e => (Text: _formatter.Format(e), Level: e.LogLevel))
                    .ToList();

                if (coloredEntries.Count > 0)
                {
                    await ConsoleColorManager.WriteColoredBatchAsync(coloredEntries, cancellationToken);
                    await Console.Out.FlushAsync();
                }
            }
            catch
            {
                // Absorve falha (mantido)
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
            // Flags de capacidade detectadas uma vez
            private static readonly bool _hasAnsiSupport;
            private static readonly bool _isNonInteractiveConsole;

            // Cache de códigos ANSI para cada nível
            private static readonly string[] _ansiColorCodes;
            private const string AnsiResetCode = "\x1b[0m";

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

            // Mapa de ConsoleColor para código ANSI (refatoração de GetAnsiCode)
            private static readonly Dictionary<ConsoleColor, string> _consoleColorToAnsi =
                new Dictionary<ConsoleColor, string>
                {
                    { ConsoleColor.Black, "\x1b[30m" },
                    { ConsoleColor.DarkBlue, "\x1b[34m" },
                    { ConsoleColor.DarkGreen, "\x1b[32m" },
                    { ConsoleColor.DarkCyan, "\x1b[36m" },
                    { ConsoleColor.DarkRed, "\x1b[31m" },
                    { ConsoleColor.DarkMagenta, "\x1b[35m" },
                    { ConsoleColor.DarkYellow, "\x1b[33m" },
                    { ConsoleColor.Gray, "\x1b[37m" },
                    { ConsoleColor.DarkGray, "\x1b[90m" },
                    { ConsoleColor.Blue, "\x1b[94m" },
                    { ConsoleColor.Green, "\x1b[92m" },
                    { ConsoleColor.Cyan, "\x1b[96m" },
                    { ConsoleColor.Red, "\x1b[91m" },
                    { ConsoleColor.Magenta, "\x1b[95m" },
                    { ConsoleColor.Yellow, "\x1b[93m" },
                    { ConsoleColor.White, "\x1b[97m" }
                };

            // Cache de strings ANSI para melhor performance

            static ConsoleColorManager()
            {
                // Inicializa cache de códigos ANSI
                _ansiColorCodes = new string[]
                {
            "\x1b[90m",  // DarkGray (Trace)
            "\x1b[37m",  // Gray (Debug)
            "\x1b[32m",  // Green (Information)
            "\x1b[33m",  // Yellow (Warning)
            "\x1b[31m",  // Red (Error)
            "\x1b[91m",  // BrightRed (Critical)
            "\x1b[97m"   // White (Default)
                };

                // Detectar capacidades do console
                _hasAnsiSupport = DetectAnsiSupport();
                _isNonInteractiveConsole = DetectNonInteractiveConsole();
            }

            /// <summary>
            /// Detectar se o terminal suporta ANSI escape codes
            /// </summary>
            private static bool DetectAnsiSupport()
            {
                try
                {
                    // Método 1: Variável de ambiente (comum em terminais modernos)
                    if (HasTermEnvironmentVariable())
                        return true;

                    // Método 2: Detecção específica de platform
                    if (IsWindowsTerminalOrVsCode())
                        return true;

                    // Método 3: RuntimeInformation
                    if (IsAnsiSupportedByPlatform())
                        return true;

                    // Método 4: Console propriedade (disponível no .NET 5+)
                    if (IsAnsiSupportedByHeuristic())
                        return true;

                    return false;
                }
                catch
                {
                    return false; // Em caso de erro, assume sem suporte
                }
            }

            /// <summary>
            /// Verifica se TERM está definido (indica terminal moderno)
            /// </summary>
            private static bool HasTermEnvironmentVariable()
            {
                return Environment.GetEnvironmentVariable("TERM") != null;
            }

            /// <summary>
            /// Detecta Windows Terminal ou VS Code (ambientes com suporte a ANSI garantido)
            /// </summary>
            private static bool IsWindowsTerminalOrVsCode()
            {
                var modernTerminals = new[] { "WT_SESSION", "VSCODE_PID" };
                return modernTerminals.Any(env => Environment.GetEnvironmentVariable(env) != null);
            }

            /// <summary>
            /// Verifica suporte ANSI baseado no SO
            /// </summary>
            private static bool IsAnsiSupportedByPlatform()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    // Windows 10+ suporta ANSI nativamente
                    return Environment.OSVersion.Version.Major >= 10;
                }

                // Linux/macOS geralmente suportam ANSI
                return true;
            }

            /// <summary>
            /// Tenta detecção heurística de ANSI via cursor position query
            /// </summary>
            private static bool IsAnsiSupportedByHeuristic()
            {
                if (Console.IsOutputRedirected)
                    return false;

                try
                {
                    var originalLeft = Console.CursorLeft;
                    Console.Write("\x1b[6n"); // Query cursor position
                    Task.Delay(50).Wait(); // Pequena pausa
                    return Console.CursorLeft != originalLeft;
                }
                catch
                {
                    return false;
                }
            }
            /// <summary>
            /// Detectar se estamos em console não-interativo (CI/CD, Docker)
            /// </summary>
            private static bool DetectNonInteractiveConsole()
            {
                try
                {
                    // Variáveis de ambiente comuns em ambientes CI/CD
                    var ciEnvVars = new[]
                    {
                "CI", "CONTINUOUS_INTEGRATION", "BUILD_NUMBER",
                "TF_BUILD", "GITHUB_ACTIONS", "GITLAB_CI",
                "JENKINS_URL", "TEAMCITY_VERSION", "BITBUCKET_BUILD_NUMBER"
            };

                    if (ciEnvVars.Any(env => Environment.GetEnvironmentVariable(env) != null))
                        return true;

                    // Console não-interativo
                    if (Console.IsOutputRedirected && Console.IsInputRedirected)
                        return true;

                    // Docker sem TTY
                    if (Environment.GetEnvironmentVariable("DOCKER_CONTAINER") != null &&
                        Environment.GetEnvironmentVariable("TERM") == null)
                        return true;

                    return false;
                }
                catch
                {
                    return false;
                }
            }

            /// <summary>
            /// Determinar se deve usar cores (considerando todos os fatores)
            /// </summary>
            public static bool ShouldUseColors()
            {
                // Não usar cores se:
                // 1. Usuário desabilitou via NO_COLOR padrão
                if (Environment.GetEnvironmentVariable("NO_COLOR") != null)
                    return false;

                // 2. Console não-interativo (CI/CD)
                if (_isNonInteractiveConsole)
                    return false;

                // 3. Output não é para console real
                return IsOutputToRealConsole();
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

                if (!ShouldUseColors())
                {
                    // Sem cores
                    Console.WriteLine(text);
                    return;
                }

                WriteColoredLineCore(text, logLevel, isAsync: false).Wait();
            }

            /// <summary>
            /// Versão assíncrona com suporte a ANSI
            /// </summary>
            public static async Task WriteColoredLineAsync(string text, LogLevel logLevel, CancellationToken cancellationToken = default)
            {
                if (string.IsNullOrEmpty(text))
                {
                    await Console.Out.WriteLineAsync();
                    return;
                }

                if (!ShouldUseColors())
                {
                    // Sem cores
                    await Console.Out.WriteLineAsync(text);
                    return;
                }

                await WriteColoredLineCore(text, logLevel, isAsync: true, cancellationToken);
            }

            /// <summary>
            /// Template Method: Núcleo compartilhado entre síncrono e assíncrono
            /// Reduz duplicação e centraliza decisão de ANSI vs ConsoleColor
            /// </summary>
            private static async Task WriteColoredLineCore(
                string text,
                LogLevel logLevel,
                bool isAsync = false,
                CancellationToken cancellationToken = default)
            {
                if (_hasAnsiSupport && !_isNonInteractiveConsole)
                {
                    await WriteWithAnsiCore(text, logLevel, isAsync, cancellationToken);
                }
                else
                {
                    await WriteWithConsoleColorCore(text, logLevel, isAsync, cancellationToken);
                }
            }

            /// <summary>
            /// Escreve com ANSI escape codes (não altera Console.ForegroundColor)
            /// Versão unificada síncrono/assíncrono
            /// </summary>
            private static async Task WriteWithAnsiCore(
                string text,
                LogLevel logLevel,
                bool isAsync = false,
                CancellationToken cancellationToken = default)
            {
                int levelIndex = MapLogLevelToIndex(logLevel);
                string ansiCode = _ansiColorCodes[levelIndex];
                string coloredText = string.Concat(ansiCode, text, AnsiResetCode, Environment.NewLine);

                if (isAsync)
                {
                    await Console.Out.WriteAsync(coloredText);
                }
                else
                {
                    Console.Write(coloredText);
                }
            }

            /// <summary>
            /// Escreve com Console.ForegroundColor (fallback)
            /// Versão unificada síncrono/assíncrono
            /// </summary>
            private static async Task WriteWithConsoleColorCore(
                string text,
                LogLevel logLevel,
                bool isAsync = false,
                CancellationToken cancellationToken = default)
            {
                var originalColor = Console.ForegroundColor;

                try
                {
                    ApplyColor(logLevel);

                    if (isAsync)
                    {
                        await Console.Out.WriteLineAsync(text);
                    }
                    else
                    {
                        Console.WriteLine(text);
                    }
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }

            /// <summary>
            /// Versão otimizada para batch usando ANSI ou ConsoleColor
            /// </summary>
            public static async Task WriteColoredBatchAsync(
                IEnumerable<(string Text, LogLevel Level)> entries,
                CancellationToken cancellationToken = default)
            {
                if (!ShouldUseColors())
                {
                    // Sem cores - escrita direta
                    foreach (var (text, _) in entries)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        await Console.Out.WriteLineAsync(text);
                    }
                    return;
                }

                if (_hasAnsiSupport && !_isNonInteractiveConsole)
                {
                    await WriteBatchWithAnsiCore(entries, cancellationToken);
                }
                else
                {
                    await WriteBatchWithConsoleColorCore(entries, cancellationToken);
                }
            }

            /// <summary>
            /// Batch com ANSI - agrupa por cor para menos concatenações
            /// </summary>
            private static async Task WriteBatchWithAnsiCore(
                IEnumerable<(string Text, LogLevel Level)> entries,
                CancellationToken cancellationToken)
            {
                var builder = new StringBuilder();
                int currentLevelIndex = -1;

                foreach (var (text, level) in entries)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    int levelIndex = MapLogLevelToIndex(level);

                    // Se mudou a cor, finaliza o bloco anterior
                    if (currentLevelIndex != levelIndex)
                    {
                        if (builder.Length > 0)
                        {
                            await Console.Out.WriteAsync(builder.ToString());
                            builder.Clear();
                        }
                        currentLevelIndex = levelIndex;
                    }

                    AppendColoredLineToBuffer(builder, text, levelIndex);

                    // Flush a cada 100 linhas para não consumir muita memória
                    if (builder.Length > 8192) // ~8KB
                    {
                        await Console.Out.WriteAsync(builder.ToString());
                        builder.Clear();
                    }
                }

                // Escreve o restante
                if (builder.Length > 0)
                {
                    await Console.Out.WriteAsync(builder.ToString());
                }
            }

            /// <summary>
            /// Batch com ConsoleColor (fallback para quando ANSI não está disponível)
            /// </summary>
            private static async Task WriteBatchWithConsoleColorCore(
                IEnumerable<(string Text, LogLevel Level)> entries,
                CancellationToken cancellationToken)
            {
                var originalColor = Console.ForegroundColor;
                LogLevel? lastLevel = null;

                try
                {
                    foreach (var (text, level) in entries)
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        // OTIMIZAÇÃO: Só muda cor se necessário
                        if (level != lastLevel)
                        {
                            ApplyColor(level);
                            lastLevel = level;
                        }

                        await Console.Out.WriteLineAsync(text);
                    }
                }
                finally
                {
                    // Restaura cor original
                    Console.ForegroundColor = originalColor;
                }
            }

            /// <summary>
            /// Append eficiente de linha colorida no buffer (ANSI)
            /// </summary>
            private static void AppendColoredLineToBuffer(StringBuilder builder, string text, int levelIndex)
            {
                builder.Append(_ansiColorCodes[levelIndex]);
                builder.Append(text);
                builder.Append(AnsiResetCode);
                builder.AppendLine();
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
            /// Refatorado: usa dicionário estático em vez de switch
            /// </summary>
            private static string GetAnsiCode(ConsoleColor color)
            {
                return _consoleColorToAnsi.TryGetValue(color, out var code)
                    ? code
                    : "\x1b[97m"; // White como fallback
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
