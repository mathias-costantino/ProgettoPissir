using System.ComponentModel.DataAnnotations;

namespace SharingMezzi.Core.DTOs;

public class LoginDto
{
    [Required(ErrorMessage = "L'email è obbligatoria")]
    [EmailAddress(ErrorMessage = "Formato email non valido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La password è obbligatoria")]
    [MinLength(6, ErrorMessage = "La password deve essere di almeno 6 caratteri")]
    public string Password { get; set; } = string.Empty;
}
