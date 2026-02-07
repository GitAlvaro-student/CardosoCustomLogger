using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace CustomLogger.Providers
{
    /// <summary>
    /// Estados do ciclo de vida do CustomLoggerProvider.
    /// Transições: CREATED → OPERATIONAL → STOPPING → DISPOSING → DISPOSED
    /// </summary>
    internal enum ProviderState
    {
        /// <summary>
        /// Provider criado mas nenhum logger foi solicitado ainda.
        /// CreateLogger(): permitido | Enqueue(): N/A | Dispose(): permitido
        /// </summary>
        Created = 0,

        /// <summary>
        /// Provider operacional, aceitando logs.
        /// CreateLogger(): permitido | Enqueue(): permitido | Dispose(): permitido
        /// </summary>
        Operational = 1,

        /// <summary>
        /// Shutdown iniciado, rejeitando novos logs, drenando buffer.
        /// CreateLogger(): ObjectDisposedException | Enqueue(): silent ignore | Dispose(): idempotente
        /// </summary>
        Stopping = 2,

        /// <summary>
        /// Flush final em andamento, liberando recursos.
        /// CreateLogger(): ObjectDisposedException | Enqueue(): silent ignore | Dispose(): idempotente
        /// </summary>
        Disposing = 3,

        /// <summary>
        /// Todos os recursos liberados, objeto inutilizável.
        /// CreateLogger(): ObjectDisposedException | Enqueue(): silent ignore | Dispose(): idempotente
        /// </summary>
        Disposed = 4
    }

    /// <summary>
    /// Gerenciador de estado do Provider.
    /// Garante transições irreversíveis e thread-safe.
    /// </summary>
    internal sealed class ProviderLifecycleManager
    {
        // Estado atual do provider (int para uso com Interlocked)
        private int _state = (int)ProviderState.Created;

        /// <summary>
        /// Estado atual do provider (apenas leitura).
        /// </summary>
        public ProviderState CurrentState => (ProviderState)Interlocked.CompareExchange(ref _state, 0, 0);

        /// <summary>
        /// Tenta transitar para OPERATIONAL.
        /// RFC: CREATED → OPERATIONAL ocorre em CreateLogger().
        /// </summary>
        /// <returns>true se transição foi bem-sucedida, false se já estava em OPERATIONAL ou além</returns>
        public bool TryTransitionToOperational()
        {
            // RFC: Transição CREATED → OPERATIONAL é permitida
            // Usa CompareExchange para atomicidade
            int expectedState = (int)ProviderState.Created;
            int newState = (int)ProviderState.Operational;
            int previousState = Interlocked.CompareExchange(ref _state, newState, expectedState);

            // Retorna true se transição ocorreu, false se já estava além de CREATED
            return previousState == expectedState;
        }

        /// <summary>
        /// Transita para STOPPING (início do shutdown).
        /// RFC: Primeira instrução do Dispose() deve setar STOPPING.
        /// </summary>
        /// <returns>true se esta thread iniciou o shutdown, false se shutdown já estava em andamento</returns>
        public bool TryTransitionToStopping()
        {
            // RFC: Dispose() deve ser idempotente
            // Apenas a PRIMEIRA thread que chamar Dispose() deve executar shutdown

            while (true)
            {
                int currentState = Interlocked.CompareExchange(ref _state, 0, 0);

                // Se já está em STOPPING ou além, retorna false (idempotência)
                if (currentState >= (int)ProviderState.Stopping)
                {
                    return false;
                }

                // Tenta transitar para STOPPING
                int previousState = Interlocked.CompareExchange(
                    ref _state,
                    (int)ProviderState.Stopping,
                    currentState);

                // Se transição foi bem-sucedida, esta thread "ganhou" o shutdown
                if (previousState == currentState)
                {
                    return true;
                }

                // Outro thread mudou estado, tenta novamente (spin)
                // Justificativa: race é improvável e window é nanossegundos
            }
        }

        /// <summary>
        /// Transita para DISPOSING (flush final em andamento).
        /// RFC: Ocorre após Timer.Dispose() e antes de Flush() final.
        /// </summary>
        public void TransitionToDisposing()
        {
            // RFC: STOPPING → DISPOSING é transição interna do Dispose()
            // Apenas a thread que "ganhou" TryTransitionToStopping() deve chamar isso
            // Não precisa de CAS pois apenas uma thread pode estar aqui

            int expectedState = (int)ProviderState.Stopping;
            int previousState = Interlocked.CompareExchange(
                ref _state,
                (int)ProviderState.Disposing,
                expectedState);

            // Invariante: Estado DEVE ser STOPPING
            // Se não for, há bug de concorrência no código que chama este método
            if (previousState != expectedState)
            {
                // Violação de invariante - isto nunca deveria acontecer
                throw new InvalidOperationException(
                    $"Invalid state transition to Disposing. Expected Stopping but was {(ProviderState)previousState}");
            }
        }

        /// <summary>
        /// Transita para DISPOSED (shutdown completo).
        /// RFC: Última transição, após todos os recursos serem liberados.
        /// </summary>
        public void TransitionToDisposed()
        {
            // RFC: DISPOSING → DISPOSED é transição final
            // Apenas a thread que iniciou shutdown deve chamar isso

            int expectedState = (int)ProviderState.Disposing;
            int previousState = Interlocked.CompareExchange(
                ref _state,
                (int)ProviderState.Disposed,
                expectedState);

            // Invariante: Estado DEVE ser DISPOSING
            if (previousState != expectedState)
            {
                // Violação de invariante - isto nunca deveria acontecer
                throw new InvalidOperationException(
                    $"Invalid state transition to Disposed. Expected Disposing but was {(ProviderState)previousState}");
            }
        }

        /// <summary>
        /// Verifica se CreateLogger() pode ser chamado.
        /// RFC: Permitido apenas em CREATED e OPERATIONAL.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Se estado for STOPPING, DISPOSING ou DISPOSED</exception>
        public void GuardCreateLogger()
        {
            ProviderState current = CurrentState;

            // RFC: CreateLogger() permitido em CREATED e OPERATIONAL
            if (current == ProviderState.Created || current == ProviderState.Operational)
            {
                return; // Permitido
            }

            // RFC: CreateLogger() lança ObjectDisposedException em STOPPING, DISPOSING, DISPOSED
            throw new ObjectDisposedException(
                "CustomLoggerProvider",
                $"Cannot create logger when provider is in state {current}");
        }

        /// <summary>
        /// Verifica se Enqueue() pode aceitar logs.
        /// RFC: Aceita apenas em OPERATIONAL.
        /// </summary>
        /// <returns>true se pode aceitar log, false se deve rejeitar silenciosamente</returns>
        public bool CanEnqueue()
        {
            // RFC: Hot path NUNCA lança exceções
            // RFC: Enqueue aceita logs apenas em OPERATIONAL
            // RFC: Outros estados rejeitam silenciosamente

            ProviderState current = CurrentState;
            return current == ProviderState.Operational;
        }

        /// <summary>
        /// Verifica se Flush() explícito pode ser executado.
        /// RFC: Flush explícito retorna silenciosamente se não estiver em OPERATIONAL.
        /// RFC: Flush implícito (do Dispose) executa sempre.
        /// </summary>
        /// <param name="isImplicitFromDispose">true se chamado pelo Dispose(), false se chamado externamente</param>
        /// <returns>true se deve executar flush, false se deve retornar silenciosamente</returns>
        public bool CanFlush(bool isImplicitFromDispose)
        {
            ProviderState current = CurrentState;

            // RFC: Flush implícito do Dispose() sempre executa (mesmo em DISPOSING)
            if (isImplicitFromDispose && current == ProviderState.Disposing)
            {
                return true;
            }

            // RFC: Flush explícito só executa em OPERATIONAL
            // Demais estados retornam silenciosamente
            return current == ProviderState.Operational;
        }
    }
}

