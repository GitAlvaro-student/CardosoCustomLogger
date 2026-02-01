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
                    if (Environment.GetEnvironmentVariable("TERM") != null)
                        return true;

                    // Método 2: Platform detection
                    if (Environment.GetEnvironmentVariable("WT_SESSION") != null) // Windows Terminal
                        return true;

                    if (Environment.GetEnvironmentVariable("VSCODE_PID") != null) // VS Code
                        return true;

                    // Método 3: RuntimeInformation
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        // Windows 10+ suporta ANSI nativamente
                        if (Environment.OSVersion.Version.Major >= 10)
                            return true;
                    }
                    else
                    {
                        // Linux/macOS geralmente suportam ANSI
                        return true;
                    }

                    // Método 4: Console propriedade (disponível no .NET 5+)
                    if (Console.IsOutputRedirected == false)
                    {
                        try
                        {
                            // Tenta escrever um código ANSI e verificar cursor position
                            // (método heurístico)
                            var originalLeft = Console.CursorLeft;
                            Console.Write("\x1b[6n"); // Query cursor position
                            Task.Delay(50).Wait(); // Pequena pausa
                            if (Console.CursorLeft != originalLeft)
                                return true;
                        }
                        catch
                        {
                            // Ignora falhas na detecção
                        }
                    }

                    return false;
                }
                catch
                {
                    return false; // Em caso de erro, assume sem suporte
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
            /// <summary>
            /// Escreve linha colorida usando o método apropriado (ANSI ou ConsoleColor)
            /// </summary>
            public static void WriteColoredLine(string text, LogLevel logLevel)
            {
                // Em diferentes ambientes, verificar comportamento
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

                if (_hasAnsiSupport && !_isNonInteractiveConsole)
                {
                    // ✅ Usa ANSI codes (mais performático, não muda estado global)
                    WriteWithAnsi(text, logLevel);
                }
                else
                {
                    // ✅ Fallback para ConsoleColor (compatibilidade)
                    WriteWithConsoleColor(text, logLevel);
                }
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

                if (_hasAnsiSupport && !_isNonInteractiveConsole)
                {
                    // ✅ Usa ANSI codes
                    await WriteWithAnsiAsync(text, logLevel, cancellationToken);
                }
                else
                {
                    // ✅ Fallback para ConsoleColor
                    await WriteWithConsoleColorAsync(text, logLevel, cancellationToken);
                }
            }

            /// <summary>
            /// Escreve com ANSI escape codes (não altera Console.ForegroundColor)
            /// </summary>
            private static void WriteWithAnsi(string text, LogLevel logLevel)
            {
                int levelIndex = MapLogLevelToIndex(logLevel);
                string ansiCode = _ansiColorCodes[levelIndex];

                // Formato: <ANSI_COLOR>text<ANSI_RESET>
                Console.WriteLine($"{ansiCode}{text}{AnsiResetCode}");
            }

            private static async Task WriteWithAnsiAsync(string text, LogLevel logLevel, CancellationToken cancellationToken)
            {
                int levelIndex = MapLogLevelToIndex(logLevel);
                string ansiCode = _ansiColorCodes[levelIndex];

                // Concatenação eficiente
                var coloredText = string.Concat(ansiCode, text, AnsiResetCode, Environment.NewLine);
                await Console.Out.WriteAsync(coloredText);
            }

            /// <summary>
            /// Método original usando Console.ForegroundColor (fallback)
            /// </summary>
            private static void WriteWithConsoleColor(string text, LogLevel logLevel)
            {
                var originalColor = Console.ForegroundColor;

                try
                {
                    ApplyColor(logLevel);
                    Console.WriteLine(text);
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }

            private static async Task WriteWithConsoleColorAsync(string text, LogLevel logLevel, CancellationToken cancellationToken)
            {
                var originalColor = Console.ForegroundColor;

                try
                {
                    ApplyColor(logLevel);
                    await Console.Out.WriteLineAsync(text);
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }

            /// <summary>
            /// Versão otimizada para batch usando ANSI
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
                    await WriteBatchWithAnsiAsync(entries, cancellationToken);
                }
                else
                {
                    await WriteBatchWithConsoleColorAsync(entries, cancellationToken);
                }
            }

            /// <summary>
            /// Batch otimizado com ANSI - agrupa por cor para menos concatenações
            /// </summary>
            private static async Task WriteBatchWithAnsiAsync(
                IEnumerable<(string Text, LogLevel Level)> entries,
                CancellationToken cancellationToken)
            {
                var builder = new StringBuilder();
                string currentAnsiCode = null;

                foreach (var (text, level) in entries)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    int levelIndex = MapLogLevelToIndex(level);
                    string ansiCode = _ansiColorCodes[levelIndex];

                    // Se mudou a cor, finaliza o bloco anterior
                    if (currentAnsiCode != ansiCode)
                    {
                        if (builder.Length > 0)
                        {
                            await Console.Out.WriteAsync(builder.ToString());
                            builder.Clear();
                        }
                        currentAnsiCode = ansiCode;
                    }

                    // Concatena eficientemente: cor + texto + reset + newline
                    builder.Append(ansiCode);
                    builder.Append(text);
                    builder.Append(AnsiResetCode);
                    builder.AppendLine();

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
            /// Batch usando Console.ForegroundColor (fallback para quando ANSI não está disponível)
            /// </summary>
            private static async Task WriteBatchWithConsoleColorAsync(
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

                        // ✅ OTIMIZAÇÃO: Só muda cor se necessário
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
