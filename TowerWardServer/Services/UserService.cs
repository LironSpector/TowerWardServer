using BCrypt.Net; // For BCrypt hashing
using DTOs;
using Models;
using Repositories;

namespace Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserGameStatsService _userGameStatsService;

        public UserService(IUserRepository userRepository, IUserGameStatsService userGameStatsService)
        {
            _userRepository = userRepository;
            _userGameStatsService = userGameStatsService;
        }

        public async Task<UserDTO> GetUserByIdAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            return MapToDTO(user);
        }

        public async Task<IEnumerable<UserDTO>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllAsync();
            return users.Select(u => MapToDTO(u));
        }

        public async Task<int> CreateUserAsync(CreateUserDTO createDto)
        {
            // Check if username already exists, etc. (optional)
            // var existing = await _userRepository.GetByUsernameAsync(createDto.Username);
            // if (existing != null) throw new Exception("Username already taken!");

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

            // Optionally create the user's initial stats
            await _userGameStatsService.CreateStatsForUserAsync(newUser.UserId);

            // Return the new user Id
            return newUser.UserId;
        }

        public async Task UpdateUserAsync(UserDTO userDto)
        {
            var user = await _userRepository.GetByIdAsync(userDto.UserId);
            if (user == null)
                throw new Exception($"User with ID {userDto.UserId} not found.");

            // Update fields (NOT password - if you want to update password, you'd do so in a dedicated method)
            user.Username = userDto.Username;
            user.Avatar = userDto.Avatar;
            user.Status = userDto.Status ?? user.Status;
            user.LastLogin = userDto.LastLogin ?? user.LastLogin;

            await _userRepository.UpdateAsync(user);
        }

        public async Task DeleteUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception($"User with ID {userId} not found.");

            await _userRepository.DeleteAsync(user);

            // Could also decide if we want to delete the stats in tandem:
            // await _userGameStatsService.DeleteStatsAsync(userId);
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception($"User with ID {userId} not found.");

            user.LastLogin = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
        }

        public async Task BanUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception($"User with ID {userId} not found.");

            user.Status = "Banned";
            await _userRepository.UpdateAsync(user);
        }

        public async Task UnbanUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception($"User with ID {userId} not found.");

            user.Status = "Active";
            await _userRepository.UpdateAsync(user);
        }

        // ----------------------------------------------------------
        // PRIVATE HELPER
        // ----------------------------------------------------------
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






//using DTOs;
//using Models;
//using Repositories;

//namespace Services
//{
//    public class UserService : IUserService
//    {
//        private readonly IUserRepository _userRepository;

//        public UserService(IUserRepository userRepository)
//        {
//            _userRepository = userRepository;
//        }

//        public async Task<UserDTO> GetUserByIdAsync(int userId)
//        {
//            var user = await _userRepository.GetByIdAsync(userId);
//            if (user == null) return null;

//            return MapToDTO(user);
//        }

//        public async Task<IEnumerable<UserDTO>> GetAllUsersAsync()
//        {
//            var users = await _userRepository.GetAllAsync();
//            return users.Select(u => MapToDTO(u));
//        }

//        public async Task<int> CreateUserAsync(UserDTO userDto)
//        {
//            // You could also do hashing of passwords if you store them differently in userDto
//            var user = new User
//            {
//                Username = userDto.Username,
//                Password = "[hashed-password-here]", // or from userDto if you have a separate create DTO
//                Avatar = userDto.Avatar,
//                CreatedAt = userDto.CreatedAt == default ? DateTime.UtcNow : userDto.CreatedAt,
//                LastLogin = userDto.LastLogin,
//                Status = userDto.Status ?? "Active"
//            };

//            await _userRepository.AddAsync(user);
//            return user.UserId;
//        }

//        public async Task UpdateUserAsync(UserDTO userDto)
//        {
//            var user = await _userRepository.GetByIdAsync(userDto.UserId);
//            if (user == null)
//                throw new Exception($"User with ID {userDto.UserId} not found.");

//            // Update fields
//            user.Username = userDto.Username;
//            // If you want to allow password updates, handle that here
//            user.Avatar = userDto.Avatar;
//            user.LastLogin = userDto.LastLogin;
//            user.Status = userDto.Status ?? user.Status;

//            await _userRepository.UpdateAsync(user);
//        }

//        public async Task DeleteUserAsync(int userId)
//        {
//            var user = await _userRepository.GetByIdAsync(userId);
//            if (user == null)
//                throw new Exception($"User with ID {userId} not found.");

//            await _userRepository.DeleteAsync(user);
//        }

//        public async Task UpdateLastLoginAsync(int userId)
//        {
//            var user = await _userRepository.GetByIdAsync(userId);
//            if (user == null)
//                throw new Exception($"User with ID {userId} not found.");

//            user.LastLogin = DateTime.UtcNow;
//            await _userRepository.UpdateAsync(user);
//        }

//        public async Task BanUserAsync(int userId)
//        {
//            var user = await _userRepository.GetByIdAsync(userId);
//            if (user == null)
//                throw new Exception($"User with ID {userId} not found.");

//            user.Status = "Banned";
//            await _userRepository.UpdateAsync(user);
//        }

//        public async Task UnbanUserAsync(int userId)
//        {
//            var user = await _userRepository.GetByIdAsync(userId);
//            if (user == null)
//                throw new Exception($"User with ID {userId} not found.");

//            user.Status = "Active";
//            await _userRepository.UpdateAsync(user);
//        }

//        // -----------------------------------------------------------------
//        // PRIVATE HELPER for Entity ↔ DTO mapping
//        // -----------------------------------------------------------------

//        private UserDTO MapToDTO(User user)
//        {
//            return new UserDTO
//            {
//                UserId = user.UserId,
//                Username = user.Username,
//                Avatar = user.Avatar,
//                CreatedAt = user.CreatedAt,
//                LastLogin = user.LastLogin,
//                Status = user.Status
//            };
//        }
//    }
//}
