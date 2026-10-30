using System.ComponentModel.DataAnnotations;
using CP6_CatalogoJogos.DTOs;
using CP6_CatalogoJogos.Models;
using CP6_CatalogoJogos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CP6_CatalogoJogos.Controllers;

[ApiController]
[Route("api/jogos")]
[Authorize]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
public class JogosController : ControllerBase
{
    private readonly JogoService _service;

    public JogosController(JogoService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<Jogo>), StatusCodes.Status200OK)]
    public ActionResult<List<Jogo>> Listar() => Ok(_service.Listar());

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(Jogo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<Jogo> BuscarPorId(int id)
    {
        var jogo = _service.BuscarPorId(id);
        if (jogo is null) return NotFound(new { mensagem = "Jogo não encontrado." });
        return Ok(jogo);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Jogo), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<Jogo> Adicionar(JogoRequest request)
    {
        var jogo = _service.Adicionar(request);
        return CreatedAtAction(nameof(BuscarPorId), new { id = jogo.Id }, jogo);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(Jogo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<Jogo> Atualizar(int id, JogoRequest request)
    {
        var jogo = _service.Atualizar(id, request);
        if (jogo is null) return NotFound(new { mensagem = "Jogo não encontrado." });
        return Ok(jogo);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult Excluir(int id)
    {
        if (!_service.Excluir(id)) return NotFound(new { mensagem = "Jogo não encontrado." });
        return NoContent();
    }

    [HttpGet("busca")]
    [ProducesResponseType(typeof(List<Jogo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<List<Jogo>> Buscar(
        [FromQuery, Required] string plataforma,
        [FromQuery, Required, Range(typeof(decimal), "0", "79228162514264337593543950335")] decimal? precoMaximo)
    {
        return Ok(_service.Buscar(plataforma, precoMaximo!.Value));
    }

    [HttpGet("relatorio-estoque")]
    [ProducesResponseType(typeof(List<RelatorioEstoqueResponse>), StatusCodes.Status200OK)]
    public ActionResult<List<RelatorioEstoqueResponse>> RelatorioEstoque() => Ok(_service.RelatorioEstoque());
}
