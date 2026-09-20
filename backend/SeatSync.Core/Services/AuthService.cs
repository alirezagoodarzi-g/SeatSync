using SeatSync.Core.DTOs;
using SeatSync.Core.Entities;
using SeatSync.Core.Interfaces;

namespace SeatSync.Core.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(IUserRepository userRepository, ITokenService tokenService, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthResult> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.EmailExistsAsync(request.Email))
        {
            return new AuthResult(false, null, "Email already registered.");
        }

        var user = new User
        {
            Email = request.Email,
            DisplayName = request.DisplayName,
            Role = request.Role,
            PasswordHash = _passwordHasher.Hash(request.Password)
        };

        await _userRepository.CreateAsync(user);

        var token = _tokenService.GenerateToken(user);
        return new AuthResult(true, token, null);
    }

    public async Task<AuthResult> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return new AuthResult(false, null, "Invalid email or password.");
        }

        var token = _tokenService.GenerateToken(user);
        return new AuthResult(true, token, null);
    }
}