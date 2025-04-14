// New Program.cs - after removing http & controllers code which was irrelevant
//// --------- Program.cs with Async Main() function to be able to also run all the database tests ---------
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Database;
using Repositories;
using Services;
using Settings;
using TcpServer;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tests;
using AdminTools;

namespace GameSolution
{
    public class Program
    {
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

                    // Re-register JWT Settings for DI
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

                    // (Optional) You can register other services here if needed.
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


            // Optionally, join the TCP server thread on shutdown:
            // tcpServerThread.Join();
        }
    }
}















// Old Program.cs - before removing http & controllers code which is irrelevant
//// --------- Program.cs with Async Main() function to be able to also run all the database tests ---------
//using System.Text;
//using Microsoft.EntityFrameworkCore;
//using Microsoft.AspNetCore.Authentication.JwtBearer;
//using Microsoft.IdentityModel.Tokens;
//using Microsoft.Extensions.Options;
//using Microsoft.AspNetCore.Builder;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Database;
//using Repositories;
//using Services;
//using Settings;
//using TcpServer;
//using Tests;
//using AdminTools;


//namespace GameSolution
//{
//    public class Program
//    {
//        public static async Task Main(string[] args)
//        {
//            // 1) Create builder for ASP.NET Core
//            var builder = WebApplication.CreateBuilder(args);

//            // 2) Add services to the container
//            builder.Services.AddControllers();

//            // 2a) Add EF Core (MySQL) using the "DefaultConnection" from appsettings.json
//            builder.Services.AddDbContext<ApplicationDbContext>(options =>
//            {
//                options.UseMySql(
//                    builder.Configuration.GetConnectionString("DefaultConnection"),
//                    new MySqlServerVersion(new Version(8, 0, 25)),
//                    mysqlOptions => mysqlOptions.EnableRetryOnFailure()
//                );
//            });

//            // 2b) Configure JWT from appsettings
//            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
//            var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
//            var secretKey = jwtSettings.SecretKey;
//            var key = Encoding.UTF8.GetBytes(secretKey);

//            // 2c) Add JWT Authentication
//            builder.Services.AddAuthentication(options =>
//            {
//                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//            })
//            .AddJwtBearer(options =>
//            {
//                options.RequireHttpsMetadata = false;
//                options.SaveToken = true;
//                options.TokenValidationParameters = new TokenValidationParameters
//                {
//                    ValidateIssuer = false,
//                    ValidateAudience = false,
//                    ValidateLifetime = true,
//                    ValidateIssuerSigningKey = true,
//                    IssuerSigningKey = new SymmetricSecurityKey(key),
//                    ClockSkew = TimeSpan.Zero
//                };
//            });

//            // 2d) Register the Repositories
//            builder.Services.AddScoped<IUserRepository, UserRepository>();
//            builder.Services.AddScoped<IUserGameStatsRepository, UserGameStatsRepository>();
//            builder.Services.AddScoped<IGameSessionRepository, GameSessionRepository>();
//            builder.Services.AddScoped<IGlobalGameStatsRepository, GlobalGameStatsRepository>();
//            builder.Services.AddScoped<IAuthenticationRepository, AuthenticationRepository>();

//            // 2e) Register the Services
//            builder.Services.AddScoped<IUserService, UserService>();
//            builder.Services.AddScoped<IUserGameStatsService, UserGameStatsService>();
//            builder.Services.AddScoped<IGameSessionService, GameSessionService>();
//            builder.Services.AddScoped<IGlobalGameStatsService, GlobalGameStatsService>();
//            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();


//            builder.Services.AddCors(options =>
//            {
//                options.AddPolicy("AllowAll", builder =>
//                {
//                    builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
//                });
//            });

//            // 3) Build the app
//            var app = builder.Build();

//            // 4) Configure the HTTP request pipeline
//            if (app.Environment.IsDevelopment())
//            {
//                app.UseDeveloperExceptionPage();
//            }

//            app.UseRouting();

//            // Enable authentication/authorization
//            app.UseAuthentication();
//            app.UseAuthorization();

//            // If you need CORS for external requests, do something like:
//            app.UseCors("AllowAll");

//            // Map Controllers
//            app.MapControllers();


//            // Optionally run the DB test
//            //using (var scope = app.Services.CreateScope())
//            //{
//            //    // We can do a quick synchronous call to the test:
//            //    DatabaseTestHelper.RunTestsAsync(app).Wait();
//            //    // or do it asynchronously in a separate method
//            //}

//            // Optionally run the better DB test
//            //await FullDatabaseTestSuite.RunAllTestsAsync(app);

//            // Run "PrintAdminReport"
//            //await AdminReport.PrintAdminReport(app);

//            // 5) Create TCP server instance
//            var tcpPort = 5555;
//            IServiceProvider rootProvider = app.Services;
//            var gameTcpServer = new GameTcpServer(tcpPort, rootProvider);

//            // 6) Start the TCP server in a background thread
//            var tcpServerThread = new Thread(() => gameTcpServer.Start()); // This runs the Start() method in the background so the main thread is free
//            tcpServerThread.IsBackground = true;
//            tcpServerThread.Start();




//            // 7) Run the ASP.NET Core Web server (blocking call on main thread)
//            app.Run();

//            // If you want, after app.Run() completes (usually on shutdown), you can do clean-up:
//            // e.g. tcpServerThread.Join(); or other final tasks
//        }
//    }
//}