//// --------- Program.cs with Async Main() function to be able to also run all the database tests ---------
using Microsoft.EntityFrameworkCore;
using Database;
using Repositories;
using Services;
using Settings;
using TcpServer;
using Tests;
using AdminTools;

namespace GameSolution
{
    /// <summary>
    /// The entry point of the server application. This Program class builds a generic host,
    /// configures dependency injection for EF Core, repositories, services, and JWT settings,
    /// and starts a TCP server in a background thread.
    /// It then runs the host which keeps the application alive until shutdown.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Main method. Builds the host, configures services, starts the TCP server, 
        /// optionally runs tests or admin reports, and finally starts the host.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        public static async Task Main(string[] args)
        {
            // Create a generic host builder
            var builder = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Register EF Core with MySQL connection from appsettings.json
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseMySql(
                            hostContext.Configuration.GetConnectionString("DefaultConnection"),
                            new MySqlServerVersion(new Version(8, 0, 25)),
                            mysqlOptions => mysqlOptions.EnableRetryOnFailure()
                        );
                    });

                    // Re-register JWT Settings for DI so that AuthenticationService receives a valid JwtSettings instance.
                    services.Configure<JwtSettings>(hostContext.Configuration.GetSection("JwtSettings"));

                    // Register the Repositories
                    services.AddScoped<IUserRepository, UserRepository>();
                    services.AddScoped<IUserGameStatsRepository, UserGameStatsRepository>();
                    services.AddScoped<IGameSessionRepository, GameSessionRepository>();
                    services.AddScoped<IGlobalGameStatsRepository, GlobalGameStatsRepository>();
                    services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();

                    // Register the Services
                    services.AddScoped<IUserService, UserService>();
                    services.AddScoped<IUserGameStatsService, UserGameStatsService>();
                    services.AddScoped<IGameSessionService, GameSessionService>();
                    services.AddScoped<IGlobalGameStatsService, GlobalGameStatsService>();
                    services.AddScoped<IAuthenticationService, AuthenticationService>();
                });

            // Build the host
            var host = builder.Build();

            // Start the TCP server in a background thread
            int tcpPort = 5555;
            IServiceProvider rootProvider = host.Services;
            var gameTcpServer = new GameTcpServer(tcpPort, rootProvider);

            Thread tcpServerThread = new Thread(() => gameTcpServer.Start());
            tcpServerThread.IsBackground = true;
            tcpServerThread.Start();

            // Optionally run tests or admin reports:
            //await FullDatabaseTestSuite.RunAllTestsAsync(host);
            //await AdminReport.PrintAdminReport(host);

            // Run the host (blocking call until shutdown)
            await host.RunAsync();
        }
    }
}
