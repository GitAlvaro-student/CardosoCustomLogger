namespace GamesAPI.Logging
{
    public class GameEventIds
    {
        // Operações GET
        public static readonly EventId ConsultarTodos = new EventId(100, "Consultar Todos Jogos");
        public static readonly EventId ConsultarPorId = new EventId(101, "Consultar Jogo por ID");
        public static readonly EventId JogoNaoEncontrado = new EventId(102, "Jogo Não Encontrado");
        public static readonly EventId JogoEncontrado = new EventId(103, "Jogo Encontrado");

        // Operações POST
        public static readonly EventId CriarJogo = new EventId(200, "Criar Novo Jogo");
        public static readonly EventId JogoCriado = new EventId(201, "Jogo Criado com Sucesso");
        public static readonly EventId ValidacaoFalhou = new EventId(202, "Validação do Jogo Falhou");

        // Operações PUT
        public static readonly EventId AtualizarJogo = new EventId(300, "Atualizar Jogo");
        public static readonly EventId JogoAtualizado = new EventId(301, "Jogo Atualizado");
        public static readonly EventId JogoNaoEncontradoParaAtualizar = new EventId(302, "Jogo Não Encontrado para Atualização");

        // Operações DELETE
        public static readonly EventId RemoverJogo = new EventId(400, "Remover Jogo");
        public static readonly EventId JogoRemovido = new EventId(401, "Jogo Removido");
        public static readonly EventId JogoNaoEncontradoParaRemover = new EventId(402, "Jogo Não Encontrado para Remoção");

        // Erros e Exceptions
        public static readonly EventId ErroInterno = new EventId(500, "Erro Interno do Servidor");
        public static readonly EventId ExceptionOcorrida = new EventId(501, "Exception Ocorrida");

        // Outros
        public static readonly EventId RequisicaoRecebida = new EventId(600, "Requisição Recebida");
        public static readonly EventId RequisicaoConcluida = new EventId(601, "Requisição Concluída");
    }
}
