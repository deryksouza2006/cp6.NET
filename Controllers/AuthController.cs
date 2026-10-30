using CP6_CatalogoJogos.DTOs;
using CP6_CatalogoJogos.Models;
using CP6_CatalogoJogos.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CP6_CatalogoJogos.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly TokenService _tokenService;

    public AuthController(TokenService tokenService)
    {
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public ActionResult<LoginResponse> Login(LoginRequest request)
    {
        // Credenciais fixas exigidas apenas para esta demonstração acadêmica.
        if (request.Username != "Mauricio" || request.Password != "123456")
            return Unauthorized(new { mensagem = "Usuário ou senha inválidos." });

        return Ok(_tokenService.GerarToken(request.Username));
    }
}
