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

namespace PaymentAPI.Controllers
{
    /// <summary>
    /// API para gerenciamento de pagamentos
    /// </summary>
    [EnableCors(origins: "*", headers: "*", methods: "*")]
    [RoutePrefix("api/pagamentos")]
    public class PagamentosController : ApiController
    {
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
                var pagamentos = _pagamentoService.ObterTodos();
                return Ok(pagamentos);
            }
            catch (Exception ex)
            {
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
                var pagamento = _pagamentoService.ObterPorId(id);
                if (pagamento == null)
                    return NotFound();

                return Ok(pagamento);
            }
            catch (Exception ex)
            {
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
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (request == null)
                    return BadRequest("Dados do pagamento não fornecidos");

                if (request.Valor <= 0)
                    return BadRequest("Valor do pagamento deve ser maior que zero");

                var pagamento = _pagamentoService.Criar(request);

                // Retorna 201 Created com location header
                var location = Url.Link("DefaultApi", new { controller = "pagamentos", id = pagamento.Id });
                return Created(location, pagamento);
            }
            catch (Exception ex)
            {
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
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                if (request == null)
                    return BadRequest("Dados do pagamento não fornecidos");

                var pagamento = _pagamentoService.Atualizar(id, request);
                if (pagamento == null)
                    return NotFound();

                return Ok(pagamento);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
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
                var excluido = _pagamentoService.Excluir(id);
                if (!excluido)
                    return NotFound();

                return Ok(new { message = "Pagamento excluído com sucesso" });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
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
                var pagamento = _pagamentoService.ProcessarPagamento(id);
                if (pagamento == null)
                    return NotFound();

                return Ok(pagamento);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
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
                var pagamento = _pagamentoService.CancelarPagamento(id);
                if (pagamento == null)
                    return NotFound();

                return Ok(pagamento);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }
    }
}
