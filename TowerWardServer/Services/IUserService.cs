using DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    /// <summary>
    /// Defines business-logic operations related to <see cref="User"/> management.
    /// </summary>
    public interface IUserService
    {
        /// <summary>
        /// Retrieves a user by their unique identifier.
        /// </summary>
        /// <param name="userId">The ID of the user to fetch.</param>
        /// <returns>
        /// A <see cref="UserDTO"/> representing the user, or <c>null</c> if not found.
        /// </returns>
        Task<UserDTO> GetUserByIdAsync(int userId);

        /// <summary>
        /// Retrieves all users in the system.
        /// </summary>
        /// <returns>
        /// A sequence of <see cref="UserDTO"/> objects for all registered users.
        /// </returns>
        Task<IEnumerable<UserDTO>> GetAllUsersAsync();

        /// <summary>
        /// Creates a new user account.
        /// </summary>
        /// <param name="createDto">Data required to create the user.</param>
        /// <returns>
        /// The newly created user's ID.
        /// </returns>
        Task<int> CreateUserAsync(CreateUserDTO createDto);

        /// <summary>
        /// Updates mutable fields of an existing user.
        /// </summary>
        /// <param name="userDto">The user data transfer object containing updated values.</param>
        Task UpdateUserAsync(UserDTO userDto);

        /// <summary>
        /// Deletes a user by their unique identifier.
        /// </summary>
        /// <param name="userId">The ID of the user to delete.</param>
        Task DeleteUserAsync(int userId);

        /// <summary>
        /// Updates the LastLogin timestamp for the specified user to now.
        /// </summary>
        /// <param name="userId">The ID of the user whose last login is updated.</param>
        Task UpdateLastLoginAsync(int userId);

        /// <summary>
        /// Marks the specified user as banned.
        /// </summary>
        /// <param name="userId">The ID of the user to ban.</param>
        Task BanUserAsync(int userId);

        /// <summary>
        /// Unbans the specified user, setting their status back to active.
        /// </summary>
        /// <param name="userId">The ID of the user to unban.</param>
        Task UnbanUserAsync(int userId);

        /// <summary>
        /// Retrieves a user by their username.
        /// </summary>
        /// <param name="username">The username to search for.</param>
        /// <returns>
        /// A <see cref="UserDTO"/> for the matching user, or <c>null</c> if not found.
        /// </returns>
        Task<UserDTO> GetUserByUsernameAsync(string username);
    }
}
