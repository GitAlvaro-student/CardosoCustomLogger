using Microsoft.Ajax.Utilities;
using Microsoft.Extensions.Logging;
using PaymentAPI.Models;
using PaymentAPI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;
using System.Web.Http.Description;
using static PaymentAPI.WebApiApplication;

namespace PaymentAPI.Controllers
{
    /// <summary>
    /// API para gerenciamento de pagamentos
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/pagamentos")]
    public class PagamentosController : ApiController
    {
        private readonly ILogger _logger = Global.LoggerFactory.CreateLogger<PagamentosController>();
        
        private readonly IPaymentService _pagamentoService;

        public PagamentosController()
        {
            _pagamentoService = new PaymentService();
        }

        /// <summary>
        /// Obtém todos os pagamentos
        /// </summary>
        /// <returns>Lista de pagamentos</returns>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(Payment[]))]
        public IHttpActionResult Get()
        {
            try
            {
                _logger.LogTrace("This is a trace log, useful for debugging.");
                _logger.LogDebug("This is a debug log, useful for development.");
                _logger.LogInformation("This is an information log, useful for general information.");
                _logger.LogWarning("This is a warning log, indicating a potential issue.");
                _logger.LogError(new InvalidOperationException("Invalid Generic Operation"), "This is an error log, indicating a failure in the application.");
                _logger.LogCritical("This is a critical log, indicating a severe failure.");

                _logger.LogDebug(Logging.PagamentoEventIds.ConsultarTodos,
                    "Requisição GET recebida para obter todos os pagamentos");

                var pagamentos = _pagamentoService.ObterTodos();

                _logger.LogInformation(Logging.PagamentoEventIds.ConsultarTodos,
                    "Retornando {QuantidadePagamentos} pagamentos", pagamentos.Count);

                return Ok(pagamentos);
            }
            catch (Exception ex)
            {
                _logger.LogError(Logging.PagamentoEventIds.ErroInterno, ex,
                    "Erro interno ao obter todos os pagamentos: {MensagemErro}", ex.Message);

                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Obtém um pagamento pelo ID
        /// </summary>
        /// <param name="id">ID do pagamento</param>
        /// <returns>Pagamento encontrado</returns>
        [HttpGet]
        [Route("{id:int}")]
        [ResponseType(typeof(Payment))]
        public IHttpActionResult Get(int id)
        {
            try
            {
                _logger.LogDebug(Logging.PagamentoEventIds.ConsultarPorId,
                    "Requisição GET recebida para pagamento ID: {PagamentoId}", id);

                var pagamento = _pagamentoService.ObterPorId(id);
                if (pagamento == null)
                {
                    _logger.LogWarning(Logging.PagamentoEventIds.PagamentoNaoEncontrado,
                        "Pagamento não encontrado na controller. ID: {PagamentoId}", id);
                    return NotFound();
                }

                _logger.LogInformation(Logging.PagamentoEventIds.PagamentoEncontrado,
                    "Pagamento encontrado via API: ID={PagamentoId}, Descrição={PagamentoDescricao}",
                    id, pagamento.Descricao);

                return Ok(pagamento);
            }
            catch (Exception ex)
            {
                _logger.LogError(Logging.PagamentoEventIds.ErroInterno, ex,
                    "Erro interno ao obter pagamento ID {PagamentoId}: {MensagemErro}",
                    id, ex.Message);

                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Cria um novo pagamento
        /// </summary>
        /// <param name="request">Dados do pagamento</param>
        /// <returns>Pagamento criado</returns>
        [HttpPost]
        [Route("")]
        [ResponseType(typeof(Payment))]
        public IHttpActionResult Post([FromBody] PaymentRequest request)
        {
            try
            {
                _logger.LogInformation(Logging.PagamentoEventIds.CriarPagamento,
                    "Requisição POST recebida para criar pagamento: {PagamentoDescricao}",
                    request?.Descricao ?? "N/A");

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                    _logger.LogWarning(Logging.PagamentoEventIds.ModelStateInvalido,
                        "ModelState inválido ao criar pagamento. Erros: {Erros}", errors);

                    return BadRequest(ModelState);
                }

                if (request == null)
                {
                    _logger.LogWarning(Logging.PagamentoEventIds.ValidacaoFalhou,
                        "Requisição POST com corpo vazio");
                    return BadRequest("Dados do pagamento não fornecidos");
                }

                if (request.Valor <= 0)
                {
                    _logger.LogWarning(Logging.PagamentoEventIds.ValidacaoFalhou,
                        "Tentativa de criar pagamento com valor inválido: {PagamentoValor}",
                        request.Valor);
                    return BadRequest("Valor do pagamento deve ser maior que zero");
                }

                var pagamento = _pagamentoService.Criar(request);

                _logger.LogInformation(Logging.PagamentoEventIds.PagamentoCriado,
                    "Pagamento criado com sucesso via API. ID: {PagamentoId}", pagamento.Id);

                var location = Url.Link("DefaultApi", new { controller = "pagamentos", id = pagamento.Id });
                return Created(location, pagamento);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(Logging.PagamentoEventIds.ValidacaoFalhou, ex,
                    "Argumento inválido ao criar pagamento: {MensagemErro}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(Logging.PagamentoEventIds.ErroInterno, ex,
                    "Erro interno ao criar pagamento: {MensagemErro}", ex.Message);

                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Atualiza um pagamento existente
        /// </summary>
        /// <param name="id">ID do pagamento</param>
        /// <param name="request">Novos dados do pagamento</param>
        /// <returns>Pagamento atualizado</returns>
        [HttpPut]
        [Route("{id:int}")]
        [ResponseType(typeof(Payment))]
        public IHttpActionResult Put(int id, [FromBody] PaymentRequest request)
        {
            try
            {
                _logger.LogInformation(Logging.PagamentoEventIds.AtualizarPagamento,
                    "Requisição PUT recebida para atualizar pagamento ID: {PagamentoId}", id);

                if (!ModelState.IsValid)
                {
                    var errors = string.Join("; ", ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage));

                    _logger.LogWarning(Logging.PagamentoEventIds.ModelStateInvalido,
                        "ModelState inválido ao atualizar pagamento ID {PagamentoId}. Erros: {Erros}",
                        id, errors);

                    return BadRequest(ModelState);
                }

                if (request == null)
                {
                    _logger.LogWarning(Logging.PagamentoEventIds.ValidacaoFalhou,
                        "Requisição PUT para ID {PagamentoId} com corpo vazio", id);
                    return BadRequest("Dados do pagamento não fornecidos");
                }

                var pagamento = _pagamentoService.Atualizar(id, request);
                if (pagamento == null)
                {
                    _logger.LogWarning(Logging.PagamentoEventIds.PagamentoNaoEncontradoParaAtualizar,
                        "Pagamento não encontrado para atualização via API. ID: {PagamentoId}", id);
                    return NotFound();
                }

                _logger.LogInformation(Logging.PagamentoEventIds.PagamentoAtualizado,
                    "Pagamento atualizado com sucesso via API. ID: {PagamentoId}", id);

                return Ok(pagamento);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(Logging.PagamentoEventIds.InvalidOperation, ex,
                    "Operação inválida ao atualizar pagamento ID {PagamentoId}: {MensagemErro}",
                    id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(Logging.PagamentoEventIds.ErroInterno, ex,
                    "Erro interno ao atualizar pagamento ID {PagamentoId}: {MensagemErro}",
                    id, ex.Message);

                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Exclui um pagamento
        /// </summary>
        /// <param name="id">ID do pagamento</param>
        /// <returns>Status da operação</returns>
        [HttpDelete]
        [Route("{id:int}")]
        public IHttpActionResult Delete(int id)
        {
            try
            {
                _logger.LogInformation(Logging.PagamentoEventIds.RemoverPagamento,
                    "Requisição DELETE recebida para remover pagamento ID: {PagamentoId}", id);

                var excluido = _pagamentoService.Excluir(id);
                if (!excluido)
                {
                    _logger.LogWarning(Logging.PagamentoEventIds.PagamentoNaoEncontradoParaRemover,
                        "Pagamento não encontrado para remoção via API. ID: {PagamentoId}", id);
                    return NotFound();
                }

                _logger.LogInformation(Logging.PagamentoEventIds.PagamentoRemovido,
                    "Pagamento removido com sucesso via API. ID: {PagamentoId}", id);

                return Ok(new { message = "Pagamento excluído com sucesso" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(Logging.PagamentoEventIds.InvalidOperation, ex,
                    "Operação inválida ao remover pagamento ID {PagamentoId}: {MensagemErro}",
                    id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(Logging.PagamentoEventIds.ErroInterno, ex,
                    "Erro interno ao remover pagamento ID {PagamentoId}: {MensagemErro}",
                    id, ex.Message);

                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Processa um pagamento pendente
        /// </summary>
        /// <param name="id">ID do pagamento</param>
        /// <returns>Pagamento processado</returns>
        [HttpPost]
        [Route("{id:int}/processar")]
        [ResponseType(typeof(Payment))]
        public IHttpActionResult Processar(int id)
        {
            try
            {
                _logger.LogInformation(Logging.PagamentoEventIds.ProcessarPagamento,
                    "Requisição POST recebida para processar pagamento ID: {PagamentoId}", id);

                var pagamento = _pagamentoService.ProcessarPagamento(id);
                if (pagamento == null)
                {
                    _logger.LogWarning(Logging.PagamentoEventIds.PagamentoNaoEncontrado,
                        "Pagamento não encontrado para processamento via API. ID: {PagamentoId}", id);
                    return NotFound();
                }

                _logger.LogInformation(Logging.PagamentoEventIds.PagamentoProcessado,
                    "Pagamento processado com sucesso via API. ID: {PagamentoId}", id);

                return Ok(pagamento);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(Logging.PagamentoEventIds.InvalidOperation, ex,
                    "Operação inválida ao processar pagamento ID {PagamentoId}: {MensagemErro}",
                    id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(Logging.PagamentoEventIds.ErroInterno, ex,
                    "Erro interno ao processar pagamento ID {PagamentoId}: {MensagemErro}",
                    id, ex.Message);

                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Cancela um pagamento
        /// </summary>
        /// <param name="id">ID do pagamento</param>
        /// <returns>Pagamento cancelado</returns>
        [HttpPost]
        [Route("{id:int}/cancelar")]
        [ResponseType(typeof(Payment))]
        public IHttpActionResult Cancelar(int id)
        {
            try
            {
                _logger.LogInformation(Logging.PagamentoEventIds.CancelarPagamento,
                    "Requisição POST recebida para cancelar pagamento ID: {PagamentoId}", id);

                var pagamento = _pagamentoService.CancelarPagamento(id);
                if (pagamento == null)
                {
                    _logger.LogWarning(Logging.PagamentoEventIds.PagamentoNaoEncontrado,
                        "Pagamento não encontrado para cancelamento via API. ID: {PagamentoId}", id);
                    return NotFound();
                }

                _logger.LogInformation(Logging.PagamentoEventIds.PagamentoCancelado,
                    "Pagamento cancelado com sucesso via API. ID: {PagamentoId}", id);

                return Ok(pagamento);
            }
            catch (Exception ex)
            {
                _logger.LogError(Logging.PagamentoEventIds.ErroInterno, ex,
                    "Erro interno ao cancelar pagamento ID {PagamentoId}: {MensagemErro}",
                    id, ex.Message);

                return InternalServerError(ex);
            }
        }
    }
}
