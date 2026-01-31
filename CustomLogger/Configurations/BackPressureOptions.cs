using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.Configurations
{
    public sealed class BackpressureOptions
    {
        /// <summary>
        /// Capacidade máxima da fila. Quando atingida, aplica estratégia de overflow.
        /// </summary>
        public int MaxQueueCapacity { get; set; } = 10000;

        /// <summary>
        /// Estratégia quando fila está cheia.
        /// </summary>
        public OverflowStrategy OverflowStrategy { get; set; } = OverflowStrategy.DropOldest;
    }

    public enum OverflowStrategy
    {
        /// <summary>
        /// Descarta logs mais antigos (padrão - melhor para logs operacionais).
        /// </summary>
        DropOldest,

        /// <summary>
        /// Descarta logs mais recentes (melhor para métricas/analytics).
        /// </summary>
        DropNewest,

        /// <summary>
        /// Bloqueia até ter espaço (pode travar aplicação - use com cuidado).
        /// </summary>
        Block
    }
}
