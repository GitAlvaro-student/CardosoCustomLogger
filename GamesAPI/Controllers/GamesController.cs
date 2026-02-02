using GamesAPI.Logging;
using GamesAPI.Models;
using GamesAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GamesAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JogosController : ControllerBase
    {
        private readonly IJogoService _jogoService;
        private readonly ILogger<JogosController> _logger;

        public JogosController(IJogoService jogoService, ILogger<JogosController> logger)
        {
            _jogoService = jogoService;
            _logger = logger;
        }

        // GET: api/jogos
        [HttpGet]
        [ProducesResponseType(typeof(List<Jogo>), 200)]
        public IActionResult ObterTodos()
        {
            try
            {
                _logger.LogDebug(GameEventIds.ConsultarTodos,
                    "Iniciando consulta de todos os jogos");

                var jogos = _jogoService.ObterTodos();

                _logger.LogInformation(GameEventIds.ConsultarTodos,
                    "Consulta retornou {QuantidadeJogos} jogos", jogos.Count);

                return Ok(jogos);
            }
            catch (Exception ex)
            {
                _logger.LogError(GameEventIds.ErroInterno, ex,
                    "Erro interno ao consultar jogos: {MensagemErro}", ex.Message);

                return StatusCode(500, new
                {
                    Mensagem = "Ocorreu um erro interno no servidor",
                    Detalhes = ex.Message
                });
            }
        }

        // GET: api/jogos/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Jogo), 200)]
        [ProducesResponseType(404)]
        public IActionResult ObterPorId(int id)
        {
            try
            {
                _logger.LogDebug(GameEventIds.ConsultarPorId,
                    "Iniciando consulta do jogo ID: {JogoId}", id);

                var jogo = _jogoService.ObterPorId(id);

                if (jogo == null)
                {
                    _logger.LogWarning(GameEventIds.JogoNaoEncontrado,
                        "Jogo não encontrado na controller. ID: {JogoId}", id);
                    return NotFound($"Jogo com ID {id} não encontrado.");
                }

                _logger.LogInformation(GameEventIds.JogoEncontrado,
                    "Retornando jogo: {JogoTitulo} (ID: {JogoId})", jogo.Titulo, id);

                return Ok(jogo);
            }
            catch (Exception ex)
            {
                _logger.LogError(GameEventIds.ErroInterno, ex,
                    "Erro interno ao consultar jogo ID {JogoId}: {MensagemErro}",
                    id, ex.Message);

                return StatusCode(500, new
                {
                    Mensagem = "Ocorreu um erro interno no servidor",
                    Detalhes = ex.Message
                });
            }
        }

        // POST: api/jogos
        [HttpPost]
        [ProducesResponseType(typeof(Jogo), 201)]
        [ProducesResponseType(400)]
        public IActionResult Adicionar([FromBody] Jogo jogo)
        {
            try
            {
                _logger.LogInformation(GameEventIds.CriarJogo,
                    "Recebida requisição POST para criar jogo: {JogoTitulo}",
                    jogo?.Titulo ?? "N/A");

                if (jogo == null)
                {
                    _logger.LogWarning(GameEventIds.ValidacaoFalhou,
                        "Requisição POST com corpo vazio");
                    return BadRequest("Dados do jogo inválidos.");
                }

                if (string.IsNullOrWhiteSpace(jogo.Titulo))
                {
                    _logger.LogWarning(GameEventIds.ValidacaoFalhou,
                        "Tentativa de criar jogo sem título");
                    return BadRequest("O título do jogo é obrigatório.");
                }

                if (jogo.Preco <= 0)
                {
                    _logger.LogWarning(GameEventIds.ValidacaoFalhou,
                        "Tentativa de criar jogo com preço inválido: {JogoPreco}", jogo.Preco);
                    return BadRequest("O preço do jogo deve ser maior que zero.");
                }

                var novoJogo = _jogoService.Adicionar(jogo);

                _logger.LogInformation(GameEventIds.JogoCriado,
                    "Jogo criado com sucesso via API. ID: {JogoId}", novoJogo.Id);

                return CreatedAtAction(nameof(ObterPorId),
                    new { id = novoJogo.Id },
                    novoJogo);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(GameEventIds.ValidacaoFalhou, ex,
                    "Argumento inválido ao criar jogo: {MensagemErro}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(GameEventIds.ErroInterno, ex,
                    "Erro interno ao criar jogo: {MensagemErro}", ex.Message);

                return StatusCode(500, new
                {
                    Mensagem = "Ocorreu um erro interno ao criar o jogo",
                    Detalhes = ex.Message
                });
            }
        }

        // PUT: api/jogos/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Jogo), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public IActionResult Atualizar(int id, [FromBody] Jogo jogo)
        {
            try
            {
                _logger.LogInformation(GameEventIds.AtualizarJogo,
                    "Recebida requisição PUT para atualizar jogo ID: {JogoId}", id);

                if (jogo == null)
                {
                    _logger.LogWarning(GameEventIds.ValidacaoFalhou,
                        "Requisição PUT para ID {JogoId} com corpo vazio", id);
                    return BadRequest("Dados do jogo inválidos.");
                }

                if (string.IsNullOrWhiteSpace(jogo.Titulo))
                {
                    _logger.LogWarning(GameEventIds.ValidacaoFalhou,
                        "Tentativa de atualizar jogo ID {JogoId} sem título", id);
                    return BadRequest("O título do jogo é obrigatório.");
                }

                var jogoAtualizado = _jogoService.Atualizar(id, jogo);

                if (jogoAtualizado == null)
                {
                    _logger.LogWarning(GameEventIds.JogoNaoEncontradoParaAtualizar,
                        "Jogo não encontrado para atualização via API. ID: {JogoId}", id);
                    return NotFound($"Jogo com ID {id} não encontrado.");
                }

                _logger.LogInformation(GameEventIds.JogoAtualizado,
                    "Jogo atualizado com sucesso via API. ID: {JogoId}", id);

                return Ok(jogoAtualizado);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(GameEventIds.ValidacaoFalhou, ex,
                    "Argumento inválido ao atualizar jogo ID {JogoId}: {MensagemErro}",
                    id, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(GameEventIds.ErroInterno, ex,
                    "Erro interno ao atualizar jogo ID {JogoId}: {MensagemErro}",
                    id, ex.Message);

                return StatusCode(500, new
                {
                    Mensagem = "Ocorreu um erro interno ao atualizar o jogo",
                    Detalhes = ex.Message
                });
            }
        }

        // DELETE: api/jogos/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult Remover(int id)
        {
            try
            {
                _logger.LogInformation(GameEventIds.RemoverJogo,
                    "Recebida requisição DELETE para remover jogo ID: {JogoId}", id);

                var removido = _jogoService.Remover(id);

                if (!removido)
                {
                    _logger.LogWarning(GameEventIds.JogoNaoEncontradoParaRemover,
                        "Jogo não encontrado para remoção via API. ID: {JogoId}", id);
                    return NotFound($"Jogo com ID {id} não encontrado.");
                }

                _logger.LogInformation(GameEventIds.JogoRemovido,
                    "Jogo removido com sucesso via API. ID: {JogoId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(GameEventIds.ErroInterno, ex,
                    "Erro interno ao remover jogo ID {JogoId}: {MensagemErro}",
                    id, ex.Message);

                return StatusCode(500, new
                {
                    Mensagem = "Ocorreu um erro interno ao remover o jogo",
                    Detalhes = ex.Message
                });
            }
        }
    }
}
