using Microsoft.AspNetCore.Mvc;
using SeatSync.Core.DTOs;
using SeatSync.Core.Interfaces;

namespace SeatSync.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var result = await _authService.RegisterAsync(request);
        if (!result.Success)
        {
            return BadRequest(new { error = result.ErrorMessage });
        }

        return Ok(new { token = result.Token });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);
        if (!result.Success)
        {
            return Unauthorized(new { error = result.ErrorMessage });
        }

        return Ok(new { token = result.Token });
    }
}