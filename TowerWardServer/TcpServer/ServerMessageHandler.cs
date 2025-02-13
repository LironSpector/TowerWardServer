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
    /// Also includes the methods for register/login, token checking, etc.
    /// </summary>
    public class ServerMessageHandler
    {
        private readonly ClientHandler _client;
        private readonly IServiceProvider _rootProvider;

        public ServerMessageHandler(ClientHandler client, IServiceProvider rootProvider)
        {
            _client = client;
            _rootProvider = rootProvider;
        }

        /// <summary>
        /// Main entry for a post-handshake, decrypted JSON.
        /// Splits into different "case" methods.
        /// </summary>
        public void HandleMessage(string data)
        {
            Console.WriteLine("[ServerMessageHandler] Decrypted data => " + data);

            JObject msgObj = JObject.Parse(data);
            string messageType = msgObj["Type"]?.ToString();

            // If not a login/registration or snapshot, parse tokenData
            if (messageType != "RegisterUser" && messageType != "LoginUser"
                && messageType != "AutoLogin" && messageType != "GameSnapshot")  //If it's one of these messges, the process is handled differently by the speicfic functions
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

                // 2) Validate or refresh
                int? userId = CheckAndRefreshTokens(accessToken, refreshToken);
                if (userId == null) return; // invalid => stop

                // If valid => user recognized
                _client.SetUserId(userId.Value);

                Console.WriteLine("CheckAndRefreshTokens - OK");
            }

            switch (messageType)
            {
                case "MatchmakingRequest":
                    _client.Server.AddToMatchmaking(_client);
                    break;

                case "SendBalloon":
                    ForwardToOpponent(data);
                    break;

                case "GameSnapshot":
                    ForwardToOpponent(data);
                    break;

                case "ShowSnapshots":
                    ForwardToOpponent(data);
                    break;

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
        /// For messages that simply get forwarded to the opponent (like SendBalloon, ShowSnapshots, etc.).
        /// </summary>
        private void ForwardToOpponent(string data)
        {
            var opp = _client.GetOpponent();
            if (opp != null)
            {
                opp.SendEncryptedMessage(data);
            }
        }

        /// <summary>
        /// For "UseMultiplayerAbility" we add "FromOpponent":true for the other side,
        /// then forward the entire message.
        /// </summary>
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
        /// For "WaveDone", check if it matches local wave index => increment, start next wave for both.
        /// </summary>
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
        /// For "GameOver": forward to opponent, then break the link.
        /// </summary>
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

        // ---------------------------------------------------------------------
        // DB / Auth Logic 
        // ---------------------------------------------------------------------
        private int? CheckAndRefreshTokens(string accessToken, string refreshToken)
        {
            using (var scope = _rootProvider.CreateScope())
            {
                var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();

                try
                {
                    // 1) Attempt Validate
                    var (isValid, userId) = authService.ValidateTokenAsync(accessToken).Result;
                    if (isValid)
                    {
                        // Already good
                        Console.WriteLine("Already good");
                        return userId;
                    }
                    else
                    {
                        // Expired or invalid => try refresh
                        var resp = authService.RefreshAsync(refreshToken).Result;
                        if (resp == null)
                        {
                            Console.WriteLine("Fail for Expired or invalid => try refresh");
                            // Also invalid => reject
                            _client.SendEncryptedMessage("{\"Type\":\"AutoLoginFail\",\"Data\":{\"Reason\":\"Invalid tokens.\"}}");
                            return null;
                        }

                        // If refresh success => return new tokens to client so they store them
                        // We also store userId in the client handler if you want
                        // e.g. userId = resp.UserId;
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
        /// "RegisterUser" => create user, increment global stats, auto-login, etc.
        /// </summary>
        private async void HandleRegisterUser(JObject msgObj)
        {
            // { "Type":"RegisterUser", "Data": {"Username":"...","Password":"..."}}
            JObject dataObj = (JObject)msgObj["Data"];
            string username = dataObj["Username"]?.ToString();
            string password = dataObj["Password"]?.ToString();

            // 1) Create a new scope
            using (var scope = _rootProvider.CreateScope())
            {
                // 2) Resolve the services
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
                        //Avatar = null // or pass some field
                    });

                    // Increment total_users in global_game_stats
                    await globalStatsService.IncrementTotalUsersAsync(1, 1);

                    // auto-login
                    var authResp = await authService.LoginAsync(username, password);
                    if (authResp == null)
                    {
                        string fail2 = "{\"Type\":\"RegisterFail\",\"Data\":{\"Reason\":\"Could not auto-login\"}}";
                        _client.SendEncryptedMessage(fail2);
                        return;
                    }

                    _client.SetUserId(userId); // <--- store the user id in this ClientHandler

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
        /// "LoginUser" => validate credentials, return tokens, store userId in client.
        /// </summary>
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
                    _client.SetUserId(user.UserId); // <--- store the user id in this ClientHandler

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
        /// "UpdateLastLogin" => set user.LastLogin = now in the DB.
        /// </summary>
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
        /// "GameOverDetailed" => create a session record, update user stats, etc.
        /// </summary>
        private async void HandleGameOverDetailed(JObject msgObj)
        {
            //Example structure:
            // { "Type":"GameOverDetailed",
            //   "Data": {
            //     "User1Id": 5,
            //     "User2Id": 9 or null,
            //     "Mode": "SinglePlayer" or "Multiplayer",
            //     "WonUserId": 5 or null,
            //     "FinalWave": 20,
            //     "TimePlayed": 300 // in seconds
            //   }
            // }

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
                    // We compute StartTime by subtracting timePlayed (seconds) from endTime
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

                    // user2 if present
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
        /// "AutoLogin" => validate or refresh tokens, then return new or same tokens if success.
        /// </summary>
        private async void HandleAutoLogin(JObject msgObj)
        {
            // e.g. {
            //   "Type":"AutoLogin",
            //   "Data":{
            //     "AccessToken":"...maybe expired...",
            //     "RefreshToken":"...maybe valid..."
            //   }
            // }

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
                    // 1) First try ValidateTokenAsync on the existing access token
                    var (isValid, userId) = await authService.ValidateTokenAsync(accessToken);
                    Console.WriteLine("isValid: " + isValid);
                    if (isValid)
                    {
                        // The old token is still good (not expired). Keep the same access token
                        _client.SetUserId(userId);
                        SendAutoLoginSuccess(userId, accessToken, null, refreshToken, null);
                        Console.WriteLine($"[ServerMessageHandler] AutoLogin => userId={userId} (token still valid)");
                    }
                    else
                    {
                        Console.WriteLine("refreshToken:" + refreshToken);
                        // 2) The access token is invalid or expired => Try refresh
                        var resp = await authService.RefreshAsync(refreshToken);
                        if (resp == null)
                        {
                            // refresh token also invalid => fail
                            SendAutoLoginFail("Expired or invalid access/refresh token. Must re-login.");
                            return;
                        }

                        // If refresh succeeded => we got new access token & refresh token
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

        private void SendAutoLoginSuccess(int userId, string accessToken, DateTime? accessTokenExpiry, string refreshToken, DateTime? refreshTokenExpiry)
        {
            // Return new or same tokens so the client can store them
            // If we didn't reissue the same, you can pass null for unused fields
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

        private void SendAutoLoginFail(string reason)
        {
            string fail = $"{{\"Type\":\"AutoLoginFail\",\"Data\":{{\"Reason\":\"{reason}\"}}}}";
            _client.SendEncryptedMessage(fail);
        }
    }
}
