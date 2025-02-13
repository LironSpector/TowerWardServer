// -------------------- New TcpServer - after adding AES & RSA encryption --------------------
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Hosting.Server;
using System.ComponentModel;
using System.Security.Cryptography;
using Services;
using DTOs;
using Models;
using Repositories;
using Microsoft.Extensions.DependencyInjection; // for CreateScope()

namespace TcpServer
{
    public class GameTcpServer
    {
        private TcpListener _tcpListener;
        private List<ClientHandler> _clients = new List<ClientHandler>();
        private Queue<ClientHandler> _waitingClients = new Queue<ClientHandler>();
        private readonly int _port;

        // We no longer store IUserService, IAuthenticationService directly here.
        // Instead, we store the *root* IServiceProvider:
        private readonly IServiceProvider _rootProvider;

        public GameTcpServer(int port, IServiceProvider rootProvider)
        {
            _port = port;
            _rootProvider = rootProvider;
        }

        public void Start()
        {
            _tcpListener = new TcpListener(IPAddress.Any, _port);
            _tcpListener.Start();
            Console.WriteLine($"TCP Server started on port {_port}");

            while (true)
            {
                TcpClient clientSocket = _tcpListener.AcceptTcpClient();
                Console.WriteLine("Client connected!");

                ClientHandler clientHandler = new ClientHandler(clientSocket, this, _rootProvider);


                lock (_clients)
                {
                    _clients.Add(clientHandler);
                }

                Thread clientThread = new Thread(clientHandler.Process);
                clientThread.Start();
            }
        }

        public void RemoveClient(ClientHandler client)
        {
            lock (_waitingClients)
            {
                if (_waitingClients.Contains(client))
                {
                    // If the client is waiting in the queue, remove it
                    // (Though Dequeue() alone won't remove that client 
                    //  unless it's at the front, so you might do a different approach.)
                    // For simplicity, we'll just remove from _clients
                    // but be aware of the queue order if needed.

                    _waitingClients.Dequeue(); //A situation where a client will be removed and is also waiting is if he is the only one waiting, so dequeuing will dequeue that client from the queue.
                }
            }

            lock (_clients)
            {
                _clients.Remove(client);
            }
        }

        public void AddToMatchmaking(ClientHandler client)
        {
            lock (_waitingClients)
            {
                Console.WriteLine("waitingClients.Count: " + _waitingClients.Count);
                if (_waitingClients.Count > 0)
                {
                    ClientHandler opponent = _waitingClients.Dequeue();
                    Console.WriteLine("Client: " + client + ", opponent: " + opponent);
                    CreateMatch(client, opponent);
                }
                else
                {
                    _waitingClients.Enqueue(client);
                    // Notify client they are waiting for an opponent
                    //client.SendMessage("{\"Type\":\"MatchWaiting\"}");
                    client.SendEncryptedMessage("{\"Type\":\"MatchWaiting\"}");
                }
                Console.WriteLine("waitingClients.Count - two: " + _waitingClients.Count);
            }
        }

        private void CreateMatch(ClientHandler client1, ClientHandler client2)
        {
            client1.SetOpponent(client2);
            client2.SetOpponent(client1);

            client1.matchWaveIndex = 0;
            client2.matchWaveIndex = 0;

            // We build a JSON with "OpponentId" = client2.UserId for client1
            // and "OpponentId" = client1.UserId for client2

            int? userId1 = client1.UserId; // might be null if user hasn’t logged in, but usually not
            int? userId2 = client2.UserId;

            // client1 sees user2 as OpponentId
            string msg1 = $"{{\"Type\":\"MatchFound\",\"Data\":{{\"OpponentId\":{(userId2 ?? -1)}}}}}";
            client1.SendEncryptedMessage(msg1);

            // client2 sees user1 as OpponentId
            string msg2 = $"{{\"Type\":\"MatchFound\",\"Data\":{{\"OpponentId\":{(userId1 ?? -1)}}}}}";
            client2.SendEncryptedMessage(msg2);

            Console.WriteLine("Match created between user1={0} and user2={1}", userId1, userId2);
        }

    }

    public class ClientHandler
    {
        private TcpClient _clientSocket;
        private GameTcpServer _server;
        private NetworkStream _stream;
        private byte[] _buffer = new byte[4096];

        private ClientHandler _opponent;
        public int matchWaveIndex = 0;

        // --- Encryption-specific ---
        private RSA _rsa;                    // Unique RSA keypair for this client connection
        private byte[] _aesKey;              // Once handshake is done
        private byte[] _aesIV;               // Once handshake is done
        private bool _handshakeCompleted = false;
        // --- end encryption fields ---


        // We store the *root* provider
        private readonly IServiceProvider _rootProvider;

        public int? UserId { get; private set; }

        public ClientHandler(TcpClient clientSocket, GameTcpServer server, IServiceProvider rootProvider)
        {
            _clientSocket = clientSocket;
            _server = server;
            _stream = clientSocket.GetStream();

            _rootProvider = rootProvider;

            // Generate a unique RSA key pair for this connection
            _rsa = RSA.Create(2048); // 2048-bit for better security

            // Immediately send public key
            SendPublicKey();
        }

        public void Process()
        {
            try
            {
                while (true)
                {
                    // Read 4 bytes for length
                    byte[] lengthBuffer = new byte[4];
                    int bytesRead = _stream.Read(lengthBuffer, 0, 4);
                    if (bytesRead == 0) break;
                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // Now read the message
                    byte[] messageBuffer = new byte[messageLength];
                    int totalBytesRead = 0;
                    while (totalBytesRead < messageLength)
                    {
                        int read = _stream.Read(messageBuffer, totalBytesRead, messageLength - totalBytesRead);
                        if (read == 0) break;
                        totalBytesRead += read;
                    }
                    if (totalBytesRead < messageLength) break; // connection closed?

                    // Convert to string
                    string receivedData = Encoding.UTF8.GetString(messageBuffer, 0, totalBytesRead);

                    if (!_handshakeCompleted)
                    {
                        // During handshake, messages are unencrypted JSON
                        HandleHandshakeMessage(receivedData);
                    }
                    else
                    {
                        // After handshake, the message is AES-encrypted base64
                        string decryptedJson = AesEncryptionServer.DecryptAES(receivedData, _aesKey, _aesIV);
                        HandleMessage(decryptedJson);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client disconnected: " + ex.Message);
            }
            finally
            {
                Console.WriteLine("Client disconnected (and is removed): " + this);

                if (_opponent != null)
                {
                    _opponent.SendEncryptedMessage("{\"Type\":\"OpponentDisconnected\"}");
                    _opponent.SetOpponent(null);
                }

                _clientSocket.Close();
                _server.RemoveClient(this);
            }
        }

        /// <summary>
        /// Called when we receive a message but the handshake hasn't completed.
        /// Possibly it's the AES key exchange from the client.
        /// </summary>
        private void HandleHandshakeMessage(string data)
        {
            Console.WriteLine("[Server] Handshake Phase - Received: " + data);
            // We expect something like:
            // {
            //   "Type": "AESKeyExchange",
            //   "EncryptedKey": "...",
            //   "EncryptedIV": "..."
            // }

            JObject jo = JObject.Parse(data);
            string msgType = jo["Type"].ToString();

            if (msgType == "AESKeyExchange")
            {
                string encKeyBase64 = jo["EncryptedKey"].ToString();
                string encIVBase64 = jo["EncryptedIV"].ToString();

                // Decrypt with our private RSA
                byte[] encKey = Convert.FromBase64String(encKeyBase64);
                byte[] encIV = Convert.FromBase64String(encIVBase64);

                //_aesKey = _rsa.Decrypt(encKey, RSAEncryptionPadding.OaepSHA256);
                //_aesIV = _rsa.Decrypt(encIV, RSAEncryptionPadding.OaepSHA256);
                _aesKey = _rsa.Decrypt(encKey, RSAEncryptionPadding.OaepSHA1);
                _aesIV = _rsa.Decrypt(encIV, RSAEncryptionPadding.OaepSHA1);

                Console.WriteLine("[Server] Successfully decrypted AES key/IV. Handshake complete!");
                Console.WriteLine("server _aesKey:");
                for (int i = 0; i < _aesKey.Length; i++)
                {
                    Console.Write(_aesKey[i]);
                }
                Console.WriteLine();
                Console.WriteLine("server _aesIV:");
                for (int i = 0; i < _aesIV.Length; i++)
                {
                    Console.Write(_aesIV[i]);
                }
                Console.WriteLine();

                _handshakeCompleted = true;
            }
            else
            {
                // Unexpected message in handshake phase
                Console.WriteLine("[Server] Unexpected handshake message type: " + msgType);
            }
        }

        /// <summary>
        /// Once handshake is done, we parse the JSON in the usual game-protocol way.
        /// </summary>
        private void HandleMessage(string data)
        {
            //Console.WriteLine("[Server] Decrypted data: " + data);
            //JObject messageObject = JObject.Parse(data);
            //string messageType = messageObject["Type"].ToString();


            Console.WriteLine("[Server] Decrypted data: " + data);
            JObject messageObject = JObject.Parse(data);
            string messageType = messageObject["Type"].ToString();


            if (messageType != "RegisterUser" && messageType != "LoginUser" && messageType != "AutoLogin" && messageType != "GameSnapshot") //If it's one of these messges, the process is handled differently by the speicfic functions
            {
                // 1) Extract the token data first
                JObject tokenData = (JObject)messageObject["TokenData"];
                if (tokenData == null)
                {
                    // No tokens => fail or treat as unauth
                    SendEncryptedMessage("{\"Type\":\"Error\",\"Data\":{\"Reason\":\"No TokenData in message.\"}}");
                    return;
                }

                string accessToken = tokenData["AccessToken"]?.ToString();
                string refreshToken = tokenData["RefreshToken"]?.ToString();

                // 2) Validate or refresh
                int? userId = CheckAndRefreshTokens(accessToken, refreshToken);
                if (userId == null)
                {
                    // invalid => bail
                    return;
                }
                // If valid => user recognized
                this.UserId = userId;
                Console.WriteLine("CheckAndRefreshTokens - OK");
            }



            switch (messageType)
            {
                case "MatchmakingRequest":
                    _server.AddToMatchmaking(this);
                    break;

                case "SendBalloon":
                    if (_opponent != null)
                    {
                        _opponent.SendEncryptedMessage(data);
                    }
                    break;

                case "GameSnapshot":
                    if (_opponent != null)
                    {
                        _opponent.SendEncryptedMessage(data);
                    }
                    break;

                case "ShowSnapshots":
                    if (_opponent != null)
                    {
                        _opponent.SendEncryptedMessage(data);
                    }
                    break;

                case "HideSnapshots":
                    if (_opponent != null)
                    {
                        _opponent.SendEncryptedMessage(data);
                    }
                    break;

                case "WaveDone":
                    {
                        //after changes in the message structure
                        JObject dataObj = (JObject)messageObject["Data"];
                        int waveFinishedIndex = dataObj["WaveIndex"].ToObject<int>();


                        Console.WriteLine($"Player finished wave {waveFinishedIndex}, matchWaveIndex= {matchWaveIndex}");

                        if (waveFinishedIndex == matchWaveIndex)
                        {
                            matchWaveIndex++;
                            if (_opponent != null)
                            {
                                _opponent.matchWaveIndex = matchWaveIndex;
                                this.matchWaveIndex = matchWaveIndex;

                                string startMsg = $"{{\"Type\":\"StartNextWave\",\"WaveIndex\":{matchWaveIndex}}}";
                                this.SendEncryptedMessage(startMsg);
                                _opponent.SendEncryptedMessage(startMsg);
                            }
                        }
                        break;
                    }

                case "UseMultiplayerAbility":
                    if (_opponent != null)
                    {
                        JObject jData = (JObject)messageObject["Data"];
                        jData["FromOpponent"] = true;
                        _opponent.SendEncryptedMessage(messageObject.ToString());
                    }
                    break;

                case "GameOver":
                    if (_opponent != null)
                    {
                        _opponent.SendEncryptedMessage(data);
                        _opponent.SetOpponent(null);
                        this.SetOpponent(null);
                    }
                    break;

                case "RegisterUser":
                    HandleRegisterUser(messageObject);
                    break;

                case "LoginUser":
                    HandleLoginUser(messageObject);
                    break;

                case "UpdateLastLogin":
                    {
                        JObject dataObj = (JObject)messageObject["Data"];
                        int currentUserId = dataObj["UserId"].ToObject<int>();

                        HandleUpdateLastLogin(currentUserId);
                        break;
                    }

                case "GameOverDetailed":
                    {
                        HandleGameOverDetailed(messageObject);
                        break;
                    }

                case "AutoLogin":
                    HandleAutoLogin(messageObject);
                    break;

                default:
                    Console.WriteLine("Unknown message type received: " + messageType);
                    break;
            }
        }


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
                            SendEncryptedMessage("{\"Type\":\"AutoLoginFail\",\"Data\":{\"Reason\":\"Access + Refresh tokens invalid. Re-login required.\"}}");
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
                    // Some error => fail
                    SendEncryptedMessage($"{{\"Type\":\"AutoLoginFail\",\"Data\":{{\"Reason\":\"{ex.Message}\"}}}}");
                    return null;
                }
            }
        }

        // Helper to re-issue tokens to the client, so it updates them
        private void SendNewTokensToClient(AuthResponseDTO resp)
        {
            var successObj = new
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
            string successJson = Newtonsoft.Json.JsonConvert.SerializeObject(successObj);
            SendEncryptedMessage(successJson);
        }


        /// <summary>
        /// Send the RSA public key unencrypted (in the handshake phase).
        /// </summary>
        private void SendPublicKey()
        {
            var rsaParams = _rsa.ExportParameters(false); // public only
            // Convert to some JSON structure
            // For example:
            // {
            //   "Type": "ServerPublicKey",
            //   "Modulus": "base64...",
            //   "Exponent": "base64..."
            // }
            JObject pubKeyJson = new JObject();
            pubKeyJson["Type"] = "ServerPublicKey";
            pubKeyJson["Modulus"] = Convert.ToBase64String(rsaParams.Modulus);
            pubKeyJson["Exponent"] = Convert.ToBase64String(rsaParams.Exponent);

            string jsonString = pubKeyJson.ToString();
            SendRaw(jsonString);
        }

        /// <summary>
        /// Send an AES-encrypted message *after* handshake. 
        /// </summary>
        public void SendEncryptedMessage(string plainJson)
        {
            if (!_handshakeCompleted)
            {
                // Possibly error or fallback to raw
                Console.WriteLine("[Server] ERROR: Trying to send encrypted message before handshake done!");
                return;
            }

            // 1) Encrypt with AES
            string cipherBase64 = AesEncryptionServer.EncryptAES(plainJson, _aesKey, _aesIV);

            // 2) Send length + cipher
            SendRaw(cipherBase64);
        }

        /// <summary>
        /// Sends a string "as is" (with 4-byte length) without AES. 
        /// Used for handshake messages or sending public key.
        /// </summary>
        private void SendRaw(string msg)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
            _stream.Write(lengthBytes, 0, lengthBytes.Length);
            _stream.Write(messageBytes, 0, messageBytes.Length);
        }


        // -----------------------------------------------------
        // CREATE A SCOPE FOR DB CALLS
        // -----------------------------------------------------

        private async void HandleRegisterUser(JObject msgObj)
        {
            // { "Type":"RegisterUser", "Data": {"Username":"...","Password":"..."}}
            JObject dataObj = (JObject)msgObj["Data"];
            string username = dataObj["Username"].ToString();
            string password = dataObj["Password"].ToString();

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
                    var existingUser = await userService.GetUserByUsernameAsync(username);
                    if (existingUser != null)
                    {
                        string fail = "{\"Type\":\"RegisterFail\",\"Data\":{\"Reason\":\"Username already taken\"}}";
                        SendEncryptedMessage(fail);
                        return;
                    }

                    // Create user
                    int userId = await userService.CreateUserAsync(new DTOs.CreateUserDTO
                    {
                        Username = username,
                        Password = password,
                        Avatar = "temp_avatar.png"
                        //Avatar = null // or pass some field
                    });

                    // Increment total_users in global_game_stats (assuming ID=1 is the primary row)
                    await globalStatsService.IncrementTotalUsersAsync(1, 1);

                    Console.WriteLine("VVVVVVVVVVVV");
                    // Auto-login
                    var authResp = await authService.LoginAsync(username, password);
                    if (authResp == null)
                    {
                        string fail2 = "{\"Type\":\"RegisterFail\",\"Data\":{\"Reason\":\"Could not auto-login\"}}";
                        SendEncryptedMessage(fail2);
                        return;
                    }

                    this.UserId = userId; // <--- store the user id in this ClientHandler

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
                    SendEncryptedMessage(successJson);
                }
                catch (Exception ex)
                {
                    string error = $"{{\"Type\":\"RegisterFail\",\"Data\":{{\"Reason\":\"{ex.Message}\"}}}}";
                    SendEncryptedMessage(error);
                }
            } // scope disposed here
        }

        private async void HandleLoginUser(JObject msgObj)
        {
            // { "Type":"LoginUser","Data": {"Username":"...","Password":"..."}}
            JObject dataObj = (JObject)msgObj["Data"];
            string username = dataObj["Username"].ToString();
            string password = dataObj["Password"].ToString();

            using (var scope = _rootProvider.CreateScope())
            {
                var authService = scope.ServiceProvider.GetRequiredService<IAuthenticationService>();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                try
                {
                    var authResp = await authService.LoginAsync(username, password);
                    if (authResp == null)
                    {
                        string fail =
                          "{\"Type\":\"LoginFail\",\"Data\":{\"Reason\":\"The Username or Password is Incorrect. Try again\"}}";
                        SendEncryptedMessage(fail);
                        return;
                    }

                    var user = await userService.GetUserByUsernameAsync(username);
                    this.UserId = user.UserId; // <--- store the user id in this ClientHandler

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
                    SendEncryptedMessage(successJson);
                }
                catch (Exception ex)
                {
                    string fail =
                      $"{{\"Type\":\"LoginFail\",\"Data\":{{\"Reason\":\"{ex.Message}\"}}}}";
                    SendEncryptedMessage(fail);
                }
            } // scope disposed
        }

        private async void HandleUpdateLastLogin(int userId)
        {
            using (var scope = _rootProvider.CreateScope())
            {
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

                try
                {
                    // This sets user.LastLogin = DateTime.UtcNow in the DB
                    await userService.UpdateLastLoginAsync(userId);
                    Console.WriteLine($"[Server] Updated last login for user ID={userId}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Server] UpdateLastLogin failed for userId={userId}: {ex.Message}");
                    // Optionally send a response or error message back if you want
                }
            }
        }


        private async void HandleGameOverDetailed(JObject msgObj)
        {
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
            int user1Id = dataObj["User1Id"].ToObject<int>();
            int? user2Id = dataObj["User2Id"]?.ToObject<int?>();
            string mode = dataObj["Mode"].ToString();
            int? wonUserId = dataObj["WonUserId"]?.ToObject<int?>();
            int finalWave = dataObj["FinalWave"].ToObject<int>();
            int timePlayed = dataObj["TimePlayed"].ToObject<int>();

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

                    int newSessionId = await gameSessionService.CreateSessionAsync(sessionDto);

                    bool isSinglePlayer = (mode == "SinglePlayer");
                    // user1 stats
                    bool user1Won = (wonUserId == user1Id);
                    await userGameStatsService.IncrementGamesPlayedAsync(user1Id, user1Won, isSinglePlayer);

                    // user2 if present
                    if (user2Id.HasValue)
                    {
                        bool user2Won = (wonUserId == user2Id);
                        await userGameStatsService.IncrementGamesPlayedAsync(user2Id.Value, user2Won, false);
                    }

                    // Update global stats
                    await globalStatsService.IncrementGamesPlayedAsync(1, isSinglePlayer);

                    Console.WriteLine($"[Server] GameOverDetailed processed. sessionId={newSessionId}, timePlayed={timePlayed}s");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[Server] GameOverDetailed failed: " + ex.Message);
                }
            }
        }


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
                    Console.WriteLine("At first in HandleAutoLogin");
                    var (isValid, userId) = await authService.ValidateTokenAsync(accessToken);
                    Console.WriteLine("isValid: " + isValid);
                    if (isValid)
                    {
                        // The old token is still good (not expired).
                        // Optionally reissue a new token if you want to refresh its expiry – or just return the same.
                        // For simplicity, let's keep the same access token:
                        this.UserId = userId;
                        SendAutoLoginSuccess(userId, accessToken, null, refreshToken, null);
                        Console.WriteLine("[Server] AutoLogin success => userId=" + userId + " (existing token still valid)");
                        return;
                    }
                    else
                    {
                        Console.WriteLine("refreshToken:" + refreshToken);
                        // 2) The access token is invalid or expired => Try refresh
                        var authResp = await authService.RefreshAsync(refreshToken);
                        if (authResp == null)
                        {
                            // refresh token also invalid => fail
                            SendAutoLoginFail("Expired or invalid access/refresh token. Must do full login.");
                            return;
                        }

                        // If refresh succeeded => we got new access token & refresh token
                        this.UserId = authResp.UserId; // If you want the connection recognized
                        SendAutoLoginSuccess(
                            authResp.UserId,
                            authResp.AccessToken,
                            authResp.AccessTokenExpiry,
                            authResp.RefreshToken,
                            authResp.RefreshTokenExpiry);

                        Console.WriteLine("[Server] AutoLogin success => userId=" + authResp.UserId + " (refreshed token)");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("catch block activated");
                    SendAutoLoginFail(ex.Message);
                }
            }
        }

        private void SendAutoLoginSuccess(
            int userId,
            string accessToken,
            DateTime? accessTokenExpiry,
            string refreshToken,
            DateTime? refreshTokenExpiry)
        {
            // Return new or same tokens so the client can store them
            // If we didn't reissue the same, you can pass null for unused fields

            var successObj = new
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
            string successJson = Newtonsoft.Json.JsonConvert.SerializeObject(successObj);
            SendEncryptedMessage(successJson);
        }

        private void SendAutoLoginFail(string reason)
        {
            string failJson = $"{{\"Type\":\"AutoLoginFail\",\"Data\":{{\"Reason\":\"{reason}\"}}}}";
            SendEncryptedMessage(failJson);
        }


        public void SetOpponent(ClientHandler opponent)
        {
            _opponent = opponent;
        }

        public override string ToString()
        {
            return $"ClientHandler({_clientSocket.Client.RemoteEndPoint})";
        }
    }
}


namespace TcpServer
{
    public static class AesEncryptionServer
    {
        public static string EncryptAES(string plainText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(encrypted);
                }
            }
        }

        public static string DecryptAES(string cipherBase64, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] cipherBytes = Convert.FromBase64String(cipherBase64);
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
    }
}
