using System;
using Newtonsoft.Json.Linq;
using Microsoft.Extensions.DependencyInjection;
using DTOs;
using Services;
using Models;
using Repositories;

namespace TcpServer
{
    /// <summary>
    /// Receives decrypted JSON from ClientHandler and runs the big switch logic.
    /// Also includes the methods for register/login, token checking, and other DB/auth operations.
    /// </summary>
    public class ServerMessageHandler
    {
        private readonly ClientHandler _client;
        private readonly IServiceProvider _rootProvider;

        /// <summary>
        /// Initializes a new instance of the ServerMessageHandler class.
        /// </summary>
        /// <param name="client">The ClientHandler representing the connected client.</param>
        /// <param name="rootProvider">The root IServiceProvider used for creating dependency injection scopes.</param>
        public ServerMessageHandler(ClientHandler client, IServiceProvider rootProvider)
        {
            _client = client;
            _rootProvider = rootProvider;
        }

        /// <summary>
        /// Main entry for processing a post-handshake, decrypted JSON message.
        /// Splits the processing logic based on the "Type" field in the JSON message.
        /// </summary>
        /// <param name="data">The decrypted JSON message as a string.</param>
        public void HandleMessage(string data)
        {
            Console.WriteLine("[ServerMessageHandler] Decrypted data => " + data);

            JObject msgObj = JObject.Parse(data);
            string messageType = msgObj["Type"]?.ToString();

            // If not a login/registration or snapshot message, then extract token data and validate it.
            if (messageType != "RegisterUser" && messageType != "LoginUser"
                && messageType != "AutoLogin" && messageType != "GameSnapshot")
            {
                // 1) Extract the token data first and check it
                JObject tokenData = (JObject)msgObj["TokenData"];
                if (tokenData == null)
                {
                    _client.SendEncryptedMessage("{\"Type\":\"Error\",\"Data\":{\"Reason\":\"No TokenData in message.\"}}");
                    return;
                }
                string accessToken = tokenData["AccessToken"]?.ToString();
                string refreshToken = tokenData["RefreshToken"]?.ToString();

                // 2) Validate or refresh tokens
                int? userId = CheckAndRefreshTokens(accessToken, refreshToken);
                if (userId == null)
                    return; // invalid, so stop processing

                // If valid, store the user id in the client handler.
                _client.SetUserId(userId.Value);

                Console.WriteLine("CheckAndRefreshTokens - OK");
            }

            switch (messageType)
            {
                case "MatchmakingRequest":
                    _client.Server.AddToMatchmaking(_client);
                    break;

                case "SendBalloon":
                case "GameSnapshot":
                case "ShowSnapshots":
                case "HideSnapshots":
                    ForwardToOpponent(data);
                    break;

                case "WaveDone":
                    HandleWaveDone(msgObj);
                    break;

                case "UseMultiplayerAbility":
                    ForwardAbility(msgObj);
                    break;

                case "GameOver":
                    HandleGameOver(data);
                    break;

                case "RegisterUser":
                    HandleRegisterUser(msgObj);
                    break;

                case "LoginUser":
                    HandleLoginUser(msgObj);
                    break;

                case "UpdateLastLogin":
                    HandleUpdateLastLogin(msgObj);
                    break;

                case "GameOverDetailed":
                    HandleGameOverDetailed(msgObj);
                    break;

                case "AutoLogin":
                    HandleAutoLogin(msgObj);
                    break;

                default:
                    Console.WriteLine("[ServerMessageHandler] Unknown messageType => " + messageType);
                    break;
            }
        }

        /// <summary>
        /// For messages that simply get forwarded to the opponent (e.g., SendBalloon, GameSnapshot, ShowSnapshots, HideSnapshots).
        /// </summary>
        /// <param name="data">The JSON message to forward.</param>
        private void ForwardToOpponent(string data)
        {
            var opp = _client.GetOpponent();
            if (opp != null)
            {
                opp.SendEncryptedMessage(data);
            }
        }

        /// <summary>
        /// For "UseMultiplayerAbility" messages, sets the "FromOpponent" flag to true in the Data object,
        /// then forwards the entire message to the opponent.
        /// </summary>
        /// <param name="msgObj">The JSON object representing the message.</param>
        private void ForwardAbility(JObject msgObj)
        {
            var opp = _client.GetOpponent();
            if (opp != null)
            {
                JObject jData = (JObject)msgObj["Data"];
                jData["FromOpponent"] = true;
                opp.SendEncryptedMessage(msgObj.ToString());
            }
        }

        /// <summary>
        /// Handles the "WaveDone" message by checking if the wave index from the message matches the local match wave index.
        /// If so, increments the match wave index for both clients and sends a "StartNextWave" message.
        /// </summary>
        /// <param name="msgObj">The JSON object representing the message.</param>
        private void HandleWaveDone(JObject msgObj)
        {
            JObject dataObj = (JObject)msgObj["Data"];
            int waveFinishedIndex = dataObj["WaveIndex"].ToObject<int>();

            Console.WriteLine($"[ServerMessageHandler] waveFinished={waveFinishedIndex}, local matchWaveIndex={_client.matchWaveIndex}");

            if (waveFinishedIndex == _client.matchWaveIndex)
            {
                _client.matchWaveIndex++;
                var opp = _client.GetOpponent();
                if (opp != null)
                {
                    opp.matchWaveIndex = _client.matchWaveIndex;

                    string startMsg = $"{{\"Type\":\"StartNextWave\",\"WaveIndex\":{_client.matchWaveIndex}}}";
                    _client.SendEncryptedMessage(startMsg);
                    opp.SendEncryptedMessage(startMsg);
                }
            }
        }

        /// <summary>
        /// Handles the "GameOver" message.
        /// Forwards the message to the opponent and then clears the opponent relationship.
        /// </summary>
        /// <param name="data">The JSON message representing the GameOver event.</param>
        private void HandleGameOver(string data)
        {
            var opp = _client.GetOpponent();
            if (opp != null)
            {
                opp.SendEncryptedMessage(data);
                opp.SetOpponent(null);
            }
            _client.SetOpponent(null);
        }


        /// <summary>
        /// Validates the provided access token or refreshes tokens if expired/invalid.
        /// Returns the user id if validation or refresh is successful; otherwise returns null.
        /// </summary>
        /// <param name="accessToken">The current access token.</param>
        /// <param name="refreshToken">The refresh token.</param>
        /// <returns>An integer user id if valid; otherwise, null.</returns>
        private int? CheckAndRefreshTokens(string accessToken, string refreshToken)
        {
            using (var scope = _rootProvider.CreateScope())
            {
                var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

                try
                {
                    // 1) Attempt to validate the access token.
                    var (isValid, userId) = authService.ValidateTokenAsync(accessToken).Result;
                    if (isValid)
                    {
                        Console.WriteLine("Already good");
                        return userId;
                    }
                    else
                    {
                        // Token expired or invalid; attempt to refresh.
                        var resp = authService.RefreshAsync(refreshToken).Result;
                        if (resp == null)
                        {
                            Console.WriteLine("Fail for Expired or invalid => try refresh");
                            _client.SendEncryptedMessage("{\"Type\":\"AutoLoginFail\",\"Data\":{\"Reason\":\"Invalid tokens.\"}}");
                            return null;
                        }

                        // If refresh success => return new tokens to client so they store them
                        Console.WriteLine("Success for Expired or invalid => try refresh");
                        SendNewTokensToClient(resp);
                        return resp.UserId;
                    }
                }
                catch (Exception ex)
                {
                    _client.SendEncryptedMessage($"{{\"Type\":\"AutoLoginFail\",\"Data\":{{\"Reason\":\"{ex.Message}\"}}}}");
                    return null;
                }
            }
        }

        /// <summary>
        /// Sends new tokens to the client by constructing an AutoLoginSuccess JSON object and sending it as an encrypted message.
        /// </summary>
        /// <param name="resp">The AuthResponseDTO containing new token data.</param>
        private void SendNewTokensToClient(AuthResponseDTO resp)
        {
            var obj = new
            {
                Type = "AutoLoginSuccess",
                Data = new
                {
                    UserId = resp.UserId,
                    AccessToken = resp.AccessToken,
                    AccessTokenExpiry = resp.AccessTokenExpiry.ToString("o"),
                    RefreshToken = resp.RefreshToken,
                    RefreshTokenExpiry = resp.RefreshTokenExpiry.ToString("o")
                }
            };
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            _client.SendEncryptedMessage(json);
        }

        /// <summary>
        /// Handles the "RegisterUser" message by creating a new user, updating global stats,
        /// and performing auto-login. Returns a success or failure message to the client.
        /// </summary>
        /// <param name="msgObj">The JSON object representing the registration request.</param>
        private async void HandleRegisterUser(JObject msgObj)
        {
            // { "Type":"RegisterUser", "Data": {"Username":"...","Password":"..."}}
            JObject dataObj = (JObject)msgObj["Data"];
            string username = dataObj["Username"]?.ToString();
            string password = dataObj["Password"]?.ToString();

            using (var scope = _rootProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
                var globalStatsService = scope.ServiceProvider.GetRequiredService<IGlobalGameStatsService>();

                try
                {
                    // Check if user exists
                    var existing = await userService.GetUserByUsernameAsync(username);
                    if (existing != null)
                    {
                        string fail = "{\"Type\":\"RegisterFail\",\"Data\":{\"Reason\":\"Username already taken\"}}";
                        _client.SendEncryptedMessage(fail);
                        return;
                    }

                    // Create user
                    int userId = await userService.CreateUserAsync(new CreateUserDTO
                    {
                        Username = username,
                        Password = password,
                        Avatar = "temp_avatar.png"
                    });

                    // Increment total users in global stats.
                    await globalStatsService.IncrementTotalUsersAsync(1, 1);

                    // Auto-login user.
                    var authResp = await authService.LoginAsync(username, password);
                    if (authResp == null)
                    {
                        string fail2 = "{\"Type\":\"RegisterFail\",\"Data\":{\"Reason\":\"Could not auto-login\"}}";
                        _client.SendEncryptedMessage(fail2);
                        return;
                    }

                    _client.SetUserId(userId); // Store userId

                    var success = new
                    {
                        Type = "RegisterSuccess",
                        Data = new
                        {
                            UserId = userId,
                            AccessToken = authResp.AccessToken,
                            AccessTokenExpiry = authResp.AccessTokenExpiry,
                            RefreshToken = authResp.RefreshToken,
                            RefreshTokenExpiry = authResp.RefreshTokenExpiry
                        }
                    };
                    string successJson = Newtonsoft.Json.JsonConvert.SerializeObject(success);
                    _client.SendEncryptedMessage(successJson);
                }
                catch (Exception ex)
                {
                    string error = $"{{\"Type\":\"RegisterFail\",\"Data\":{{\"Reason\":\"{ex.Message}\"}}}}";
                    _client.SendEncryptedMessage(error);
                }
            }
        }

        /// <summary>
        /// Handles the "LoginUser" message by validating credentials and returning tokens to the client.
        /// Also stores the userId in the client handler.
        /// </summary>
        /// <param name="msgObj">The JSON object representing the login request.</param>
        private async void HandleLoginUser(JObject msgObj)
        {
            JObject dataObj = (JObject)msgObj["Data"];
            string username = dataObj["Username"]?.ToString();
            string password = dataObj["Password"]?.ToString();

            using (var scope = _rootProvider.CreateScope())
            {
                var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                try
                {
                    var authResp = await authService.LoginAsync(username, password);
                    if (authResp == null)
                    {
                        string failJson = "{\"Type\":\"LoginFail\",\"Data\":{\"Reason\":\"Incorrect username or password\"}}";
                        _client.SendEncryptedMessage(failJson);
                        return;
                    }

                    var user = await userService.GetUserByUsernameAsync(username);
                    _client.SetUserId(user.UserId); // Store userId

                    var success = new
                    {
                        Type = "LoginSuccess",
                        Data = new
                        {
                            UserId = user.UserId,
                            AccessToken = authResp.AccessToken,
                            AccessTokenExpiry = authResp.AccessTokenExpiry,
                            RefreshToken = authResp.RefreshToken,
                            RefreshTokenExpiry = authResp.RefreshTokenExpiry
                        }
                    };
                    string successJson = Newtonsoft.Json.JsonConvert.SerializeObject(success);
                    _client.SendEncryptedMessage(successJson);
                }
                catch (Exception ex)
                {
                    string fail = $"{{\"Type\":\"LoginFail\",\"Data\":{{\"Reason\":\"{ex.Message}\"}}}}";
                    _client.SendEncryptedMessage(fail);
                }
            }
        }

        /// <summary>
        /// Handles the "UpdateLastLogin" message by updating the user's last login time in the database.
        /// </summary>
        /// <param name="msgObj">The JSON object representing the update request.</param>
        private async void HandleUpdateLastLogin(JObject msgObj)
        {
            JObject dataObj = (JObject)msgObj["Data"];
            int currentUserId = dataObj["UserId"]?.ToObject<int>() ?? -1;

            using (var scope = _rootProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                try
                {
                    await userService.UpdateLastLoginAsync(currentUserId);
                    Console.WriteLine($"[ServerMessageHandler] Updated last login for user={currentUserId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ServerMessageHandler] UpdateLastLogin fail => {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handles the "GameOverDetailed" message by creating a game session record,
        /// updating user statistics, and incrementing global stats.
        /// </summary>
        /// <param name="msgObj">The JSON object representing the game over details.</param>
        private async void HandleGameOverDetailed(JObject msgObj)
        {
            JObject dataObj = (JObject)msgObj["Data"];
            int user1Id = dataObj["User1Id"]?.ToObject<int>() ?? -1;
            int? user2Id = dataObj["User2Id"]?.ToObject<int?>();
            string mode = dataObj["Mode"]?.ToString();
            int? wonUserId = dataObj["WonUserId"]?.ToObject<int?>();
            int finalWave = dataObj["FinalWave"]?.ToObject<int>() ?? 0;
            int timePlayed = dataObj["TimePlayed"]?.ToObject<int>() ?? 0;

            using (var scope = _rootProvider.CreateScope())
            {
                var gameSessionService = scope.ServiceProvider.GetRequiredService<IGameSessionService>();
                var userGameStatsService = scope.ServiceProvider.GetRequiredService<IUserGameStatsService>();
                var globalStatsService = scope.ServiceProvider.GetRequiredService<IGlobalGameStatsService>();

                try
                {
                    DateTime endTime = DateTime.UtcNow;
                    // Compute startTime by subtracting timePlayed seconds from the current time.
                    DateTime startTime = endTime.AddSeconds(-timePlayed);

                    var sessionDto = new GameSessionDTO
                    {
                        User1Id = user1Id,
                        User2Id = user2Id,
                        Mode = mode,
                        StartTime = startTime,
                        EndTime = endTime,
                        WonUserId = wonUserId,
                        FinalWave = finalWave,
                        TimePlayed = timePlayed
                    };
                    int sessionId = await gameSessionService.CreateSessionAsync(sessionDto);

                    bool isSinglePlayer = (mode == "SinglePlayer");
                    bool user1Won = (wonUserId == user1Id);
                    await userGameStatsService.IncrementGamesPlayedAsync(user1Id, user1Won, isSinglePlayer);

                    if (user2Id.HasValue)
                    {
                        bool user2Won = (wonUserId == user2Id);
                        await userGameStatsService.IncrementGamesPlayedAsync(user2Id.Value, user2Won, false);
                    }

                    await globalStatsService.IncrementGamesPlayedAsync(1, isSinglePlayer);

                    Console.WriteLine($"[ServerMessageHandler] GameOverDetailed => sessionId={sessionId}, timePlayed={timePlayed}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[ServerMessageHandler] GameOverDetailed fail => " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Handles the "AutoLogin" message by validating the provided access token.
        /// If the token is valid, returns the token as is; otherwise, attempts to refresh it.
        /// In either case, sends an AutoLoginSuccess message with the tokens if successful,
        /// or an AutoLoginFail message if not.
        /// </summary>
        /// <param name="msgObj">The JSON object representing the auto-login request.</param>
        private async void HandleAutoLogin(JObject msgObj)
        {
            JObject dataObj = (JObject)msgObj["Data"];
            string accessToken = dataObj["AccessToken"]?.ToString();
            string refreshToken = dataObj["RefreshToken"]?.ToString();

            Console.WriteLine("dataObj: " + dataObj);
            Console.WriteLine("accessToken: " + accessToken);
            Console.WriteLine("refreshToken: " + refreshToken);
            Console.WriteLine("refreshToken:" + refreshToken);

            using (var scope = _rootProvider.CreateScope())
            {
                var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
                try
                {
                    // 1) Attempt to validate the existing access token.
                    var (isValid, userId) = await authService.ValidateTokenAsync(accessToken);
                    Console.WriteLine("isValid: " + isValid);
                    if (isValid)
                    {
                        // Token is valid, so send the same token back.
                        _client.SetUserId(userId);
                        SendAutoLoginSuccess(userId, accessToken, null, refreshToken, null);
                        Console.WriteLine($"[ServerMessageHandler] AutoLogin => userId={userId} (token still valid)");
                    }
                    else
                    {
                        Console.WriteLine("refreshToken:" + refreshToken);
                        // 2) If token is invalid or expired, try to refresh.
                        var resp = await authService.RefreshAsync(refreshToken);
                        if (resp == null)
                        {
                            // Refresh failed: send AutoLoginFail.
                            SendAutoLoginFail("Expired or invalid access/refresh token. Must re-login.");
                            return;
                        }

                        // Refresh succeeded: send new tokens to the client.
                        _client.SetUserId(resp.UserId);
                        SendAutoLoginSuccess(
                            resp.UserId,
                            resp.AccessToken,
                            resp.AccessTokenExpiry,
                            resp.RefreshToken,
                            resp.RefreshTokenExpiry);

                        Console.WriteLine("[Server] AutoLogin success => userId=" + resp.UserId + " (refreshed token)");
                    }
                }
                catch (Exception ex)
                {
                    SendAutoLoginFail(ex.Message);
                }
            }
        }

        /// <summary>
        /// Constructs and sends an AutoLoginSuccess message to the client containing the provided tokens.
        /// </summary>
        /// <param name="userId">The user id associated with the tokens.</param>
        /// <param name="accessToken">The access token.</param>
        /// <param name="accessTokenExpiry">The expiry time for the access token.</param>
        /// <param name="refreshToken">The refresh token.</param>
        /// <param name="refreshTokenExpiry">The expiry time for the refresh token.</param>
        private void SendAutoLoginSuccess(int userId, string accessToken, DateTime? accessTokenExpiry, string refreshToken, DateTime? refreshTokenExpiry)
        {
            var obj = new
            {
                Type = "AutoLoginSuccess",
                Data = new
                {
                    UserId = userId,
                    AccessToken = accessToken,
                    AccessTokenExpiry = accessTokenExpiry?.ToString("o"),
                    RefreshToken = refreshToken,
                    RefreshTokenExpiry = refreshTokenExpiry?.ToString("o")
                }
            };
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(obj);
            _client.SendEncryptedMessage(json);
        }

        /// <summary>
        /// Sends an AutoLoginFail message to the client with the specified reason.
        /// </summary>
        /// <param name="reason">The reason for the login failure.</param>
        private void SendAutoLoginFail(string reason)
        {
            string fail = $"{{\"Type\":\"AutoLoginFail\",\"Data\":{{\"Reason\":\"{reason}\"}}}}";
            _client.SendEncryptedMessage(fail);
        }
    }
}
