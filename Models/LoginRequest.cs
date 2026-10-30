using System.ComponentModel.DataAnnotations;

namespace CP6_CatalogoJogos.Models;

public class LoginRequest
{
    [Required(ErrorMessage = "Informe o usuário.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a senha.")]
    public string Password { get; set; } = string.Empty;
}
