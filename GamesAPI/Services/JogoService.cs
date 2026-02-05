using GamesAPI.Logging;
using GamesAPI.Models;
using System.Xml.Linq;

namespace GamesAPI.Services
{
    public class JogoService : IJogoService
    {
        private static List<Jogo> _jogos = new List<Jogo>();
        private static int _proximoId = 1;
        private readonly ILogger<JogoService> _logger;

        public JogoService(ILogger<JogoService> logger)
        {
            _logger = logger;
            InicializarDados();
        }

        private void InicializarDados()
        {
            try
            {
                _logger.LogInformation("Inicializando dados de jogos...");

                _jogos.Add(new Jogo
                {
                    Id = _proximoId++,
                    Titulo = "The Legend of Zelda: Breath of the Wild",
                    Desenvolvedor = "Nintendo",
                    Genero = "Aventura",
                    AnoLancamento = 2017,
                    Preco = 299.90m,
                    DataCadastro = DateTime.Now.AddDays(-10)
                });

                _jogos.Add(new Jogo
                {
                    Id = _proximoId++,
                    Titulo = "God of War: Ragnarok",
                    Desenvolvedor = "Santa Monica Studio",
                    Genero = "Ação",
                    AnoLancamento = 2022,
                    Preco = 349.90m,
                    DataCadastro = DateTime.Now.AddDays(-5)
                });

                _logger.LogInformation("Dados inicializados com {QuantidadeJogos} jogos", _jogos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(GameEventIds.ExceptionOcorrida, ex,
                    "Erro ao inicializar dados: {MensagemErro}", ex.Message);
                throw;
            }
        }

        public List<Jogo> ObterTodos()
        {
            _logger.LogDebug(GameEventIds.ConsultarTodos,
                "Consultando todos os jogos. Total: {QuantidadeJogos}", _jogos.Count);

            return _jogos;
        }

        public Jogo? ObterPorId(int id)
        {
            _logger.LogDebug(GameEventIds.ConsultarPorId,
                "Consultando jogo pelo ID: {JogoId}", id);

            var jogo = _jogos.FirstOrDefault(j => j.Id == id);

            if (jogo == null)
            {
                _logger.LogWarning(GameEventIds.JogoNaoEncontrado,
                    "Jogo com ID {JogoId} não encontrado", id);
            }
            else
            {
                _logger.LogInformation(GameEventIds.JogoEncontrado,
                    "Jogo encontrado: {JogoTitulo} (ID: {JogoId})", jogo.Titulo, id);
            }

            return jogo;
        }

        public Jogo Adicionar(Jogo jogo)
        {
            _logger.LogInformation(GameEventIds.CriarJogo,
                "Criando novo jogo: {JogoTitulo}", jogo.Titulo);

            try
            {
                if (string.IsNullOrWhiteSpace(jogo.Titulo))
                {
                    _logger.LogWarning(GameEventIds.ValidacaoFalhou,
                        "Tentativa de criar jogo sem título");
                    throw new ArgumentException("Título do jogo é obrigatório");
                }

                jogo.Id = _proximoId++;
                jogo.DataCadastro = DateTime.Now;
                _jogos.Add(jogo);

                _logger.LogInformation(GameEventIds.JogoCriado,
                    "Jogo criado com sucesso: ID={JogoId}, Título={JogoTitulo}, Preço={JogoPreco:C}",
                    jogo.Id, jogo.Titulo, jogo.Preco);

                return jogo;
            }
            catch (Exception ex)
            {
                _logger.LogError(GameEventIds.ExceptionOcorrida, ex,
                    "Erro ao criar jogo {JogoTitulo}: {MensagemErro}",
                    jogo.Titulo, ex.Message);
                throw;
            }
        }

        public Jogo? Atualizar(int id, Jogo jogoAtualizado)
        {
            _logger.LogInformation(GameEventIds.AtualizarJogo,
                "Atualizando jogo ID: {JogoId}", id);

            try
            {
                var jogoExistente = ObterPorId(id);
                if (jogoExistente == null)
                {
                    _logger.LogWarning(GameEventIds.JogoNaoEncontradoParaAtualizar,
                        "Jogo não encontrado para atualização. ID: {JogoId}", id);
                    return null;
                }

                if (string.IsNullOrWhiteSpace(jogoAtualizado.Titulo))
                {
                    _logger.LogWarning(GameEventIds.ValidacaoFalhou,
                        "Tentativa de atualizar jogo ID {JogoId} sem título", id);
                    throw new ArgumentException("Título do jogo é obrigatório");
                }

                // Log das alterações
                LogAlteracoes(jogoExistente, jogoAtualizado);

                jogoExistente.Titulo = jogoAtualizado.Titulo;
                jogoExistente.Desenvolvedor = jogoAtualizado.Desenvolvedor;
                jogoExistente.Genero = jogoAtualizado.Genero;
                jogoExistente.AnoLancamento = jogoAtualizado.AnoLancamento;
                jogoExistente.Preco = jogoAtualizado.Preco;

                _logger.LogInformation(GameEventIds.JogoAtualizado,
                    "Jogo atualizado com sucesso: ID={JogoId}, Título={JogoTitulo}",
                    id, jogoAtualizado.Titulo);

                return jogoExistente;
            }
            catch (Exception ex)
            {
                _logger.LogError(GameEventIds.ExceptionOcorrida, ex,
                    "Erro ao atualizar jogo ID {JogoId}: {MensagemErro}",
                    id, ex.Message);
                throw;
            }
        }

        private void LogAlteracoes(Jogo antigo, Jogo novo)
        {
            var alteracoes = new List<string>();

            if (antigo.Titulo != novo.Titulo)
                alteracoes.Add($"Título: '{antigo.Titulo}' → '{novo.Titulo}'");

            if (antigo.Preco != novo.Preco)
                alteracoes.Add($"Preço: {antigo.Preco:C} → {novo.Preco:C}");

            if (alteracoes.Any())
            {
                _logger.LogDebug("Alterações detectadas: {Alteracoes}",
                    string.Join(" | ", alteracoes));
            }
        }

        public bool Remover(int id)
        {
            _logger.LogInformation(GameEventIds.RemoverJogo,
                "Removendo jogo ID: {JogoId}", id);

            try
            {
                var jogo = ObterPorId(id);
                if (jogo == null)
                {
                    _logger.LogWarning(GameEventIds.JogoNaoEncontradoParaRemover,
                        "Jogo não encontrado para remoção. ID: {JogoId}", id);
                    return false;
                }

                _jogos.Remove(jogo);

                _logger.LogInformation(GameEventIds.JogoRemovido,
                    "Jogo removido com sucesso: ID={JogoId}, Título={JogoTitulo}",
                    id, jogo.Titulo);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(GameEventIds.ExceptionOcorrida, ex,
                    "Erro ao remover jogo ID {JogoId}: {MensagemErro}",
                    id, ex.Message);
                throw;
            }
        }
    }
}

