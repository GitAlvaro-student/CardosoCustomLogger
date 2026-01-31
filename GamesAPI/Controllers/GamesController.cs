using GamesAPI.Models;
using GamesAPI.Services;
using Microsoft.AspNetCore.Mvc;

namespace GamesAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GamesController : ControllerBase
    {
        private readonly IJogoService _jogoService;

        public GamesController(IJogoService jogoService)
        {
            _jogoService = jogoService;
        }

        // GET: api/jogos
        [HttpGet]
        [ProducesResponseType(typeof(List<Jogo>), 200)]
        public IActionResult ObterTodos()
        {
            var jogos = _jogoService.ObterTodos();
            return Ok(jogos);
        }

        // GET: api/jogos/{id}
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Jogo), 200)]
        [ProducesResponseType(404)]
        public IActionResult ObterPorId(int id)
        {
            var jogo = _jogoService.ObterPorId(id);
            if (jogo == null)
                return NotFound($"Jogo com ID {id} não encontrado.");

            return Ok(jogo);
        }

        // POST: api/jogos
        [HttpPost]
        [ProducesResponseType(typeof(Jogo), 201)]
        [ProducesResponseType(400)]
        public IActionResult Adicionar([FromBody] Jogo jogo)
        {
            if (jogo == null)
                return BadRequest("Dados do jogo inválidos.");

            if (string.IsNullOrWhiteSpace(jogo.Titulo))
                return BadRequest("O título do jogo é obrigatório.");

            var novoJogo = _jogoService.Adicionar(jogo);
            return CreatedAtAction(nameof(ObterPorId), new { id = novoJogo.Id }, novoJogo);
        }

        // PUT: api/jogos/{id}
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Jogo), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public IActionResult Atualizar(int id, [FromBody] Jogo jogo)
        {
            if (jogo == null)
                return BadRequest("Dados do jogo inválidos.");

            if (string.IsNullOrWhiteSpace(jogo.Titulo))
                return BadRequest("O título do jogo é obrigatório.");

            var jogoAtualizado = _jogoService.Atualizar(id, jogo);
            if (jogoAtualizado == null)
                return NotFound($"Jogo com ID {id} não encontrado.");

            return Ok(jogoAtualizado);
        }

        // DELETE: api/jogos/{id}
        [HttpDelete("{id}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        public IActionResult Remover(int id)
        {
            var removido = _jogoService.Remover(id);
            if (!removido)
                return NotFound($"Jogo com ID {id} não encontrado.");

            return NoContent();
        }
    }
}
