using DTOs;

namespace Services
{
    /// <summary>
    /// Defines business logic operations for managing a user's game statistics.
    /// </summary>
    public interface IUserGameStatsService
    {
        /// <summary>
        /// Retrieves the game stats for the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>
        /// A <see cref="UserGameStatsDTO"/> containing the user’s statistics,
        /// or <c>null</c> if no stats record exists.
        /// </returns>
        Task<UserGameStatsDTO> GetStatsByUserIdAsync(int userId);

        /// <summary>
        /// Creates an initial stats record for a newly registered user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <exception cref="System.Exception">
        /// Thrown if stats already exist for the given user.
        /// </exception>
        Task CreateStatsForUserAsync(int userId);

        /// <summary>
        /// Updates an existing stats record with new values.
        /// </summary>
        /// <param name="statsDto">
        /// A <see cref="UserGameStatsDTO"/> containing the updated stats values.
        /// </param>
        /// <exception cref="System.Exception">
        /// Thrown if no stats are found for the given user.
        /// </exception>
        Task UpdateStatsAsync(UserGameStatsDTO statsDto);

        /// <summary>
        /// Deletes the stats record for the specified user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <exception cref="System.Exception">
        /// Thrown if no stats are found for the given user.
        /// </exception>
        Task DeleteStatsAsync(int userId);

        /// <summary>
        /// Adds experience points (XP) to a user’s stats.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="amount">The amount of XP to add.</param>
        /// <exception cref="System.Exception">
        /// Thrown if no stats are found for the given user.
        /// </exception>
        Task AddXpAsync(int userId, int amount);

        /// <summary>
        /// Increments the total games played count and related fields.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="won">
        /// <c>true</c> if the user won the game; otherwise <c>false</c>.
        /// </param>
        /// <param name="singlePlayer">
        /// <c>true</c> if it was a single-player game; <c>false</c> for multiplayer.
        /// </param>
        /// <exception cref="System.Exception">
        /// Thrown if no stats are found for the given user.
        /// </exception>
        Task IncrementGamesPlayedAsync(int userId, bool won, bool singlePlayer);
    }
}
