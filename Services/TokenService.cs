using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CP6_CatalogoJogos.DTOs;
using Microsoft.IdentityModel.Tokens;

namespace CP6_CatalogoJogos.Services;

public class TokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public LoginResponse GerarToken(string username)
    {
        var agora = DateTime.UtcNow;
        var expiraEm = agora.AddSeconds(_configuration.GetValue<int>("Jwt:ExpirationSeconds", 3600));
        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, username),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            notBefore: agora,
            expires: expiraEm,
            signingCredentials: credenciais);

        return new LoginResponse(new JwtSecurityTokenHandler().WriteToken(token), "Bearer", expiraEm);
    }
}
