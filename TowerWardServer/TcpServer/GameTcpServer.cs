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

namespace TcpServer
{
    public class GameTcpServer
    {
        private TcpListener _tcpListener;
        private List<ClientHandler> _clients = new List<ClientHandler>();
        private Queue<ClientHandler> _waitingClients = new Queue<ClientHandler>();
        private readonly int _port;

        public GameTcpServer(int port)
        {
            _port = port;
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

                ClientHandler clientHandler = new ClientHandler(clientSocket, this);
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

            //client1.SendMessage("{\"Type\":\"MatchFound\"}");
            //client2.SendMessage("{\"Type\":\"MatchFound\"}");
            client1.SendEncryptedMessage("{\"Type\":\"MatchFound\"}");
            client2.SendEncryptedMessage("{\"Type\":\"MatchFound\"}");
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

        public ClientHandler(TcpClient clientSocket, GameTcpServer server)
        {
            _clientSocket = clientSocket;
            _server = server;
            _stream = clientSocket.GetStream();

            // Generate a unique RSA key pair for this connection
            _rsa = RSA.Create(2048); // 2048-bit for better security

            // As soon as we create this handler, send our RSA public key to the client (unencrypted)
            // The client will parse it to encrypt its AES key+IV
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
                Console.WriteLine("AA");
                string encKeyBase64 = jo["EncryptedKey"].ToString();
                string encIVBase64 = jo["EncryptedIV"].ToString();
                Console.WriteLine("BB");

                // Decrypt with our private RSA
                byte[] encKey = Convert.FromBase64String(encKeyBase64);
                byte[] encIV = Convert.FromBase64String(encIVBase64);
                Console.WriteLine("CC");

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
            Console.WriteLine("[Server] Decrypted data: " + data);
            JObject messageObject = JObject.Parse(data);
            string messageType = messageObject["Type"].ToString();

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
                        int waveFinishedIndex = (int)messageObject["WaveIndex"];
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

                default:
                    Console.WriteLine("Unknown message type received: " + messageType);
                    break;
            }
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















// -------------------- Previous TcpServer - before adding AES & RSA encryption --------------------
//using System;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.Threading;
//using System.Collections.Generic;
//using Newtonsoft.Json.Linq;
//using Microsoft.AspNetCore.Hosting.Server;
//using System.ComponentModel;
//using System.Security.Cryptography;

//namespace TcpServer
//{
//    public class GameTcpServer
//    {
//        private TcpListener _tcpListener;
//        private List<ClientHandler> _clients = new List<ClientHandler>();
//        private Queue<ClientHandler> _waitingClients = new Queue<ClientHandler>();
//        private readonly int _port;

//        public GameTcpServer(int port)
//        {
//            _port = port;
//        }

//        public void Start()
//        {
//            _tcpListener = new TcpListener(IPAddress.Any, _port);
//            _tcpListener.Start();
//            Console.WriteLine($"TCP Server started on port {_port}");

//            while (true)
//            {
//                TcpClient clientSocket = _tcpListener.AcceptTcpClient();
//                Console.WriteLine("Client connected!");

//                ClientHandler clientHandler = new ClientHandler(clientSocket, this);
//                lock (_clients)
//                {
//                    _clients.Add(clientHandler);
//                }

//                Thread clientThread = new Thread(clientHandler.Process);
//                clientThread.Start();
//            }
//        }

//        public void RemoveClient(ClientHandler client)
//        {
//            lock (_waitingClients)
//            {
//                if (_waitingClients.Contains(client))
//                {
//                    // If the client is waiting in the queue, remove it
//                    // (Though Dequeue() alone won't remove that client 
//                    //  unless it's at the front, so you might do a different approach.)
//                    // For simplicity, we'll just remove from _clients
//                    // but be aware of the queue order if needed.

//                    _waitingClients.Dequeue(); //A situation where a client will be removed and is also waiting is if he is the only one waiting, so dequeuing will dequeue that client from the queue.
//                }
//            }

//            lock (_clients)
//            {
//                _clients.Remove(client);
//            }
//        }

//        public void AddToMatchmaking(ClientHandler client)
//        {
//            lock (_waitingClients)
//            {
//                Console.WriteLine("waitingClients.Count: " + _waitingClients.Count);
//                if (_waitingClients.Count > 0)
//                {
//                    ClientHandler opponent = _waitingClients.Dequeue();
//                    Console.WriteLine("Client: " + client + ", opponent: " + opponent);
//                    CreateMatch(client, opponent);
//                }
//                else
//                {
//                    _waitingClients.Enqueue(client);
//                    // Notify client they are waiting for an opponent
//                    client.SendMessage("{\"Type\":\"MatchWaiting\"}");
//                }
//                Console.WriteLine("waitingClients.Count - two: " + _waitingClients.Count);
//            }
//        }

//        private void CreateMatch(ClientHandler client1, ClientHandler client2)
//        {
//            client1.SetOpponent(client2);
//            client2.SetOpponent(client1);

//            client1.matchWaveIndex = 0;
//            client2.matchWaveIndex = 0;

//            client1.SendMessage("{\"Type\":\"MatchFound\"}");
//            client2.SendMessage("{\"Type\":\"MatchFound\"}");
//        }
//    }

//    public class ClientHandler
//    {
//        private TcpClient _clientSocket;
//        private GameTcpServer _server;
//        private NetworkStream _stream;
//        private byte[] _buffer = new byte[4096];

//        private ClientHandler _opponent;
//        public int matchWaveIndex = 0;

//        public ClientHandler(TcpClient clientSocket, GameTcpServer server)
//        {
//            _clientSocket = clientSocket;
//            _server = server;
//            _stream = clientSocket.GetStream();
//        }

//        public void Process()
//        {
//            try
//            {
//                while (true)
//                {
//                    byte[] lengthBuffer = new byte[4];
//                    int bytesRead = _stream.Read(lengthBuffer, 0, 4);
//                    if (bytesRead == 0) break;
//                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

//                    byte[] messageBuffer = new byte[messageLength];
//                    int totalBytesRead = 0;
//                    while (totalBytesRead < messageLength)
//                    {
//                        int read = _stream.Read(messageBuffer, totalBytesRead, messageLength - totalBytesRead);
//                        if (read == 0) break;
//                        totalBytesRead += read;
//                    }

//                    if (totalBytesRead < messageLength)
//                    {
//                        // Connection closed in the middle of a message
//                        break;
//                    }

//                    string data = Encoding.UTF8.GetString(messageBuffer, 0, totalBytesRead);
//                    HandleMessage(data);
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine("Client disconnected: " + ex.Message);
//            }
//            finally
//            {
//                Console.WriteLine("Client disconnected (and is removed): " + this);

//                if (_opponent != null)
//                {
//                    _opponent.SendMessage("{\"Type\":\"OpponentDisconnected\"}");
//                    _opponent.SetOpponent(null);
//                }

//                _clientSocket.Close();
//                _server.RemoveClient(this);
//            }
//        }

//        private void HandleMessage(string data)
//        {
//            Console.WriteLine("Data: " + data);
//            JObject messageObject = JObject.Parse(data);
//            string messageType = messageObject["Type"].ToString();

//            switch (messageType)
//            {
//                case "MatchmakingRequest":
//                    _server.AddToMatchmaking(this);
//                    Console.WriteLine("AddedToMatchmaking a client");
//                    break;

//                case "SendBalloon":
//                    if (_opponent != null)
//                    {
//                        // Relay the message to the opponent
//                        _opponent.SendMessage(data);
//                    }
//                    break;

//                case "GameSnapshot":
//                    if (_opponent != null)
//                    {
//                        // Relay the snapshot to the opponent
//                        _opponent.SendMessage(data);
//                    }
//                    break;

//                case "ShowSnapshots":
//                    if (_opponent != null)
//                    {
//                        // Relay the message to the opponent
//                        _opponent.SendMessage(data);
//                    }
//                    break;

//                case "HideSnapshots":
//                    if (_opponent != null)
//                    {
//                        // Relay the message to the opponent
//                        _opponent.SendMessage(data);
//                    }
//                    break;

//                case "WaveDone":
//                    {
//                        int waveFinishedIndex = (int)messageObject["WaveIndex"];
//                        Console.WriteLine($"Player finished wave {waveFinishedIndex}, matchWaveIndex= {matchWaveIndex}");

//                        // If waveFinishedIndex matches the matchWaveIndex we currently have
//                        if (waveFinishedIndex == matchWaveIndex)
//                        {
//                            matchWaveIndex++; // Next wave
//                            if (_opponent != null)
//                            {
//                                _opponent.matchWaveIndex = matchWaveIndex;
//                                this.matchWaveIndex = matchWaveIndex;

//                                // Tell both clients to spawn wave matchWaveIndex
//                                string startMsg = $"{{\"Type\":\"StartNextWave\",\"WaveIndex\":{matchWaveIndex}}}";
//                                this.SendMessage(startMsg);
//                                _opponent.SendMessage(startMsg);
//                            }
//                        }
//                        break;
//                    }

//                case "UseMultiplayerAbility":
//                    if (_opponent != null)
//                    {
//                        // Insert "FromOpponent":true in the JSON to let the opponent know
//                        JObject jData = (JObject)messageObject["Data"];
//                        jData["FromOpponent"] = true;

//                        // Then forward
//                        _opponent.SendMessage(messageObject.ToString());
//                    }
//                    // If there's also a local effect, you might do it here or the client does it after sending
//                    break;

//                case "GameOver":
//                    if (_opponent != null)
//                    {
//                        // Relay the game over message to the opponent
//                        _opponent.SendMessage(data);

//                        //The opponent of both players is reset to null since the game is over and they don't have an opponent right now
//                        _opponent.SetOpponent(opponent: null);
//                        this.SetOpponent(opponent: null);
//                    }
//                    break;

//                // Handle other message types
//                default:
//                    Console.WriteLine("Unknown message type received: " + messageType);
//                    break;
//            }
//        }

//        public void SendMessage(string message)
//        {
//            byte[] messageBytes = Encoding.UTF8.GetBytes(message);
//            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);

//            _stream.Write(lengthBytes, 0, lengthBytes.Length);
//            _stream.Write(messageBytes, 0, messageBytes.Length);
//        }

//        public void SetOpponent(ClientHandler opponent)
//        {
//            _opponent = opponent;
//        }

//        public override string ToString()
//        {
//            return $"ClientHandler({_clientSocket.Client.RemoteEndPoint})";
//        }
//    }


//    public static class EncryptionHelper
//    {
//        private static readonly byte[] key;
//        private static readonly byte[] iv;

//        // Static constructor to initialize the key and IV
//        static EncryptionHelper()
//        {
//            key = GenerateRandomBytes(32); // 256-bit key
//            iv = GenerateRandomBytes(16);  // 128-bit IV

//            Console.WriteLine($"Generated Key: {Convert.ToBase64String(key)}");
//            Console.WriteLine($"Generated IV: {Convert.ToBase64String(iv)}");
//        }

//        // Method to generate random bytes
//        private static byte[] GenerateRandomBytes(int length)
//        {
//            byte[] randomBytes = new byte[length];
//            using (var rng = RandomNumberGenerator.Create())
//            {
//                rng.GetBytes(randomBytes);
//            }
//            return randomBytes;
//        }

//        public static string Encrypt(string plainText)
//        {
//            using (Aes aes = Aes.Create())
//            {
//                aes.Key = key;
//                aes.IV = iv;

//                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
//                {
//                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
//                    byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

//                    return Convert.ToBase64String(encryptedBytes);
//                }
//            }
//        }

//        public static string Decrypt(string cipherText)
//        {
//            using (Aes aes = Aes.Create())
//            {
//                aes.Key = key;
//                aes.IV = iv;

//                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
//                {
//                    byte[] cipherBytes = Convert.FromBase64String(cipherText);
//                    byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

//                    return Encoding.UTF8.GetString(decryptedBytes);
//                }
//            }
//        }
//    }
//}

