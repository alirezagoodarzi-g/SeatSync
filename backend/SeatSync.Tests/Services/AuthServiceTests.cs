using Moq;
using SeatSync.Core.DTOs;
using SeatSync.Core.Entities;
using SeatSync.Core.Interfaces;
using SeatSync.Core.Services;
using Xunit;

namespace SeatSync.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _authService = new AuthService(_userRepository.Object, _tokenService.Object, _passwordHasher.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ReturnsFailure()
    {
        // Arrange
        _userRepository
            .Setup(r => r.EmailExistsAsync("taken@example.com"))
            .ReturnsAsync(true);

        var request = new RegisterRequest("taken@example.com", "SomePassword123!", "Some User", Role.Customer);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Equal("Email already registered.", result.ErrorMessage);

        // The repository should never be asked to create a user in this case
        _userRepository.Verify(r => r.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ReturnsSuccessAndToken()
    {
        // Arrange
        _userRepository
            .Setup(r => r.EmailExistsAsync("new@example.com"))
            .ReturnsAsync(false);

        _userRepository
            .Setup(r => r.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => u);

        _passwordHasher
            .Setup(h => h.Hash(It.IsAny<string>()))
            .Returns("hashed-password");

        _tokenService
            .Setup(t => t.GenerateToken(It.IsAny<User>()))
            .Returns("fake-jwt-token");

        var request = new RegisterRequest("new@example.com", "SomePassword123!", "New User", Role.Customer);

        // Act
        var result = await _authService.RegisterAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.Equal("fake-jwt-token", result.Token);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsFailure()
    {
        // Arrange
        var existingUser = new User
        {
            Email = "user@example.com",
            PasswordHash = "correct-hash"
        };

        _userRepository
            .Setup(r => r.GetByEmailAsync("user@example.com"))
            .ReturnsAsync(existingUser);

        _passwordHasher
            .Setup(h => h.Verify("wrong-password", "correct-hash"))
            .Returns(false);

        var request = new LoginRequest("user@example.com", "wrong-password");

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.Token);
        Assert.Equal("Invalid email or password.", result.ErrorMessage);
    }
}