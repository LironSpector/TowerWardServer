using Models;

namespace Repositories
{
    /// <summary>
    /// Defines data access methods for <see cref="User"/> entities.
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// Retrieves a user by their unique identifier, including related game stats.
        /// </summary>
        Task<User> GetByIdAsync(int userId);

        /// <summary>
        /// Retrieves a user by their username, including related game stats.
        /// </summary>
        Task<User> GetByUsernameAsync(string username);

        /// <summary>
        /// Retrieves all users, along with their related game stats.
        /// </summary>
        Task<IEnumerable<User>> GetAllAsync();

        /// <summary>
        /// Adds a new user to the database.
        /// </summary>
        Task AddAsync(User user);

        /// <summary>
        /// Updates an existing user in the database.
        /// </summary>
        Task UpdateAsync(User user);

        /// <summary>
        /// Deletes a user from the database.
        /// </summary>
        Task DeleteAsync(User user);
    }
}
