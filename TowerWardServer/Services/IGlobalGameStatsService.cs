using DTOs;

namespace Services
{
    /// <summary>
    /// Defines operations for managing system wide game statistics.
    /// </summary>
    public interface IGlobalGameStatsService
    {
        /// <summary>
        /// Retrieves the <see cref="GlobalGameStatsDTO"/> record for the given ID.
        /// </summary>
        /// <param name="id">The identifier of the global stats record (id=1).</param>
        /// <returns>
        /// The matching <see cref="GlobalGameStatsDTO"/>, or <c>null</c> if not found.
        /// </returns>
        Task<GlobalGameStatsDTO> GetGlobalStatsAsync(int id);

        /// <summary>
        /// Creates a new global statistics record.
        /// </summary>
        /// <param name="statsDto">
        /// The DTO containing initial values for global stats.
        /// </param>
        /// <exception cref="System.Exception">
        /// Thrown if a record with the same ID already exists.
        /// </exception>
        Task CreateGlobalStatsAsync(GlobalGameStatsDTO statsDto);

        /// <summary>
        /// Updates an existing global statistics record.
        /// </summary>
        /// <param name="statsDto">
        /// The DTO containing updated values (must include the existing record's ID).
        /// </param>
        /// <exception cref="System.Exception">
        /// Thrown if the record is not found.
        /// </exception>
        Task UpdateGlobalStatsAsync(GlobalGameStatsDTO statsDto);

        /// <summary>
        /// Deletes the global statistics record with the specified ID.
        /// </summary>
        /// <param name="id">The identifier of the stats record to delete.</param>
        /// <exception cref="System.Exception">
        /// Thrown if the record is not found.
        /// </exception>
        Task DeleteGlobalStatsAsync(int id);

        /// <summary>
        /// Increments the total user count by a specified amount.
        /// </summary>
        /// <param name="id">The ID of the stats record to update.</param>
        /// <param name="amount">The number of users to add (default is 1).</param>
        /// <exception cref="System.Exception">
        /// Thrown if the record is not found.
        /// </exception>
        Task IncrementTotalUsersAsync(int id, int amount = 1);

        /// <summary>
        /// Increments the overall games played count and either the single-player
        /// or multiplayer sub-count.
        /// </summary>
        /// <param name="id">The ID of the stats record to update.</param>
        /// <param name="singlePlayer">
        /// <c>true</c> to increment single-player games; <c>false</c> for multiplayer.
        /// </param>
        /// <exception cref="System.Exception">
        /// Thrown if the record is not found.
        /// </exception>
        Task IncrementGamesPlayedAsync(int id, bool singlePlayer);
    }
}
