using CP6_CatalogoJogos.DTOs;
using CP6_CatalogoJogos.Models;
using CP6_CatalogoJogos.Repositories;

namespace CP6_CatalogoJogos.Services;

public class JogoService
{
    private readonly IJogoRepository _repository;

    public JogoService(IJogoRepository repository)
    {
        _repository = repository;
    }

    public List<Jogo> Listar() => _repository.Listar();
    public Jogo? BuscarPorId(int id) => _repository.BuscarPorId(id);
    public Jogo Adicionar(JogoRequest request) => _repository.Adicionar(Converter(request));
    public Jogo? Atualizar(int id, JogoRequest request) => _repository.Atualizar(id, Converter(request));
    public bool Excluir(int id) => _repository.Excluir(id);

    public List<Jogo> Buscar(string plataforma, decimal precoMaximo) =>
        _repository.Listar()
            .Where(j => string.Equals(j.Plataforma, plataforma.Trim(), StringComparison.OrdinalIgnoreCase)
                && j.Preco <= precoMaximo)
            .ToList();

    public List<RelatorioEstoqueResponse> RelatorioEstoque() =>
        _repository.Listar()
            .GroupBy(j => j.Plataforma, StringComparer.OrdinalIgnoreCase)
            .Select(grupo => new RelatorioEstoqueResponse(
                grupo.Key,
                grupo.Count(),
                grupo.Sum(j => j.Preco * j.Estoque)))
            .OrderBy(item => item.Plataforma)
            .ToList();

    private static Jogo Converter(JogoRequest request) => new()
    {
        Titulo = request.Titulo.Trim(),
        Plataforma = request.Plataforma.Trim(),
        Genero = request.Genero.Trim(),
        Preco = request.Preco,
        AnoLancamento = request.AnoLancamento,
        Estoque = request.Estoque
    };
}
