using SeatSync.Core.Entities;

namespace SeatSync.Core.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}