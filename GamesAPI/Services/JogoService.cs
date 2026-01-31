using GamesAPI.Models;
using System.Xml.Linq;

namespace GamesAPI.Services
{
    public class JogoService : IJogoService
    {
        private static List<Jogo> _jogos = new List<Jogo>();
        private static int _proximoId = 1;

        static JogoService()
        {
            // Dados iniciais para teste
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
        }

        public List<Jogo> ObterTodos()
        {
            return _jogos;
        }

        public Jogo? ObterPorId(int id)
        {
            return _jogos.FirstOrDefault(j => j.Id == id);
        }

        public Jogo Adicionar(Jogo jogo)
        {
            jogo.Id = _proximoId++;
            jogo.DataCadastro = DateTime.Now;
            _jogos.Add(jogo);
            return jogo;
        }

        public Jogo? Atualizar(int id, Jogo jogoAtualizado)
        {
            var jogoExistente = ObterPorId(id);
            if (jogoExistente == null)
                return null;

            jogoExistente.Titulo = jogoAtualizado.Titulo;
            jogoExistente.Desenvolvedor = jogoAtualizado.Desenvolvedor;
            jogoExistente.Genero = jogoAtualizado.Genero;
            jogoExistente.AnoLancamento = jogoAtualizado.AnoLancamento;
            jogoExistente.Preco = jogoAtualizado.Preco;

            return jogoExistente;
        }

        public bool Remover(int id)
        {
            var jogo = ObterPorId(id);
            if (jogo == null)
                return false;

            _jogos.Remove(jogo);
            return true;
        }
    }
}
