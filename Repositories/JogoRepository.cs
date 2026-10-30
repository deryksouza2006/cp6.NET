using CP6_CatalogoJogos.Models;

namespace CP6_CatalogoJogos.Repositories;

public class JogoRepository : IJogoRepository
{
    private readonly object _sincronizacao = new();
    private readonly List<Jogo> _jogos = new()
    {
        new() { Id = 1, Titulo = "Gran Turismo 7", Plataforma = "PlayStation 5", Genero = "Corrida", Preco = 249.90m, AnoLancamento = 2022, Estoque = 5 },
        new() { Id = 2, Titulo = "The Legend of Zelda: Tears of the Kingdom", Plataforma = "Nintendo Switch", Genero = "Aventura", Preco = 299.90m, AnoLancamento = 2023, Estoque = 4 },
        new() { Id = 3, Titulo = "Forza Horizon 5", Plataforma = "Xbox Series X", Genero = "Corrida", Preco = 199.90m, AnoLancamento = 2021, Estoque = 6 },
        new() { Id = 4, Titulo = "Stardew Valley", Plataforma = "PC", Genero = "Simulação", Preco = 24.90m, AnoLancamento = 2016, Estoque = 10 },
        new() { Id = 5, Titulo = "God of War", Plataforma = "PlayStation 4", Genero = "Ação", Preco = 99.90m, AnoLancamento = 2018, Estoque = 3 }
    };
    private int _proximoId = 6;

    public List<Jogo> Listar()
    {
        lock (_sincronizacao)
        {
            return _jogos.Select(Copiar).ToList();
        }
    }

    public Jogo? BuscarPorId(int id)
    {
        lock (_sincronizacao)
        {
            var jogo = _jogos.FirstOrDefault(j => j.Id == id);
            return jogo is null ? null : Copiar(jogo);
        }
    }

    public Jogo Adicionar(Jogo jogo)
    {
        lock (_sincronizacao)
        {
            var novoJogo = Copiar(jogo);
            novoJogo.Id = _proximoId++;
            _jogos.Add(novoJogo);
            return Copiar(novoJogo);
        }
    }

    public Jogo? Atualizar(int id, Jogo jogo)
    {
        lock (_sincronizacao)
        {
            var indice = _jogos.FindIndex(j => j.Id == id);
            if (indice < 0) return null;

            var atualizado = Copiar(jogo);
            atualizado.Id = id;
            _jogos[indice] = atualizado;
            return Copiar(atualizado);
        }
    }

    public bool Excluir(int id)
    {
        lock (_sincronizacao)
        {
            return _jogos.RemoveAll(j => j.Id == id) > 0;
        }
    }

    // As cópias evitam alterações na lista fora do lock do Singleton.
    private static Jogo Copiar(Jogo jogo) => new()
    {
        Id = jogo.Id,
        Titulo = jogo.Titulo,
        Plataforma = jogo.Plataforma,
        Genero = jogo.Genero,
        Preco = jogo.Preco,
        AnoLancamento = jogo.AnoLancamento,
        Estoque = jogo.Estoque
    };
}
