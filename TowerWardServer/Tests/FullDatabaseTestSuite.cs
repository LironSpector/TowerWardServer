using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Services;
using DTOs;

namespace Tests
{
    /// <summary>
    /// A comprehensive test suite covering all major database-related functionality
    /// of the server. It manually exercises the Services layer to confirm that
    /// create, read, update, and delete operations work as expected.
    /// </summary>
    public static class FullDatabaseTestSuite
    {
        /// <summary>
        /// Entry point to run all tests.
        /// Call this method from Program.cs with:
        ///     await FullDatabaseTestSuite.RunAllTestsAsync(host);
        /// </summary>
        /// <param name="host">The IHost instance to use for creating dependency injection scopes.</param>
        public static async Task RunAllTestsAsync(IHost host)
        {
            using var scope = host.Services.CreateScope();

            // Retrieve services from dependency injection
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
            var statsService = scope.ServiceProvider.GetRequiredService<IUserGameStatsService>();
            var sessionService = scope.ServiceProvider.GetRequiredService<IGameSessionService>();
            var globalStatsService = scope.ServiceProvider.GetRequiredService<IGlobalGameStatsService>();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

            Console.WriteLine("========================================");
            Console.WriteLine("Starting FullDatabaseTestSuite...");
            Console.WriteLine("========================================");

            try
            {
                //Note for me: Last time I checked, all the tests succeeded!

                // 1) Test Global Game Stats creation and usage
                await TestGlobalStats(globalStatsService);

                // 2) Test user creation, retrieval, updating, banning, unbanning, etc.
                await TestUserCrudOperations(userService);

                // 3) Test user game stats creation, increments, updates, deletion, etc.
                //    (Also tests related with the user created in #2)
                await TestUserGameStatsOperations(userService, statsService);

                // 4) Test game session creation, update, end session, delete, etc.
                await TestGameSessions(userService, sessionService);

                // 5) Test authentication: login, invalid login, refresh, revoke, etc.
                //    Create a dedicated user for these auth tests to avoid messing up
                //    the user from #2 and #3 tests.
                await TestAuthenticationFlow(userService, authService);

                Console.WriteLine("========================================");
                Console.WriteLine("ALL TESTS PASSED SUCCESSFULLY");
                Console.WriteLine("========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine("========================================");
                Console.WriteLine("TEST SUITE FAILURE!");
                Console.WriteLine($"Exception: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine("========================================");
            }
        }

        // -----------------------------------------------------------------------
        //  Test #1: Global Game Stats
        // -----------------------------------------------------------------------
        private static async Task TestGlobalStats(IGlobalGameStatsService globalStatsService)
        {
            Console.WriteLine("Running Test #1: Global Stats...");

            // Pick an ID to store global stats. Different one from the ID=1 that I have for the real data
            int globalStatsId = 99;

            // Attempt to delete any existing record for the test row
            var existing = await globalStatsService.GetGlobalStatsAsync(globalStatsId);
            if (existing != null)
            {
                await globalStatsService.DeleteGlobalStatsAsync(globalStatsId);
                Console.WriteLine($"Deleted pre-existing global stats with ID={globalStatsId} for a clean slate.");
            }

            // 1) Create new global stats
            var newGlobalStatsDto = new GlobalGameStatsDTO
            {
                Id = globalStatsId,
                TotalUsers = 123,
                TotalGamesPlayed = 456,
                TotalSingleplayerGames = 111,
                TotalMultiplayerGames = 345
            };

            await globalStatsService.CreateGlobalStatsAsync(newGlobalStatsDto);
            Console.WriteLine($"Created GlobalGameStats with ID={globalStatsId}.");

            // 2) Retrieve and verify
            var fetched = await globalStatsService.GetGlobalStatsAsync(globalStatsId);
            if (fetched == null)
            {
                throw new Exception("TestGlobalStats: Fetched GlobalGameStats returned null after creation.");
            }

            if (fetched.TotalUsers != 123 ||
                fetched.TotalGamesPlayed != 456 ||
                fetched.TotalSingleplayerGames != 111 ||
                fetched.TotalMultiplayerGames != 345)
            {
                throw new Exception("TestGlobalStats: Fetched GlobalGameStats did not match the created data.");
            }

            Console.WriteLine("Verified creation and retrieval of global stats.");

            // 3) Update stats
            fetched.TotalUsers += 1;             // 124
            fetched.TotalGamesPlayed += 10;      // 466
            fetched.TotalSingleplayerGames += 2; // 113
            fetched.TotalMultiplayerGames += 8;  // 353

            await globalStatsService.UpdateGlobalStatsAsync(fetched);
            Console.WriteLine("Updated global stats with new values.");

            // 4) Verify updated
            var updated = await globalStatsService.GetGlobalStatsAsync(globalStatsId);
            if (updated == null)
            {
                throw new Exception("TestGlobalStats: Updated GlobalGameStats returned null after update.");
            }

            if (updated.TotalUsers != 124 ||
                updated.TotalGamesPlayed != 466 ||
                updated.TotalSingleplayerGames != 113 ||
                updated.TotalMultiplayerGames != 353)
            {
                throw new Exception("TestGlobalStats: Global stats not updated correctly.");
            }

            Console.WriteLine("Verified global stats update.");

            // 5) Use increment methods
            await globalStatsService.IncrementTotalUsersAsync(globalStatsId, 2); // +2 users
            await globalStatsService.IncrementGamesPlayedAsync(globalStatsId, true);  // singlePlayer => +1 total, +1 single
            await globalStatsService.IncrementGamesPlayedAsync(globalStatsId, false); // multi => +1 total, +1 multi

            var afterIncrements = await globalStatsService.GetGlobalStatsAsync(globalStatsId);
            if (afterIncrements.TotalUsers != 126 ||
                afterIncrements.TotalGamesPlayed != 468 ||
                afterIncrements.TotalSingleplayerGames != 114 ||
                afterIncrements.TotalMultiplayerGames != 354)
            {
                throw new Exception("TestGlobalStats: Increments did not produce expected results.");
            }

            Console.WriteLine("Verified increment methods for global stats.");

            // 6) Clean up (optional)
            await globalStatsService.DeleteGlobalStatsAsync(globalStatsId);
            Console.WriteLine("Deleted test global stats record.");

            Console.WriteLine("Test #1: Global Stats - PASSED.");
        }

        // -----------------------------------------------------------------------
        //  Test #2: User CRUD (Create, Read, Update, Delete) + Ban/Unban
        // -----------------------------------------------------------------------
        private static async Task TestUserCrudOperations(IUserService userService)
        {
            Console.WriteLine("Running Test #2: User CRUD operations...");

            // Create a user, read it, update it, ban/unban it, and more.

            // 1) Create a user
            string testUsername = "TestUser_" + GenerateRandomSuffix();
            var createDto = new CreateUserDTO
            {
                Username = testUsername,
                Password = "TestPassword123!",
                Avatar = "test-avatar.png"
            };

            int newUserId = await userService.CreateUserAsync(createDto);
            Console.WriteLine($"Created user with ID={newUserId}, username={testUsername}.");

            // 2) Retrieve user by ID
            var fetched = await userService.GetUserByIdAsync(newUserId);
            if (fetched == null)
            {
                throw new Exception($"TestUserCrudOperations: Could not fetch user with ID={newUserId} after creation.");
            }
            if (fetched.Username != testUsername)
            {
                throw new Exception("TestUserCrudOperations: Fetched user has unexpected username mismatch.");
            }

            Console.WriteLine("Verified retrieval of newly created user.");

            // 3) Update user
            fetched.Avatar = "updated-avatar.png";
            fetched.Status = "Active"; // or any custom status
            fetched.LastLogin = DateTime.UtcNow;

            await userService.UpdateUserAsync(fetched);
            Console.WriteLine("Updated user's avatar, status, and lastLogin.");

            // 4) Re-fetch to confirm updates
            var updated = await userService.GetUserByIdAsync(newUserId);
            if (updated == null)
            {
                throw new Exception($"User with ID={newUserId} not found after update (unexpected).");
            }

            if (updated.Avatar != "updated-avatar.png")
            {
                throw new Exception("User update: Avatar mismatch after update.");
            }
            if (updated.Status != "Active")
            {
                throw new Exception("User update: Status mismatch after update.");
            }
            if (!updated.LastLogin.HasValue)
            {
                throw new Exception("User update: LastLogin was not updated as expected.");
            }

            Console.WriteLine("Verified user update and re-fetch.");

            // 5) Ban user
            await userService.BanUserAsync(newUserId);
            var bannedCheck = await userService.GetUserByIdAsync(newUserId);
            if (bannedCheck.Status != "Banned")
            {
                throw new Exception("BanUser: Status did not update to 'Banned'.");
            }
            Console.WriteLine("Verified user ban.");

            // 6) Unban user
            await userService.UnbanUserAsync(newUserId);
            var unbanCheck = await userService.GetUserByIdAsync(newUserId);
            if (unbanCheck.Status != "Active")
            {
                throw new Exception("UnbanUser: Status did not revert to 'Active'.");
            }
            Console.WriteLine("Verified user unban.");

            // 7) Delete user
            // Optionally, test the delete
            await userService.DeleteUserAsync(newUserId);

            // Confirm deletion
            var checkDeleted = await userService.GetUserByIdAsync(newUserId);
            if (checkDeleted != null)
            {
                throw new Exception($"DeleteUser: user with ID={newUserId} still exists after deletion attempt.");
            }
            Console.WriteLine($"User with ID={newUserId} deleted successfully.");

            Console.WriteLine("Test #2: User CRUD operations - PASSED.");
        }

        // -----------------------------------------------------------------------
        //  Test #3: User Game Stats operations
        // -----------------------------------------------------------------------
        private static async Task TestUserGameStatsOperations(
            IUserService userService,
            IUserGameStatsService statsService)
        {
            Console.WriteLine("Running Test #3: User Game Stats...");

            // Create a new user in order to test stats. Typically, the
            // CreateUserAsync call automatically creates a stats record, but I decided to test it anyway.

            // 1) Create a user
            string userName = "StatsUser_" + GenerateRandomSuffix();
            var createDto = new CreateUserDTO
            {
                Username = userName,
                Password = "StatsPass123",
                Avatar = "stats-avatar.png"
            };
            int userId = await userService.CreateUserAsync(createDto);
            Console.WriteLine($"Created user for stats test: ID={userId}, Username={userName}.");

            // 2) Immediately fetch stats
            var initialStats = await statsService.GetStatsByUserIdAsync(userId);
            if (initialStats == null)
            {
                throw new Exception("TestUserGameStatsOperations: Stats not automatically created for new user.");
            }
            if (initialStats.GamesPlayed != 0)
            {
                throw new Exception("TestUserGameStatsOperations: New stats record should start with 0 games played.");
            }

            Console.WriteLine("Verified automatic creation of user game stats on new user creation.");

            // 3) Update stats manually
            initialStats.GamesPlayed = 5;
            initialStats.GamesWon = 2;
            initialStats.TotalTimePlayed = 360; // seconds or minutes
            initialStats.SinglePlayerGames = 3;
            initialStats.MultiplayerGames = 2;
            initialStats.Xp = 100;

            await statsService.UpdateStatsAsync(initialStats);
            Console.WriteLine("Manually updated user stats with some sample values.");

            // 4) Re-fetch and verify
            var updatedStats = await statsService.GetStatsByUserIdAsync(userId);
            if (updatedStats.GamesPlayed != 5 ||
                updatedStats.GamesWon != 2 ||
                updatedStats.TotalTimePlayed != 360 ||
                updatedStats.SinglePlayerGames != 3 ||
                updatedStats.MultiplayerGames != 2 ||
                updatedStats.Xp != 100)
            {
                throw new Exception("UpdateStatsAsync: The stats did not update as expected.");
            }

            Console.WriteLine("Verified user stats update operation.");

            // 5) Test AddXpAsync
            await statsService.AddXpAsync(userId, 50);
            var xpCheck = await statsService.GetStatsByUserIdAsync(userId);
            if (xpCheck.Xp != 150)
            {
                throw new Exception("AddXpAsync: XP was not incremented by the expected amount (50).");
            }

            Console.WriteLine("Verified AddXpAsync operation.");

            // 6) Test IncrementGamesPlayedAsync
            //    Let's say the user just won a single-player game
            await statsService.IncrementGamesPlayedAsync(userId, won: true, singlePlayer: true);

            var incrementCheck = await statsService.GetStatsByUserIdAsync(userId);
            if (incrementCheck.GamesPlayed != 6) // was 5
            {
                throw new Exception("IncrementGamesPlayedAsync: GamesPlayed was not incremented properly.");
            }
            if (incrementCheck.GamesWon != 3) // was 2
            {
                throw new Exception("IncrementGamesPlayedAsync: GamesWon was not incremented properly for a 'won=true' scenario.");
            }
            if (incrementCheck.SinglePlayerGames != 4) // was 3
            {
                throw new Exception("IncrementGamesPlayedAsync: SinglePlayerGames was not incremented properly.");
            }

            Console.WriteLine("Verified IncrementGamesPlayedAsync for single-player/win scenario.");

            // 7) Another increment test for multiplayer & lost scenario
            await statsService.IncrementGamesPlayedAsync(userId, won: false, singlePlayer: false);
            var mpCheck = await statsService.GetStatsByUserIdAsync(userId);
            if (mpCheck.GamesPlayed != 7)
            {
                throw new Exception("IncrementGamesPlayedAsync (MP/lost): GamesPlayed didn't increment to 7.");
            }
            if (mpCheck.GamesWon != 3) // Should be unchanged
            {
                throw new Exception("IncrementGamesPlayedAsync (MP/lost): GamesWon changed unexpectedly.");
            }
            if (mpCheck.MultiplayerGames != 3) // was 2, increment by 1
            {
                throw new Exception("IncrementGamesPlayedAsync (MP/lost): MultiplayerGames did not increment to 3.");
            }

            Console.WriteLine("Verified IncrementGamesPlayedAsync for multiplayer/lost scenario.");

            // 8) Let's test delete stats
            await statsService.DeleteStatsAsync(userId);
            var afterDelete = await statsService.GetStatsByUserIdAsync(userId);
            if (afterDelete != null)
            {
                throw new Exception("DeleteStatsAsync: Stats still exist after I attempted to delete them.");
            }
            Console.WriteLine("Verified deleting the user's stats record.");

            // 9) Also optionally delete user at the end to avoid polluting DB
            await userService.DeleteUserAsync(userId);
            Console.WriteLine("Deleted the test user for game stats as a final cleanup.");

            Console.WriteLine("Test #3: User Game Stats - PASSED.");
        }

        // -----------------------------------------------------------------------
        //  Test #4: Game Sessions
        // -----------------------------------------------------------------------
        private static async Task TestGameSessions(
            IUserService userService,
            IGameSessionService sessionService)
        {
            Console.WriteLine("Running Test #4: Game Session creation and usage...");

            // I need at least 2 users to test a multiplayer session. Let's create them:
            var userId1 = await CreateTempUserForSessionTest(userService, "SessionUserA_");
            var userId2 = await CreateTempUserForSessionTest(userService, "SessionUserB_");

            // 1) Create a single-player session (with user1)
            var singlePlayerSessionDto = new GameSessionDTO
            {
                User1Id = userId1,
                User2Id = null,
                Mode = "SinglePlayer",
                StartTime = DateTime.UtcNow
            };

            int spSessionId = await sessionService.CreateSessionAsync(singlePlayerSessionDto);
            Console.WriteLine($"Created single-player session ID={spSessionId} for user={userId1}.");

            // 2) Retrieve the session
            var spSessionFetch = await sessionService.GetSessionByIdAsync(spSessionId);
            if (spSessionFetch == null)
            {
                throw new Exception("Could not fetch single-player session after creation.");
            }
            if (spSessionFetch.Mode != "SinglePlayer")
            {
                throw new Exception("Single-player session incorrectly stored or retrieved with a different mode.");
            }

            Console.WriteLine("Verified single-player session creation and retrieval.");

            // 3) End the single-player session
            await sessionService.EndSessionAsync(spSessionId, wonUserId: userId1, finalWave: 12, timePlayed: 300);
            var endedSession = await sessionService.GetSessionByIdAsync(spSessionId);
            if (!endedSession.EndTime.HasValue)
            {
                throw new Exception("EndSessionAsync: EndTime was not set after ending session.");
            }
            if (endedSession.WonUserId != userId1)
            {
                throw new Exception("EndSessionAsync: WonUserId did not match userId1 for the ended session.");
            }
            if (endedSession.FinalWave != 12 || endedSession.TimePlayed != 300)
            {
                throw new Exception("EndSessionAsync: finalWave/timePlayed did not match the arguments given.");
            }

            Console.WriteLine("Verified single-player session end with final wave and time played.");

            // 4) Create a multiplayer session
            var mpSessionDto = new GameSessionDTO
            {
                User1Id = userId1,
                User2Id = userId2,
                Mode = "Multiplayer",
                StartTime = DateTime.UtcNow
            };

            int mpSessionId = await sessionService.CreateSessionAsync(mpSessionDto);
            Console.WriteLine($"Created multiplayer session ID={mpSessionId} for user1={userId1}, user2={userId2}.");

            // 5) Update session with random changes (not the "end" method)
            var mpSessionFetch = await sessionService.GetSessionByIdAsync(mpSessionId);
            mpSessionFetch.FinalWave = 99; // let's just set a wave for some reason
            mpSessionFetch.TimePlayed = 999;
            await sessionService.UpdateSessionAsync(mpSessionFetch);

            var mpSessionRefetch = await sessionService.GetSessionByIdAsync(mpSessionId);
            if (mpSessionRefetch.FinalWave != 99 || mpSessionRefetch.TimePlayed != 999)
            {
                throw new Exception("GameSession update: finalWave/timePlayed mismatch after update.");
            }

            Console.WriteLine("Verified updating game session fields (not ending).");

            // 6) End the multiplayer session, let's say user2 wins
            await sessionService.EndSessionAsync(mpSessionId, wonUserId: userId2, finalWave: 110, timePlayed: 1200);

            var endedMp = await sessionService.GetSessionByIdAsync(mpSessionId);
            if (!endedMp.EndTime.HasValue)
            {
                throw new Exception("Multiplayer session end: EndTime was not set properly.");
            }
            if (endedMp.WonUserId != userId2)
            {
                throw new Exception("EndSessionAsync: Wrong winner ID set.");
            }
            if (endedMp.FinalWave != 110 || endedMp.TimePlayed != 1200)
            {
                throw new Exception("Multiplayer session end: finalWave/timePlayed mismatch after EndSessionAsync call.");
            }

            Console.WriteLine("Verified ending multiplayer session.");

            // 7) Retrieve all sessions to ensure I can get them in a collection
            var allSessions = await sessionService.GetAllSessionsAsync();
            if (allSessions == null || !allSessions.Any())
            {
                throw new Exception("GetAllSessionsAsync returned empty or null. Expected to see our newly created sessions.");
            }

            Console.WriteLine($"Verified I can fetch all sessions. I see at least {allSessions.Count()} session(s).");

            // 8) Delete sessions
            await sessionService.DeleteSessionAsync(spSessionId);
            await sessionService.DeleteSessionAsync(mpSessionId);
            var spCheck = await sessionService.GetSessionByIdAsync(spSessionId);
            var mpCheck = await sessionService.GetSessionByIdAsync(mpSessionId);
            if (spCheck != null || mpCheck != null)
            {
                throw new Exception("DeleteSessionAsync: One or both sessions still exist after deletion attempt.");
            }

            Console.WriteLine("Verified session deletion.");

            // 9) Cleanup: delete the 2 users
            await userService.DeleteUserAsync(userId1);
            await userService.DeleteUserAsync(userId2);

            Console.WriteLine("Test #4: Game Sessions - PASSED.");
        }

        /// <summary>
        /// Helper method to create a new user for session tests.
        /// </summary>
        private static async Task<int> CreateTempUserForSessionTest(IUserService userService, string namePrefix)
        {
            var username = namePrefix + GenerateRandomSuffix();
            var userDto = new CreateUserDTO
            {
                Username = username,
                Password = "SessionTestPass",
                Avatar = "session-user.png"
            };
            int userId = await userService.CreateUserAsync(userDto);
            Console.WriteLine($"Created temp user: ID={userId}, Username={username}");
            return userId;
        }

        // -----------------------------------------------------------------------
        //  Test #5: Authentication Flow
        // -----------------------------------------------------------------------
        private static async Task TestAuthenticationFlow(IUserService userService, IAuthenticationService authService)
        {
            Console.WriteLine("Running Test #5: Authentication Flow...");

            // Create a user solely for testing login/refresh scenarios
            string authUserName = "AuthUser_" + GenerateRandomSuffix();
            var createDto = new CreateUserDTO
            {
                Username = authUserName,
                Password = "AuthPass123",
                Avatar = "auth-user.png"
            };

            // 1) Create user
            int userId = await userService.CreateUserAsync(createDto);
            Console.WriteLine($"Created auth test user: ID={userId}, username={authUserName}.");

            // 2) Try valid login
            var authResponse = await authService.LoginAsync(authUserName, "AuthPass123");
            if (authResponse == null)
            {
                throw new Exception("TestAuthenticationFlow: Valid login returned null, which indicates a failure.");
            }
            if (string.IsNullOrEmpty(authResponse.AccessToken) || string.IsNullOrEmpty(authResponse.RefreshToken))
            {
                throw new Exception("LoginAsync: AccessToken/RefreshToken were not returned properly.");
            }

            Console.WriteLine("Verified valid login scenario with correct credentials.");

            // 3) Test invalid login
            var invalidLogin = await authService.LoginAsync(authUserName, "WrongPassword");
            if (invalidLogin != null)
            {
                throw new Exception("TestAuthenticationFlow: Invalid login returned non-null, indicating acceptance of wrong password.");
            }
            Console.WriteLine("Verified invalid login scenario rejects wrong password.");

            // 4) Validate the returned JWT (access token)
            var validationResult = await authService.ValidateTokenAsync(authResponse.AccessToken);
            if (!validationResult.IsValid)
            {
                throw new Exception("TestAuthenticationFlow: The access token from a valid login is being deemed invalid.");
            }
            if (validationResult.UserId != userId)
            {
                throw new Exception("TestAuthenticationFlow: The 'sub' claim from the validated token does not match our userId.");
            }
            Console.WriteLine("Verified that the access token is valid and maps to the correct user.");

            // 5) Test refresh
            var refreshResult = await authService.RefreshAsync(authResponse.RefreshToken);
            if (refreshResult == null)
            {
                throw new Exception("TestAuthenticationFlow: RefreshAsync returned null for a valid refresh token.");
            }
            if (string.IsNullOrEmpty(refreshResult.AccessToken) ||
                string.IsNullOrEmpty(refreshResult.RefreshToken))
            {
                throw new Exception("RefreshAsync: Did not provide new access or refresh tokens.");
            }

            Console.WriteLine("Verified refreshing tokens with a valid refresh token.");

            // 6) Test refresh with old/used refresh token (the old refresh token should typically be invalid now)
            var secondRefreshAttempt = await authService.RefreshAsync(authResponse.RefreshToken);
            if (secondRefreshAttempt != null)
            {
                throw new Exception("TestAuthenticationFlow: The old refresh token was reused and accepted, which is unexpected if one-time use is enforced.");
            }
            Console.WriteLine("Verified that re-using the old refresh token after first refresh fails.");

            // 7) Revoke all tokens
            await authService.RevokeAllAsync(userId);
            // Attempting to refresh again with the new refresh token I got from (5) should fail now.
            var postRevokeRefresh = await authService.RefreshAsync(refreshResult.RefreshToken);
            if (postRevokeRefresh != null)
            {
                throw new Exception("TestAuthenticationFlow: I revoked all tokens, but refresh is still successful. RevokeAllAsync might not be working.");
            }

            Console.WriteLine("Verified that RevokeAllAsync invalidates any existing refresh tokens.");

            // 8) Clean up
            await userService.DeleteUserAsync(userId);
            Console.WriteLine("Deleted auth test user.");

            Console.WriteLine("Test #5: Authentication Flow - PASSED.");
        }

        // -----------------------------------------------------------------------
        //  Utility: GenerateRandomSuffix
        // -----------------------------------------------------------------------
        /// <summary>
        /// Generates a short random alphanumeric suffix to ensure unique usernames in tests.
        /// </summary>
        private static string GenerateRandomSuffix()
        {
            // Just 6 random alphanumeric chars
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = new char[6];
            for (int i = 0; i < 6; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            return new string(result);
        }
    }
}
