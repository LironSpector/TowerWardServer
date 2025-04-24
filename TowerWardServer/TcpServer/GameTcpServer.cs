using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpServer
{
    /// <summary>
    /// Main TCP server that listens on a port, accepts connections,
    /// and handles matchmaking for queued clients.
    /// </summary>
    public class GameTcpServer
    {
        private TcpListener _tcpListener;
        private readonly int _port;

        // Keep track of all connected clients.
        private readonly List<ClientHandler> _clients = new List<ClientHandler>();

        // Matchmaking queue.
        private readonly Queue<ClientHandler> _waitingClients = new Queue<ClientHandler>();

        // Root IServiceProvider for DB/Service access inside client requests.
        private readonly IServiceProvider _rootProvider;

        /// <summary>
        /// Initializes a new instance of the GameTcpServer class.
        /// </summary>
        /// <param name="port">The port on which to listen for incoming connections.</param>
        /// <param name="rootProvider">The root IServiceProvider for dependency injection.</param>
        public GameTcpServer(int port, IServiceProvider rootProvider)
        {
            _port = port;
            _rootProvider = rootProvider;
        }

        /// <summary>
        /// Starts the listener on the configured port and accepts clients in a loop.
        /// For each new client connection, a ClientHandler is created and processed on a dedicated background thread.
        /// </summary>
        public void Start()
        {
            _tcpListener = new TcpListener(IPAddress.Any, _port);
            _tcpListener.Start();
            Console.WriteLine($"[GameTcpServer] Listening on port {_port}...");

            while (true)
            {
                // Wait for a new client connection.
                TcpClient clientSocket = _tcpListener.AcceptTcpClient();
                Console.WriteLine("[GameTcpServer] Client connected.");

                // Create a new handler for that client.
                ClientHandler clientHandler = new ClientHandler(clientSocket, this, _rootProvider);

                // Keep track of the new client.
                lock (_clients)
                {
                    _clients.Add(clientHandler);
                }

                // Start a thread to process the client.
                Thread clientThread = new Thread(clientHandler.Process);
                clientThread.IsBackground = true; // Set the thread as a background thread.
                clientThread.Start();
            }
        }

        /// <summary>
        /// Removes the given client from both the main clients list and the waiting queue if present.
        /// </summary>
        /// <param name="client">The ClientHandler to remove.</param>
        public void RemoveClient(ClientHandler client)
        {
            lock (_waitingClients)
            {
                if (_waitingClients.Contains(client))
                {
                    //A situation where a client will be removed and is also waiting is if he is the only
                    //one waiting, so dequeuing will dequeue that client from the queue.
                    _waitingClients.Dequeue();
                }
            }

            lock (_clients)
            {
                _clients.Remove(client);
            }
        }

        /// <summary>
        /// Queues a client for matchmaking. If there's an existing waiting client,
        /// match them immediately; otherwise, store the client in the waiting queue.
        /// </summary>
        /// <param name="client">The ClientHandler to add to the matchmaking queue.</param>
        public void AddToMatchmaking(ClientHandler client)
        {
            lock (_waitingClients)
            {
                Console.WriteLine($"[GameTcpServer] Current waiting count: {_waitingClients.Count}");
                if (_waitingClients.Count > 0)
                {
                    // Found an opponent in the queue; match them.
                    ClientHandler opponent = _waitingClients.Dequeue();
                    CreateMatch(client, opponent);
                }
                else
                {
                    // Enqueue the client and send a "MatchWaiting" message.
                    _waitingClients.Enqueue(client);
                    client.SendEncryptedMessage("{\"Type\":\"MatchWaiting\"}");
                }
                Console.WriteLine($"[GameTcpServer] Waiting count after: {_waitingClients.Count}");
            }
        }

        /// <summary>
        /// Matches two clients in a 1v1 scenario. Sets each as the opponent of the other
        /// and sends a "MatchFound" message containing the opponent's user ID.
        /// </summary>
        /// <param name="client1">The first ClientHandler.</param>
        /// <param name="client2">The second ClientHandler.</param>
        private void CreateMatch(ClientHandler client1, ClientHandler client2)
        {
            client1.SetOpponent(client2);
            client2.SetOpponent(client1);

            client1.matchWaveIndex = 0;
            client2.matchWaveIndex = 0;

            // Build JSON messages with the opponent's userId.
            int? userId1 = client1.UserId;
            int? userId2 = client2.UserId;

            string msg1 = $"{{\"Type\":\"MatchFound\",\"Data\":{{\"OpponentId\":{(userId2 ?? -1)}}}}}";
            string msg2 = $"{{\"Type\":\"MatchFound\",\"Data\":{{\"OpponentId\":{(userId1 ?? -1)}}}}}";

            // Send the match found message to both clients.
            // Each client must know the userId of his opponent, mostly for the database game session creation with UserId of both users
            client1.SendEncryptedMessage(msg1);
            client2.SendEncryptedMessage(msg2);

            Console.WriteLine($"[GameTcpServer] Match created: user1={userId1} vs user2={userId2}");
        }
    }
}
