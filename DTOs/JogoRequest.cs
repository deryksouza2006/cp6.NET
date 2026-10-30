using System.ComponentModel.DataAnnotations;

namespace CP6_CatalogoJogos.DTOs;

// O cliente informa os dados; somente o repositório define o Id.
public class JogoRequest : IValidatableObject
{
    [Required(ErrorMessage = "Informe o título.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe a plataforma.")]
    public string Plataforma { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe o gênero.")]
    public string Genero { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335", ErrorMessage = "O preço não pode ser negativo.")]
    public decimal Preco { get; set; }

    public int AnoLancamento { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "O estoque não pode ser negativo.")]
    public int Estoque { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var anoMaximo = DateTime.UtcNow.Year + 1;
        if (AnoLancamento < 1950 || AnoLancamento > anoMaximo)
        {
            yield return new ValidationResult(
                $"O ano de lançamento deve estar entre 1950 e {anoMaximo}.",
                new[] { nameof(AnoLancamento) });
        }
    }
}
