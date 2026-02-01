using GamesAPI.Models;

namespace GamesAPI.Services
{
    public interface IJogoService
    {
        List<Jogo> ObterTodos();
        Jogo? ObterPorId(int id);
        Jogo Adicionar(Jogo jogo);
        Jogo? Atualizar(int id, Jogo jogo);
        bool Remover(int id);
    }
}
