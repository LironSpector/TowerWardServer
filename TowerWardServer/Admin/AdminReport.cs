using Microsoft.Extensions.Hosting;
using Services;

namespace AdminTools
{
    /// <summary>
    /// Generates and prints an admin report of the global (system-wide) statistics,
    /// without exposing any specific user data.
    /// </summary>
    public static class AdminReport
    {
        /// <summary>
        /// Fetches the GlobalGameStats record (by default ID=1) and prints a
        /// nicely formatted summary of the global stats in the console.
        /// 
        /// Call this method from Program.cs after building the host:
        ///     await AdminReport.PrintAdminReport(host);
        /// </summary>
        /// <param name="host">The IHost instance (for dependency injection scope creation).</param>
        /// <param name="statsId">Which global stats record ID to load (defaults to 1).</param>
        public static async Task PrintAdminReport(IHost host, int statsId = 1)
        {
            // Create a dependency injection scope using host.Services
            using var scope = host.Services.CreateScope();
            var globalStatsService = scope.ServiceProvider.GetRequiredService<IGlobalGameStatsService>();

            // Attempt to fetch the global stats record by the given ID
            var globalStats = await globalStatsService.GetGlobalStatsAsync(statsId);
            if (globalStats == null)
            {
                Console.WriteLine($"[AdminReport] No GlobalGameStats record found for ID={statsId}.");
                return;
            }

            // Print a nicely formatted summary report in the console
            Console.WriteLine();
            Console.WriteLine("====================================================");
            Console.WriteLine("|              ADMIN SUMMARY REPORT               |");
            Console.WriteLine("====================================================");
            Console.WriteLine($"|  Global Stats ID           : {globalStats.Id,-24} |");
            Console.WriteLine($"|  Total Registered Users    : {globalStats.TotalUsers,-24} |");
            Console.WriteLine($"|  Total Games Played        : {globalStats.TotalGamesPlayed,-24} |");
            Console.WriteLine($"|  Single-player Games       : {globalStats.TotalSingleplayerGames,-24} |");
            Console.WriteLine($"|  Multi-player Games        : {globalStats.TotalMultiplayerGames,-24} |");
            Console.WriteLine("====================================================");
            Console.WriteLine($"Report generated on: {DateTime.Now}");
            Console.WriteLine();
        }
    }
}
