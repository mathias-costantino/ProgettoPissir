using System.ComponentModel.DataAnnotations;

namespace SharingMezzi.Core.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Il nome è obbligatorio")]
    [StringLength(100, ErrorMessage = "Il nome non può superare i 100 caratteri")]
    public string Nome { get; set; } = string.Empty;

    [Required(ErrorMessage = "Il cognome è obbligatorio")]
    [StringLength(100, ErrorMessage = "Il cognome non può superare i 100 caratteri")]
    public string Cognome { get; set; } = string.Empty;

    [Required(ErrorMessage = "L'email è obbligatoria")]
    [EmailAddress(ErrorMessage = "Formato email non valido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La password è obbligatoria")]
    [MinLength(6, ErrorMessage = "La password deve essere di almeno 6 caratteri")]
    public string Password { get; set; } = string.Empty;

    [Compare("Password", ErrorMessage = "Le password non corrispondono")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Formato telefono non valido")]
    public string? Telefono { get; set; }
}
