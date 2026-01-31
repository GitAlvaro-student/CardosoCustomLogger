namespace GamesAPI.Models
{
    public class Jogo
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Desenvolvedor { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public int AnoLancamento { get; set; }
        public decimal Preco { get; set; }
        public DateTime DataCadastro { get; set; }
    }
}
