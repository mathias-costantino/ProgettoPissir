using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SharingMezzi.Core.DTOs;
using SharingMezzi.Infrastructure.Services;
using System.Security.Claims;

namespace SharingMezzi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    /// <summary>
    /// Login utente con email e password
    /// </summary>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResultDto>> Login([FromBody] LoginDto loginDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResultDto
                {
                    Success = false,
                    Message = "Dati di login non validi",
                });
            }

            var result = await _authService.LoginAsync(loginDto);
            
            if (!result.Success)
            {
                return Unauthorized(result);
            }

            _logger.LogInformation("Login effettuato con successo per {Email}", loginDto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il login per {Email}", loginDto.Email);
            return StatusCode(500, new AuthResultDto
            {
                Success = false,
                Message = "Errore interno del server"
            });
        }
    }

    /// <summary>
    /// Registrazione nuovo utente
    /// </summary>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResultDto>> Register([FromBody] RegisterDto registerDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new AuthResultDto
                {
                    Success = false,
                    Message = "Dati di registrazione non validi",
                });
            }

            var result = await _authService.RegisterAsync(registerDto);
            
            if (!result.Success)
            {
                return BadRequest(result);
            }

            _logger.LogInformation("Registrazione completata per {Email}", registerDto.Email);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la registrazione per {Email}", registerDto.Email);
            return StatusCode(500, new AuthResultDto
            {
                Success = false,
                Message = "Errore interno del server"
            });
        }
    }

    /// <summary>
    /// Rinnova il token di accesso usando il refresh token
    /// </summary>
    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResultDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
    {
        try
        {
            if (string.IsNullOrEmpty(refreshTokenDto.Token) || string.IsNullOrEmpty(refreshTokenDto.RefreshToken))
            {
                return BadRequest(new AuthResultDto
                {
                    Success = false,
                    Message = "Token e refresh token sono obbligatori"
                });
            }

            var result = await _authService.RefreshTokenAsync(refreshTokenDto.Token, refreshTokenDto.RefreshToken);
            
            if (!result.Success)
            {
                return Unauthorized(result);
            }

            _logger.LogInformation("Token rinnovato con successo");
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il refresh del token");
            return StatusCode(500, new AuthResultDto
            {
                Success = false,
                Message = "Errore interno del server"
            });
        }
    }

    /// <summary>
    /// Logout - revoca il refresh token
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult> Logout([FromBody] LogoutDto logoutDto)
    {
        try
        {
            if (string.IsNullOrEmpty(logoutDto.RefreshToken))
            {
                return BadRequest(new { message = "Refresh token obbligatorio" });
            }

            var success = await _authService.RevokeTokenAsync(logoutDto.RefreshToken);
            
            if (!success)
            {
                return BadRequest(new { message = "Impossibile effettuare il logout" });
            }

            _logger.LogInformation("Logout effettuato per utente {UserId}", GetCurrentUserId());
            return Ok(new { message = "Logout effettuato con successo" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il logout");
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Ottieni informazioni dell'utente corrente
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UtenteDto>> GetCurrentUser()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return Unauthorized(new { message = "Token non valido" });
            }

            var user = await _authService.GetCurrentUserAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Utente non trovato" });
            }

            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante il recupero dell'utente corrente");
            return StatusCode(500, new { message = "Errore interno del server" });
        }
    }

    /// <summary>
    /// Verifica se il token è valido
    /// </summary>
    [HttpPost("validate")]
    [Authorize]
    public ActionResult ValidateToken()
    {
        try
        {
            var userId = GetCurrentUserId();
            var userRole = GetCurrentUserRole();
            
            return Ok(new 
            { 
                valid = true, 
                userId = userId,
                role = userRole,
                message = "Token valido" 
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Errore durante la validazione del token");
            return Unauthorized(new { valid = false, message = "Token non valido" });
        }
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("user_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    private string GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value ?? "Utente";
    }
}

public class RefreshTokenDto
{
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
}

public class LogoutDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
