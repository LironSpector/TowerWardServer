using BCrypt.Net;
using DTOs;
using Models;
using Repositories;

namespace Services
{
    /// <summary>
    /// Implements <see cref="IUserService"/> to handle user-related business logic,
    /// including CRUD operations and account status management.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserGameStatsService _userGameStatsService;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        /// <param name="userRepository">Repository for user data access.</param>
        /// <param name="userGameStatsService">Service for initializing user game stats.</param>
        public UserService(
            IUserRepository userRepository,
            IUserGameStatsService userGameStatsService)
        {
            _userRepository = userRepository;
            _userGameStatsService = userGameStatsService;
        }

        /// <inheritdoc/>
        public async Task<UserDTO> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;
            return MapToDTO(user);
        }

        /// <inheritdoc/>
        public async Task<IEnumerable<UserDTO>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => MapToDTO(u));
        }

        /// <inheritdoc/>
        public async Task<int> CreateUserAsync(CreateUserDTO createDto)
        {
            // Hash the incoming plaintext password
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(createDto.Password);
            var newUser = new User
            {
                Username = createDto.Username,
                Password = hashedPassword,
                Avatar = createDto.Avatar,
                CreatedAt = DateTime.UtcNow,
                LastLogin = null,
                Status = "Active"
            };

            // Save user
            await _userRepository.AddAsync(newUser);

            // Initialize the user's game stats
            await _userGameStatsService.CreateStatsForUserAsync(newUser.UserId);

            // Return the new user Id
            return newUser.UserId;
        }

        /// <inheritdoc/>
        public async Task UpdateUserAsync(UserDTO userDto)
        {
            var user = await _userRepository.GetByIdAsync(userDto.UserId);
            if (user == null)
            {
                throw new Exception($"User with ID {userDto.UserId} not found.");
            }

            // Update allowed fields
            user.Username = userDto.Username;
            user.Avatar = userDto.Avatar;
            user.Status = userDto.Status ?? user.Status;
            user.LastLogin = userDto.LastLogin ?? user.LastLogin;

            await _userRepository.UpdateAsync(user);
        }

        /// <inheritdoc/>
        public async Task DeleteUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new Exception($"User with ID {userId} not found.");
            }

            await _userRepository.DeleteAsync(user);

            // Optionally delete associated stats:
            // await _userGameStatsService.DeleteStatsAsync(userId);
        }

        /// <inheritdoc/>
        public async Task UpdateLastLoginAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new Exception($"User with ID {userId} not found.");
            }

            user.LastLogin = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }

        /// <inheritdoc/>
        public async Task BanUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new Exception($"User with ID {userId} not found.");
            }

            user.Status = "Banned";
            await _userRepository.UpdateAsync(user);
        }

        /// <inheritdoc/>
        public async Task UnbanUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
            {
                throw new Exception($"User with ID {userId} not found.");
            }

            user.Status = "Active";
            await _userRepository.UpdateAsync(user);
        }

        /// <inheritdoc/>
        public async Task<UserDTO> GetUserByUsernameAsync(string username)
        {
            var user = await _userRepository.GetByUsernameAsync(username);
            return user == null ? null : MapToDTO(user);
        }

        // ----------------------------------------------------------
        // PRIVATE HELPER
        // ----------------------------------------------------------

        /// <summary>
        /// Maps a <see cref="User"/> entity to a <see cref="UserDTO"/>.
        /// </summary>
        /// <param name="user">The user entity to map.</param>
        /// <returns>The resulting <see cref="UserDTO"/>.</returns>
        private UserDTO MapToDTO(User user)
        {
            return new UserDTO
            {
                UserId = user.UserId,
                Username = user.Username,
                Avatar = user.Avatar,
                CreatedAt = user.CreatedAt,
                LastLogin = user.LastLogin,
                Status = user.Status
            };
        }
    }
}
