namespace CP6_CatalogoJogos.Models;

public class Jogo
{
    public int Id { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Plataforma { get; set; } = string.Empty;
    public string Genero { get; set; } = string.Empty;
    public decimal Preco { get; set; }
    public int AnoLancamento { get; set; }
    public int Estoque { get; set; }
}
