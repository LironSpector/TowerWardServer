using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Services;
using DTOs;

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
        /// Call this method from Program.cs after building the app:
        ///     await AdminReport.PrintAdminReport(app);
        /// </summary>
        /// <param name="app">The WebApplication (for DI scope creation).</param>
        /// <param name="statsId">Which global stats record ID to load (defaults to 1).</param>
        public static async Task PrintAdminReport(WebApplication app, int statsId = 1)
        {
            // Create a DI scope so we can retrieve necessary services
            using var scope = app.Services.CreateScope();
            var globalStatsService = scope.ServiceProvider.GetRequiredService<IGlobalGameStatsService>();

            // Attempt to fetch the global stats record by the given ID
            var globalStats = await globalStatsService.GetGlobalStatsAsync(statsId);
            if (globalStats == null)
            {
                Console.WriteLine($"[AdminReport] No GlobalGameStats record found for ID={statsId}.");
                return;
            }

            // Print a nice ASCII table containing the important global stats
            Console.WriteLine();
            Console.WriteLine("====================================================");
            Console.WriteLine("|              ADMIN SUMMARY REPORT               |");
            Console.WriteLine("====================================================");
            Console.WriteLine($"|  Global Stats ID           : {globalStats.Id,-24} |"); // The -24 ensures the value is left-aligned within a 24-character-wide field, padding with spaces on the right if necessary.
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
