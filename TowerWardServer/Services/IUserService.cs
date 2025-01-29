using DTOs;

namespace Services
{
    public interface IUserService
    {
        // Read operations
        Task<UserDTO> GetUserByIdAsync(int userId);
        Task<IEnumerable<UserDTO>> GetAllUsersAsync();

        // Creation - using CreateUserDTO
        Task<int> CreateUserAsync(CreateUserDTO createDto);

        // Update - using UserDTO for the updatable fields
        Task UpdateUserAsync(UserDTO userDto);

        // Delete
        Task DeleteUserAsync(int userId);

        // Additional methods
        Task UpdateLastLoginAsync(int userId);
        Task BanUserAsync(int userId);
        Task UnbanUserAsync(int userId);

        Task<UserDTO> GetUserByUsernameAsync(string username);
    }
}
