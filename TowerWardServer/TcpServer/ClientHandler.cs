using System;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Security.Cryptography;

namespace TcpServer
{
    /// <summary>
    /// Per-client connection. Reads data in a loop, does RSA handshake,
    /// then passes decrypted messages to ServerMessageHandler.
    /// </summary>
    public class ClientHandler
    {
        // The parent server for matchmaking, removal, etc.
        private readonly GameTcpServer _server;
        private readonly IServiceProvider _rootProvider;

        // Networking
        private readonly TcpClient _clientSocket;
        private readonly NetworkStream _stream;
        private readonly byte[] _buffer = new byte[4096];

        // Opponent reference (if matched)
        private ClientHandler _opponent;

        public int matchWaveIndex = 0;
        public int? UserId { get; private set; } = null;

        // Encryption 
        private RSA _rsa; // Unique RSA keypair for this client connection
        private byte[] _aesKey; // Once handshake is done
        private byte[] _aesIV; // Once handshake is done
        private bool _handshakeCompleted = false;

        // A sub-handler for messages
        private ServerMessageHandler _messageHandler;

        public ClientHandler(TcpClient clientSocket, GameTcpServer server, IServiceProvider rootProvider)
        {
            _clientSocket = clientSocket;
            _server = server;
            _rootProvider = rootProvider;

            _stream = _clientSocket.GetStream();

            // Generate a unique RSA key pair for this connection 
            // and send the public key to the client
            _rsa = RSA.Create(2048); // 2048-bit for better security

            SendPublicKey();

            // Initialize the server message handler, pass 'this' so it can:
            //    - read or set 'UserId'
            //    - call 'SendEncryptedMessage(...)', etc.
            _messageHandler = new ServerMessageHandler(this, _rootProvider);
        }

        /// <summary>
        /// Main loop for receiving messages: read length prefix, 
        /// read data, decrypt (if handshake done), handle message, etc.
        /// </summary>
        public void Process()
        {
            try
            {
                while (true)
                {
                    // 1) Read 4 bytes for length
                    byte[] lengthBuffer = new byte[4];
                    int bytesRead = _stream.Read(lengthBuffer, 0, 4);
                    if (bytesRead == 0) break; // connection closed
                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // 2) Read message
                    byte[] messageBuffer = new byte[messageLength];
                    int totalBytesRead = 0;
                    while (totalBytesRead < messageLength)
                    {
                        int read = _stream.Read(messageBuffer, totalBytesRead, messageLength - totalBytesRead);
                        if (read == 0) break;
                        totalBytesRead += read;
                    }
                    if (totalBytesRead < messageLength) break;

                    // 3) Convert to string
                    string receivedData = Encoding.UTF8.GetString(messageBuffer, 0, totalBytesRead);

                    // Handshake or normal message
                    if (!_handshakeCompleted)
                    {
                        // Unencrypted handshake step
                        HandleHandshakeMessage(receivedData);
                    }
                    else
                    {
                        // Decrypt with AES, then pass to message handler
                        string decryptedJson = AesEncryptionServer.DecryptAES(receivedData, _aesKey, _aesIV);
                        _messageHandler.HandleMessage(decryptedJson);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ClientHandler] Client disconnected: " + ex.Message);
            }
            finally
            {
                // Cleanup
                Console.WriteLine($"[ClientHandler] Client ended: {this}");

                // Notify opponent if we had one
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
        /// Process the handshake message (e.g., "AESKeyExchange")
        /// before we set _handshakeCompleted = true.
        /// Called when we receive a message but the handshake hasn't completed. Possibly it's the AES key exchange from the client.
        /// </summary>
        private void HandleHandshakeMessage(string data)
        {
            Console.WriteLine("[ClientHandler] Handshake Phase - received => " + data);
            JObject jo = JObject.Parse(data);
            string msgType = jo["Type"].ToString();

            if (msgType == "AESKeyExchange")
            {
                string encKeyBase64 = jo["EncryptedKey"].ToString();
                string encIVBase64 = jo["EncryptedIV"].ToString();

                byte[] encKey = Convert.FromBase64String(encKeyBase64);
                byte[] encIV = Convert.FromBase64String(encIVBase64);

                // Decrypt with RSA
                _aesKey = _rsa.Decrypt(encKey, RSAEncryptionPadding.OaepSHA1);
                _aesIV = _rsa.Decrypt(encIV, RSAEncryptionPadding.OaepSHA1);

                _handshakeCompleted = true;

                Console.WriteLine("[ClientHandler] AES handshake complete. Key & IV established.");


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
            }
            else
            {
                Console.WriteLine("[ClientHandler] Unexpected handshake message => " + msgType);
            }
        }

        /// <summary>
        /// Sends the server's RSA public key unencrypted to the client.
        /// </summary>
        private void SendPublicKey()
        {
            var rsaParams = _rsa.ExportParameters(false); // public only
            // e.g.: { "Type":"ServerPublicKey", "Modulus":"...", "Exponent":"..." }
            JObject pubKeyJson = new JObject
            {
                ["Type"] = "ServerPublicKey",
                ["Modulus"] = Convert.ToBase64String(rsaParams.Modulus),
                ["Exponent"] = Convert.ToBase64String(rsaParams.Exponent)
            };

            SendRaw(pubKeyJson.ToString());
        }

        /// <summary>
        /// Sends a message unencrypted (raw) with length prefix.
        /// Used only for handshake messages (like public key).
        /// </summary>
        private void SendRaw(string msg)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
            _stream.Write(lengthBytes, 0, lengthBytes.Length);
            _stream.Write(messageBytes, 0, messageBytes.Length);
        }

        /// <summary>
        /// Sends a message AES-encrypted (post-handshake) with length prefix.
        /// </summary>
        public void SendEncryptedMessage(string plainJson)
        {
            if (!_handshakeCompleted)
            {
                Console.WriteLine("[ClientHandler] ERROR: Attempt to send encrypted message before handshake!");
                return;
            }
            // 1) Encrypt
            string cipherBase64 = AesEncryptionServer.EncryptAES(plainJson, _aesKey, _aesIV);

            // 2) Send length + cipher
            SendRaw(cipherBase64);
        }

        /// <summary>
        /// Called by the server or by the message handler to set an opponent reference.
        /// </summary>
        public void SetOpponent(ClientHandler opponent)
        {
            _opponent = opponent;
        }

        /// <summary>
        /// Exposes the opponent reference if needed by the message handler.
        /// </summary>
        public ClientHandler GetOpponent()
        {
            return _opponent;
        }

        /// <summary>
        /// Allows the message handler to store the user id once we validate tokens or login.
        /// </summary>
        public void SetUserId(int userId)
        {
            this.UserId = userId;
        }

        public GameTcpServer Server => _server;

        public override string ToString()
        {
            return $"ClientHandler({_clientSocket.Client.RemoteEndPoint}, UserId={UserId})";
        }
    }
}
