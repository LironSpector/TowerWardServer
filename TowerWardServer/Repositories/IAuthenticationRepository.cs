using Models;

namespace Repositories
{
    public interface IAuthenticationRepository
    {
        Task<Authentication> GetByIdAsync(int authId);
        Task<Authentication> GetByRefreshTokenAsync(string refreshToken);
        Task<IEnumerable<Authentication>> GetAllForUserAsync(int userId);

        Task AddAsync(Authentication authRecord);
        Task UpdateAsync(Authentication authRecord);
        Task DeleteAsync(Authentication authRecord);

        // Possibly a method to revoke all tokens for a user, etc.
        Task RevokeAllTokensForUserAsync(int userId);
    }
}
