using CP6_CatalogoJogos.Models;

namespace CP6_CatalogoJogos.Repositories;

public interface IJogoRepository
{
    List<Jogo> Listar();
    Jogo? BuscarPorId(int id);
    Jogo Adicionar(Jogo jogo);
    Jogo? Atualizar(int id, Jogo jogo);
    bool Excluir(int id);
}
