using SeatSync.Core.DTOs;

namespace SeatSync.Core.Interfaces;

public interface IAuthService
{
    Task<AuthResult> RegisterAsync(RegisterRequest request);
    Task<AuthResult> LoginAsync(LoginRequest request);
}