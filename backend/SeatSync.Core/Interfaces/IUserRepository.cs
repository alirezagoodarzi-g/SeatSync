using SeatSync.Core.Entities;

namespace SeatSync.Core.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User> CreateAsync(User user);
    Task<bool> EmailExistsAsync(string email);
}