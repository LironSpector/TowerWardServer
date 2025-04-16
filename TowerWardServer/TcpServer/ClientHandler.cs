using System;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Threading;
using System.Security.Cryptography;

namespace TcpServer
{
    /// <summary>
    /// Per-client connection. Reads data in a loop, performs the RSA handshake to establish AES encryption,
    /// and then passes decrypted messages to the ServerMessageHandler.
    /// </summary>
    public class ClientHandler
    {
        /// <summary>
        /// Gets the parent GameTcpServer responsible for handling matchmaking and client removal.
        /// </summary>
        private readonly GameTcpServer _server;

        /// <summary>
        /// Gets the root IServiceProvider for dependency injection (used to create scopes for DB/service access).
        /// </summary>
        private readonly IServiceProvider _rootProvider;

        #region Networking Fields

        /// <summary>
        /// The TcpClient instance representing the connected client.
        /// </summary>
        private readonly TcpClient _clientSocket;

        /// <summary>
        /// The NetworkStream obtained from the TcpClient for reading and writing data.
        /// </summary>
        private readonly NetworkStream _stream;

        #endregion

        #region Client State Fields

        /// <summary>
        /// The opponent ClientHandler, if the client is matched with another client.
        /// </summary>
        private ClientHandler _opponent;

        /// <summary>
        /// Gets or sets the current match wave index used for matchmaking rounds.
        /// </summary>
        public int matchWaveIndex = 0;

        /// <summary>
        /// Gets the user ID associated with the connected client after successful authentication.
        /// </summary>
        public int? UserId { get; private set; } = null;

        #endregion

        #region Encryption Fields

        /// <summary>
        /// The RSA instance used to generate a unique RSA keypair for this client connection.
        /// </summary>
        private RSA _rsa;

        /// <summary>
        /// The AES key derived from the client's handshake, used for decrypting subsequent messages.
        /// </summary>
        private byte[] _aesKey;

        /// <summary>
        /// The AES initialization vector derived from the client's handshake, used for decryption.
        /// </summary>
        private byte[] _aesIV;

        /// <summary>
        /// Indicates whether the initial handshake (RSA/AES exchange) with the client has been successfully completed.
        /// </summary>
        private bool _handshakeCompleted = false;

        #endregion

        #region Message Handling

        /// <summary>
        /// The ServerMessageHandler instance that processes decrypted messages from this client.
        /// </summary>
        private ServerMessageHandler _messageHandler;

        #endregion

        /// <summary>
        /// Initializes a new instance of the ClientHandler class.
        /// Generates a unique RSA keypair for this connection, sends the public key to the client,
        /// and initializes the ServerMessageHandler.
        /// </summary>
        /// <param name="clientSocket">The TcpClient representing the client connection.</param>
        /// <param name="server">The parent GameTcpServer instance.</param>
        /// <param name="rootProvider">The root IServiceProvider for creating DI scopes.</param>
        public ClientHandler(TcpClient clientSocket, GameTcpServer server, IServiceProvider rootProvider)
        {
            _clientSocket = clientSocket;
            _server = server;
            _rootProvider = rootProvider;

            _stream = _clientSocket.GetStream();

            // Generate a unique RSA key pair for this client connection (2048-bit for better security)
            _rsa = RSA.Create(2048);

            // Send the RSA public key to the client so they can encrypt the AES key and IV.
            SendPublicKey();

            // Initialize the server message handler, which will process subsequent messages.
            _messageHandler = new ServerMessageHandler(this, _rootProvider);
        }

        /// <summary>
        /// Main loop for receiving messages from the client.
        /// Reads length-prefixed messages, decrypts them (post-handshake), and passes them to the message handler.
        /// </summary>
        public void Process()
        {
            try
            {
                while (true)
                {
                    // 1) Read 4 bytes for message length.
                    byte[] lengthBuffer = new byte[4];
                    int bytesRead = _stream.Read(lengthBuffer, 0, 4);
                    if (bytesRead == 0) break; // connection closed
                    int messageLength = BitConverter.ToInt32(lengthBuffer, 0);

                    // 2) Read the actual message.
                    byte[] messageBuffer = new byte[messageLength];
                    int totalBytesRead = 0;
                    while (totalBytesRead < messageLength)
                    {
                        int read = _stream.Read(messageBuffer, totalBytesRead, messageLength - totalBytesRead);
                        if (read == 0) break;
                        totalBytesRead += read;
                    }
                    if (totalBytesRead < messageLength) break;

                    // 3) Convert the message bytes to a UTF-8 string.
                    string receivedData = Encoding.UTF8.GetString(messageBuffer, 0, totalBytesRead);

                    // 4) If handshake isn't complete, process handshake; otherwise, decrypt and handle message.
                    if (!_handshakeCompleted)
                    {
                        // Process unencrypted handshake messages.
                        HandleHandshakeMessage(receivedData);
                    }
                    else
                    {
                        // Decrypt the data using AES and then handle the JSON message.
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
                // Cleanup when the client disconnects.
                Console.WriteLine($"[ClientHandler] Client ended: {this}");

                // Notify the opponent if one exists.
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
        /// Processes the handshake message from the client, such as the "AESKeyExchange".
        /// Decrypts the AES key and IV using RSA and marks the handshake as complete.
        /// </summary>
        /// <param name="data">The received handshake message in string format.</param>
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

                // Decrypt the AES key and IV using RSA.
                _aesKey = _rsa.Decrypt(encKey, RSAEncryptionPadding.OaepSHA1);
                _aesIV = _rsa.Decrypt(encIV, RSAEncryptionPadding.OaepSHA1);

                _handshakeCompleted = true;

                Console.WriteLine("[ClientHandler] AES handshake complete. Key & IV established.");

                // Debug output for AES key and IV.
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
            var rsaParams = _rsa.ExportParameters(false); // Export only the public parameters.
            // Construct JSON message for the public key.
            JObject pubKeyJson = new JObject
            {
                ["Type"] = "ServerPublicKey",
                ["Modulus"] = Convert.ToBase64String(rsaParams.Modulus),
                ["Exponent"] = Convert.ToBase64String(rsaParams.Exponent)
            };

            SendRaw(pubKeyJson.ToString());
        }

        /// <summary>
        /// Sends a raw, unencrypted message to the client with a length prefix.
        /// This method is primarily used for sending handshake messages.
        /// </summary>
        /// <param name="msg">The message string to send.</param>
        private void SendRaw(string msg)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(msg);
            byte[] lengthBytes = BitConverter.GetBytes(messageBytes.Length);
            _stream.Write(lengthBytes, 0, lengthBytes.Length);
            _stream.Write(messageBytes, 0, messageBytes.Length);
        }

        /// <summary>
        /// Sends a message to the client after encrypting it with AES.
        /// The encrypted message is sent with a length prefix.
        /// </summary>
        /// <param name="plainJson">The plaintext JSON message to encrypt and send.</param>
        public void SendEncryptedMessage(string plainJson)
        {
            if (!_handshakeCompleted)
            {
                Console.WriteLine("[ClientHandler] ERROR: Attempt to send encrypted message before handshake!");
                return;
            }
            // Encrypt the plain JSON using AES.
            string cipherBase64 = AesEncryptionServer.EncryptAES(plainJson, _aesKey, _aesIV);

            // Send the encrypted message using the raw sending method.
            SendRaw(cipherBase64);
        }

        /// <summary>
        /// Sets the opponent client that is paired with this client.
        /// </summary>
        /// <param name="opponent">The ClientHandler representing the opponent.</param>
        public void SetOpponent(ClientHandler opponent)
        {
            _opponent = opponent;
        }

        /// <summary>
        /// Retrieves the current opponent client.
        /// </summary>
        /// <returns>The ClientHandler of the opponent, or null if none is set.</returns>
        public ClientHandler GetOpponent()
        {
            return _opponent;
        }

        /// <summary>
        /// Stores the authenticated user's ID in this client handler.
        /// </summary>
        /// <param name="userId">The user ID to store.</param>
        public void SetUserId(int userId)
        {
            this.UserId = userId;
        }

        /// <summary>
        /// Gets the parent GameTcpServer instance for this client.
        /// </summary>
        public GameTcpServer Server => _server;

        /// <summary>
        /// Returns a string representation of the client handler, including its remote endpoint and user ID.
        /// </summary>
        /// <returns>A string identifying the client handler.</returns>
        public override string ToString()
        {
            return $"ClientHandler({_clientSocket.Client.RemoteEndPoint}, UserId={UserId})";
        }
    }
}
