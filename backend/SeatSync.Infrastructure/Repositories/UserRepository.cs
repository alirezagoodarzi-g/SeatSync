using Microsoft.EntityFrameworkCore;
using SeatSync.Core.Entities;
using SeatSync.Core.Interfaces;
using SeatSync.Infrastructure.Data;

namespace SeatSync.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly SeatSyncDbContext _context;

    public UserRepository(SeatSyncDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
        => await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

    public async Task<User?> GetByEmailAsync(string email)
        => await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> EmailExistsAsync(string email)
        => await _context.Users.AnyAsync(u => u.Email == email);
}