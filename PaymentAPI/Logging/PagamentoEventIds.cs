// Logging/PagamentoEventIds.cs
using Microsoft.Extensions.Logging;

namespace PaymentAPI.Logging
{
    public static class PagamentoEventIds
    {
        // Operações GET
        public static readonly EventId ConsultarTodos = new EventId(100, "Consultar Todos Pagamentos");
        public static readonly EventId ConsultarPorId = new EventId(101, "Consultar Pagamento por ID");
        public static readonly EventId PagamentoNaoEncontrado = new EventId(102, "Pagamento Não Encontrado");
        public static readonly EventId PagamentoEncontrado = new EventId(103, "Pagamento Encontrado");

        // Operações POST
        public static readonly EventId CriarPagamento = new EventId(200, "Criar Novo Pagamento");
        public static readonly EventId PagamentoCriado = new EventId(201, "Pagamento Criado com Sucesso");
        public static readonly EventId ValidacaoFalhou = new EventId(202, "Validação do Pagamento Falhou");

        // Operações PUT
        public static readonly EventId AtualizarPagamento = new EventId(300, "Atualizar Pagamento");
        public static readonly EventId PagamentoAtualizado = new EventId(301, "Pagamento Atualizado");
        public static readonly EventId PagamentoNaoEncontradoParaAtualizar = new EventId(302, "Pagamento Não Encontrado para Atualização");

        // Operações DELETE
        public static readonly EventId RemoverPagamento = new EventId(400, "Remover Pagamento");
        public static readonly EventId PagamentoRemovido = new EventId(401, "Pagamento Removido");
        public static readonly EventId PagamentoNaoEncontradoParaRemover = new EventId(402, "Pagamento Não Encontrado para Remoção");

        // Operações especiais
        public static readonly EventId ProcessarPagamento = new EventId(500, "Processar Pagamento");
        public static readonly EventId PagamentoProcessado = new EventId(501, "Pagamento Processado");
        public static readonly EventId CancelarPagamento = new EventId(600, "Cancelar Pagamento");
        public static readonly EventId PagamentoCancelado = new EventId(601, "Pagamento Cancelado");

        // Erros e Exceptions
        public static readonly EventId ErroInterno = new EventId(900, "Erro Interno do Servidor");
        public static readonly EventId ExceptionOcorrida = new EventId(901, "Exception Ocorrida");
        public static readonly EventId InvalidOperation = new EventId(902, "Operação Inválida");

        // Outros
        public static readonly EventId RequisicaoRecebida = new EventId(1000, "Requisição Recebida");
        public static readonly EventId RequisicaoConcluida = new EventId(1001, "Requisição Concluída");
        public static readonly EventId ModelStateInvalido = new EventId(1002, "ModelState Inválido");
    }
}