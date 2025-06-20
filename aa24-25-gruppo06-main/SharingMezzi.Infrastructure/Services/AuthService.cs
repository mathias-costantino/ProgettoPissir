using Microsoft.EntityFrameworkCore;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Core.Entities;
using SharingMezzi.Infrastructure.Database;
using System.Security.Cryptography;
using System.Text;

namespace SharingMezzi.Infrastructure.Services;

public interface IAuthService
{
    Task<AuthResultDto> LoginAsync(LoginDto loginDto);
    Task<AuthResultDto> RegisterAsync(RegisterDto registerDto);
    Task<AuthResultDto> RefreshTokenAsync(string token, string refreshToken);
    Task<bool> RevokeTokenAsync(string refreshToken);
    Task<UtenteDto?> GetCurrentUserAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly SharingMezziContext _context;
    private readonly IJwtService _jwtService;

    public AuthService(SharingMezziContext context, IJwtService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<AuthResultDto> LoginAsync(LoginDto loginDto)
    {
        try
        {
            var utente = await _context.Utenti
                .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower());

            if (utente == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Email o password non corretti"
                };
            }

            // Verifica password
            if (!VerifyPassword(loginDto.Password, utente.Password))
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Email o password non corretti"
                };
            }

            // Genera token
            var accessToken = _jwtService.GenerateAccessToken(utente);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Salva refresh token nel database
            utente.RefreshToken = refreshToken;
            utente.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var utenteDto = new UtenteDto
            {
                Id = utente.Id,
                Nome = utente.Nome,
                Cognome = utente.Cognome,
                Email = utente.Email,
                Telefono = utente.Telefono,
                Ruolo = utente.Ruolo.ToString(),
                DataRegistrazione = utente.DataRegistrazione,
                Credito = utente.Credito,
                PuntiEco = utente.PuntiEco,
                Stato = utente.Stato.ToString(),
                DataSospensione = utente.DataSospensione,
                MotivoSospensione = utente.MotivoSospensione
            };

            return new AuthResultDto
            {
                Success = true,
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                Message = "Login effettuato con successo",
                User = utenteDto
            };
        }
        catch (Exception ex)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = $"Errore durante il login: {ex.Message}"
            };
        }
    }

    public async Task<AuthResultDto> RegisterAsync(RegisterDto registerDto)
    {
        try
        {
            // Verifica se l'email esiste già
            var existingUser = await _context.Utenti
                .FirstOrDefaultAsync(u => u.Email.ToLower() == registerDto.Email.ToLower());

            if (existingUser != null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Un utente con questa email è già registrato"
                };
            }

            // Crea nuovo utente
            var nuovoUtente = new Utente
            {
                Nome = registerDto.Nome.Trim(),
                Cognome = registerDto.Cognome.Trim(),
                Email = registerDto.Email.ToLower().Trim(),
                Password = HashPassword(registerDto.Password),
                Telefono = registerDto.Telefono?.Trim(),
                Ruolo = RuoloUtente.Utente,
                DataRegistrazione = DateTime.UtcNow
            };

            _context.Utenti.Add(nuovoUtente);
            await _context.SaveChangesAsync();

            // Genera token
            var accessToken = _jwtService.GenerateAccessToken(nuovoUtente);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Salva refresh token
            nuovoUtente.RefreshToken = refreshToken;
            nuovoUtente.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            var utenteDto = new UtenteDto
            {
                Id = nuovoUtente.Id,
                Nome = nuovoUtente.Nome,
                Cognome = nuovoUtente.Cognome,
                Email = nuovoUtente.Email,
                Telefono = nuovoUtente.Telefono,
                Ruolo = nuovoUtente.Ruolo.ToString(),
                DataRegistrazione = nuovoUtente.DataRegistrazione,
                Credito = nuovoUtente.Credito,
                PuntiEco = nuovoUtente.PuntiEco,
                Stato = nuovoUtente.Stato.ToString(),
                DataSospensione = nuovoUtente.DataSospensione,
                MotivoSospensione = nuovoUtente.MotivoSospensione
            };

            return new AuthResultDto
            {
                Success = true,
                Token = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                Message = "Registrazione completata con successo",
                User = utenteDto
            };
        }
        catch (Exception ex)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = $"Errore durante la registrazione: {ex.Message}"
            };
        }
    }

    public async Task<AuthResultDto> RefreshTokenAsync(string token, string refreshToken)
    {
        try
        {
            var principal = _jwtService.GetPrincipalFromExpiredToken(token);
            if (principal == null)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Token non valido"
                };
            }

            var userIdClaim = principal.FindFirst("user_id")?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Token non valido"
                };
            }

            var utente = await _context.Utenti.FindAsync(userId);
            if (utente == null || utente.RefreshToken != refreshToken || utente.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return new AuthResultDto
                {
                    Success = false,
                    Message = "Refresh token non valido o scaduto"
                };
            }

            var newAccessToken = _jwtService.GenerateAccessToken(utente);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            utente.RefreshToken = newRefreshToken;
            utente.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return new AuthResultDto
            {
                Success = true,
                Token = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(60),
                Message = "Token rinnovato con successo"
            };
        }
        catch (Exception ex)
        {
            return new AuthResultDto
            {
                Success = false,
                Message = $"Errore durante il refresh del token: {ex.Message}"
            };
        }
    }

    public async Task<bool> RevokeTokenAsync(string refreshToken)
    {
        try
        {
            var utente = await _context.Utenti
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

            if (utente == null)
                return false;

            utente.RefreshToken = null;
            utente.RefreshTokenExpiryTime = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UtenteDto?> GetCurrentUserAsync(int userId)
    {
        try
        {
            var utente = await _context.Utenti.FindAsync(userId);
            if (utente == null)
                return null;

            return new UtenteDto
            {
                Id = utente.Id,
                Nome = utente.Nome,
                Cognome = utente.Cognome,
                Email = utente.Email,
                Telefono = utente.Telefono,
                Ruolo = utente.Ruolo.ToString(),
                DataRegistrazione = utente.DataRegistrazione,
                Credito = utente.Credito,
                PuntiEco = utente.PuntiEco,
                Stato = utente.Stato.ToString(),
                DataSospensione = utente.DataSospensione,
                MotivoSospensione = utente.MotivoSospensione
            };
        }
        catch
        {
            return null;
        }
    }

    private static string HashPassword(string password)
    {
        using var sha256 = SHA256.Create();
        var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }

    private static bool VerifyPassword(string password, string hashedPassword)
    {
        var hashedInput = HashPassword(password);
        return hashedInput == hashedPassword;
    }
}
