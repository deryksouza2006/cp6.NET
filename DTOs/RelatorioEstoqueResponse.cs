namespace CP6_CatalogoJogos.DTOs;

public record RelatorioEstoqueResponse(
    string Plataforma,
    int QuantidadeTitulos,
    decimal ValorTotalEstoque);
