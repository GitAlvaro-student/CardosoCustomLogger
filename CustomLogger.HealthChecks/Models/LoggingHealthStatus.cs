using System;
using System.Collections.Generic;
using System.Text;

namespace CustomLogger.HealthChecks.Models
{
    /// <summary>
    /// Representa o estado de saúde do sistema de logging.
    /// 
    /// ORDEM DE SEVERIDADE (crescente):
    /// Unknown (0) → Healthy (1) → Degraded (2) → Unhealthy (3)
    /// 
    /// INTERPRETAÇÃO:
    /// - <see cref="Unknown"/>: Estado indeterminado ou falha na avaliação
    /// - <see cref="Healthy"/>: Sistema operando normalmente
    /// - <see cref="Degraded"/>: Sistema operacional mas com restrições
    /// - <see cref="Unhealthy"/>: Sistema comprometido, pode haver perda de dados
    /// 
    /// VERSIONAMENTO:
    /// Os valores numéricos são fixos e não devem ser alterados em versões futuras.
    /// Novos estados (se necessário) devem usar valores >= 100 para evitar conflitos.
    /// 
    /// COMPATIBILIDADE:
    /// .NET Standard 2.0 - compatível com .NET Framework 4.6.1+ e .NET Core 2.0+
    /// </summary>
    public enum LoggingHealthStatus
    {
        /// <summary>
        /// Estado de saúde indeterminado.
        /// 
        /// QUANDO USAR:
        /// - Avaliação falhou por exceção inesperada
        /// - Estado interno inconsistente ou inacessível
        /// - Sistema em inicialização ou shutdown
        /// - Dados insuficientes para determinar saúde
        /// 
        /// COMPORTAMENTO ESPERADO:
        /// - HealthChecks devem retornar Unknown em vez de lançar exceção
        /// - Consumidores devem tratar como "assume worst case" ou retry
        /// - Logging de diagnóstico (fora do CustomLogger) pode registrar causa
        /// 
        /// IMPORTANTE:
        /// Unknown não é um estado de negócio do logger, mas sim uma proteção
        /// contra falhas no próprio mecanismo de health check.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Sistema de logging operando dentro de parâmetros normais.
        /// 
        /// CRITÉRIOS (todos devem ser verdadeiros):
        /// - Buffer abaixo de 80% da capacidade
        /// - Nenhum sink em modo fallback
        /// - Nenhum sink falhando continuamente
        /// - Modo degradado NÃO ativo
        /// 
        /// GARANTIAS:
        /// - Logs estão sendo processados sem atrasos significativos
        /// - Nenhum log está sendo descartado
        /// - Todos os destinos configurados estão acessíveis
        /// 
        /// INTERPRETAÇÃO:
        /// Sistema confiável para logging em produção.
        /// </summary>
        Healthy = 1,

        /// <summary>
        /// Sistema de logging operacional mas com funcionalidade reduzida.
        /// 
        /// SITUAÇÕES QUE CAUSAM DEGRADED:
        /// - Buffer entre 80% e 99% da capacidade
        /// - Um ou mais sinks operando em modo fallback
        /// - Modo degradado do provider está ativo
        /// - Sink com falhas intermitentes (não contínuas)
        /// 
        /// GARANTIAS:
        /// - Logs ainda estão sendo processados (possivelmente com atraso)
        /// - Nenhum log sendo descartado ainda
        /// - Funcionalidade principal mantida através de fallbacks
        /// 
        /// INTERPRETAÇÃO:
        /// Sistema ainda utilizável, mas requer atenção. Investigar causa da degradação.
        /// Pode escalar para Unhealthy se não for resolvido.
        /// </summary>
        Degraded = 2,

        /// <summary>
        /// Sistema de logging comprometido, pode haver perda de dados.
        /// 
        /// SITUAÇÕES QUE CAUSAM UNHEALTHY:
        /// - Buffer em 100% da capacidade (descartando mensagens)
        /// - Sink falhando continuamente (2+ falhas consecutivas em 5 minutos)
        /// - Todos os sinks primários falhando simultaneamente
        /// - Modo degradado ativo E buffer próximo do limite
        /// 
        /// CONSEQUÊNCIAS:
        /// - Logs podem estar sendo descartados
        /// - Perda de observabilidade da aplicação
        /// - Diagnóstico de problemas comprometido
        /// 
        /// INTERPRETAÇÃO:
        /// Sistema NÃO confiável para logging. Requer intervenção imediata.
        /// Considerar ações corretivas (aumentar capacidade, verificar destinos).
        /// </summary>
        Unhealthy = 3
    }
}
