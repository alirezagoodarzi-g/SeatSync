using SeatSync.Core.Entities;

namespace SeatSync.Core.DTOs;

public record RegisterRequest(string Email, string Password, string DisplayName, Role Role);

public record LoginRequest(string Email, string Password);

public record AuthResult(bool Success, string? Token, string? ErrorMessage);