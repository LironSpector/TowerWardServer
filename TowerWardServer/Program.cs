using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Database;
using Repositories;
using Services;
using Settings;
using TcpServer;


namespace GameSolution
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // 1) Create builder for ASP.NET Core
            var builder = WebApplication.CreateBuilder(args);

            // 2) Add services to the container
            builder.Services.AddControllers();

            // 2a) Add EF Core (MySQL) using the "DefaultConnection" from appsettings.json
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySql(
                    builder.Configuration.GetConnectionString("DefaultConnection"),
                    new MySqlServerVersion(new Version(8, 0, 25)),
                    mysqlOptions => mysqlOptions.EnableRetryOnFailure()
                );
            });

            // 2b) Configure JWT from appsettings
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
            var secretKey = jwtSettings.SecretKey;
            var key = Encoding.UTF8.GetBytes(secretKey);

            // 2c) Add JWT Authentication
            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };
            });

            // 2d) Register the Repositories
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IUserGameStatsRepository, UserGameStatsRepository>();
            builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
            builder.Services.AddScoped<IGlobalGameStatsRepository, GlobalGameStatsRepository>();
            builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();

            // 2e) Register the Services
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<IUserGameStatsService, UserGameStatsService>();
            builder.Services.AddScoped<IGameSessionService, GameSessionService>();
            builder.Services.AddScoped<IGlobalGameStatsService, GlobalGameStatsService>();
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

            // Example: If I have a user creation step or initialization, I can do so here.





            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", builder =>
                {
                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                });
            });

            // 3) Build the app
            var app = builder.Build();

            // 4) Configure the HTTP request pipeline
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // Enable authentication/authorization
            app.UseAuthentication();
            app.UseAuthorization();

            // If you need CORS for external requests, do something like:
            app.UseCors("AllowAll");

            // Map Controllers
            app.MapControllers();


            // Optionally run the DB test
            //using (var scope = app.Services.CreateScope())
            //{
            //    // We can do a quick synchronous call to the test:
            //    DatabaseTestHelper.RunTestsAsync(app).Wait();
            //    // or do it asynchronously in a separate method
            //}




            // 5) Start the TCP server in a background thread
            //var tcpPort = 5555; // same port you used previously
            //var gameTcpServer = new GameTcpServer(tcpPort);

            //var tcpServerThread = new Thread(() =>
            //{
            //    // This runs the Start() method in the background so the main thread is free
            //    gameTcpServer.Start();
            //});
            //tcpServerThread.IsBackground = true;
            //tcpServerThread.Start();



            //    so we can pass them to the TCP server
            //using (var scope = app.Services.CreateScope())
            //{
            //    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            //    var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
            //    // etc if needed

            //    // 4) Create TCP server instance
            //    var tcpPort = 5555;
            //    var gameTcpServer = new GameTcpServer(tcpPort, userService, authService /* any other services needed */);

            //    // 5) Start the TCP server in a background thread
            //    var tcpThread = new Thread(() => gameTcpServer.Start());
            //    tcpThread.IsBackground = true;
            //    tcpThread.Start();
            //}



            //var userService = app.Services.GetRequiredService<IUserService>();
            //var authService = app.Services.GetRequiredService<IAuthenticationService>();

            // 4) Create TCP server instance
            var tcpPort = 5555;
            //var gameTcpServer = new GameTcpServer(tcpPort, userService, authService);
            //var rootProvider = app.Services;
            IServiceProvider rootProvider = app.Services;
            var gameTcpServer = new GameTcpServer(tcpPort, rootProvider);

            // 5) Start the TCP server in a background thread
            var tcpServerThread = new Thread(() => gameTcpServer.Start());
            tcpServerThread.IsBackground = true;
            tcpServerThread.Start();




            // 6) Run the ASP.NET Core Web server (blocking call on main thread)
            app.Run();

            // If you want, after app.Run() completes (usually on shutdown), you can do clean-up:
            // e.g. tcpServerThread.Join(); or other final tasks
        }
    }
}









public static class DatabaseTestHelper
{
    public static async Task RunTestsAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
        var userGameStatsService = scope.ServiceProvider.GetRequiredService<IUserGameStatsService>();
        var sessionService = scope.ServiceProvider.GetRequiredService<IGameSessionService>();
        var globalStatsService = scope.ServiceProvider.GetRequiredService<IGlobalGameStatsService>();

        Console.WriteLine("=== Starting DB Test ===");

        // 1) Create a global stats record if it doesn't exist (id=1 for example)
        var existingGlobalStats = await globalStatsService.GetGlobalStatsAsync(1);
        if (existingGlobalStats == null)
        {
            await globalStatsService.CreateGlobalStatsAsync(new DTOs.GlobalGameStatsDTO
            {
                Id = 1,
                TotalUsers = 0,
                TotalGamesPlayed = 0,
                TotalSingleplayerGames = 0,
                TotalMultiplayerGames = 0
            });
            Console.WriteLine("Created GlobalGameStats row with ID=1");
        }

        // 2) Create some users
        //    - We have CreateUserAsync(CreateUserDTO) from your code
        var userId1 = await userService.CreateUserAsync(new DTOs.CreateUserDTO
        {
            Username = "Alice",
            Password = "alicepassword",
            Avatar = "avatar1.png"
        });
        var userId2 = await userService.CreateUserAsync(new DTOs.CreateUserDTO
        {
            Username = "Bob",
            Password = "bobpassword",
            Avatar = "avatar2.png"
        });
        Console.WriteLine($"Created users: Alice (ID={userId1}), Bob (ID={userId2})");

        // 3) Check user stats were automatically created (assuming you do that in CreateUserAsync).
        var aliceStats = await userGameStatsService.GetStatsByUserIdAsync(userId1);
        var bobStats = await userGameStatsService.GetStatsByUserIdAsync(userId2);

        Console.WriteLine($"Alice Stats => GamesPlayed={aliceStats?.GamesPlayed}, XP={aliceStats?.Xp}");
        Console.WriteLine($"Bob Stats => GamesPlayed={bobStats?.GamesPlayed}, XP={bobStats?.Xp}");

        // 4) Simulate a single-player session
        var sessionIdSp = await sessionService.CreateSessionAsync(new DTOs.GameSessionDTO
        {
            User1Id = userId1,
            User2Id = null,
            Mode = "SinglePlayer",
            StartTime = DateTime.UtcNow
        });
        Console.WriteLine($"Created single-player session ID={sessionIdSp}");

        // "Play" for 120 seconds, then end it
        await Task.Delay(500); // just simulating time; real game logic might update
        await sessionService.EndSessionAsync(sessionIdSp, userId1, 15, 120);

        // Now we increment stats manually or automatically:
        // If we do it manually:
        await userGameStatsService.IncrementGamesPlayedAsync(userId1, true, true);
        await userGameStatsService.AddXpAsync(userId1, 100);

        // Also increment global stats
        await globalStatsService.IncrementTotalUsersAsync(1, 0); // 0 => no new users, but you can do others
        await globalStatsService.IncrementGamesPlayedAsync(1, true); // singlePlayer?

        // 5) Multiplayer session
        var sessionIdMp = await sessionService.CreateSessionAsync(new DTOs.GameSessionDTO
        {
            User1Id = userId1,
            User2Id = userId2,
            Mode = "Multiplayer",
            StartTime = DateTime.UtcNow
        });
        Console.WriteLine($"Created multiplayer session ID={sessionIdMp}");

        // after some "game" time ...
        await Task.Delay(500);
        // let's say Bob wins
        await sessionService.EndSessionAsync(sessionIdMp, userId2, 20, 300); // finalWave=20, timePlayed=300s
        // update stats
        await userGameStatsService.IncrementGamesPlayedAsync(userId1, false, false);
        await userGameStatsService.IncrementGamesPlayedAsync(userId2, true, false);
        // maybe give the winner Bob some XP:
        await userGameStatsService.AddXpAsync(userId2, 200);

        // update global stats for multi
        await globalStatsService.IncrementGamesPlayedAsync(1, false); // false => it's a multiplayer game

        // 6) Print results
        var updatedAliceStats = await userGameStatsService.GetStatsByUserIdAsync(userId1);
        var updatedBobStats = await userGameStatsService.GetStatsByUserIdAsync(userId2);

        Console.WriteLine($"Alice final => GamesPlayed={updatedAliceStats.GamesPlayed}, " +
                          $"GamesWon={updatedAliceStats.GamesWon}, XP={updatedAliceStats.Xp}");
        Console.WriteLine($"Bob final => GamesPlayed={updatedBobStats.GamesPlayed}, " +
                          $"GamesWon={updatedBobStats.GamesWon}, XP={updatedBobStats.Xp}");

        var globalStats = await globalStatsService.GetGlobalStatsAsync(1);
        Console.WriteLine($"Global Stats => totalUsers={globalStats.TotalUsers}, " +
                          $"totalGamesPlayed={globalStats.TotalGamesPlayed}, " +
                          $"singleplayer={globalStats.TotalSingleplayerGames}, " +
                          $"multiplayer={globalStats.TotalMultiplayerGames}");

        Console.WriteLine("=== DB Test Completed ===");
    }
}
