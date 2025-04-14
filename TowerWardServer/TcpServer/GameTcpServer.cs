//// -------------------- New TcpServer - after organizing and dividing TcpServer to several classes --------------------
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection; // for CreateScope()
//using Microsoft.AspNetCore.Hosting.Server;

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

        // Keep track of all connected clients
        private readonly List<ClientHandler> _clients = new List<ClientHandler>();

        // Matchmaking queue
        private readonly Queue<ClientHandler> _waitingClients = new Queue<ClientHandler>();

        // Root IServiceProvider for DB/Service access inside client requests
        private readonly IServiceProvider _rootProvider;

        public GameTcpServer(int port, IServiceProvider rootProvider)
        {
            _port = port;
            _rootProvider = rootProvider;
        }

        /// <summary>
        /// Starts the listener on the given port. Accepts clients in a loop.
        /// </summary>
        public void Start()
        {
            _tcpListener = new TcpListener(IPAddress.Any, _port);
            _tcpListener.Start();
            Console.WriteLine($"[GameTcpServer] Listening on port {_port}...");

            while (true)
            {
                // Wait for a new client connection
                TcpClient clientSocket = _tcpListener.AcceptTcpClient();
                Console.WriteLine("[GameTcpServer] Client connected.");

                // Create a new handler for that client
                ClientHandler clientHandler = new ClientHandler(clientSocket, this, _rootProvider);

                // Keep track of it
                lock (_clients)
                {
                    _clients.Add(clientHandler);
                }

                // Start a thread to process this client
                Thread clientThread = new Thread(clientHandler.Process);
                //clientThread.IsBackground = true; // Check if needed, maybe yes!
                clientThread.Start();
            }
        }

        /// <summary>
        /// Remove the client from both the main list and waiting queue (if present).
        /// </summary>
        public void RemoveClient(ClientHandler client)
        {
            lock (_waitingClients)
            {
                if (_waitingClients.Contains(client))
                {
                    // If the client is in the queue, remove it
                    // This simple approach might only remove the front, so be mindful

                    // If the client is waiting in the queue, remove it
                    // (Dequeue() won't remove that client 
                    //  unless it's at the front, so maybe do a different approach, but in the comment below is a reason why it's ok.)

                    _waitingClients.Dequeue(); //A situation where a client will be removed and is also waiting is if he is the only one waiting, so dequeuing will dequeue that client from the queue.
                }
            }

            lock (_clients)
            {
                _clients.Remove(client);
            }
        }

        /// <summary>
        /// Queue a client for matchmaking. If there's an existing waiting client,
        /// match them immediately. Otherwise, store them in the queue.
        /// </summary>
        public void AddToMatchmaking(ClientHandler client)
        {
            lock (_waitingClients)
            {
                Console.WriteLine($"[GameTcpServer] Current waiting count: {_waitingClients.Count}");
                if (_waitingClients.Count > 0)
                {
                    // Found an opponent in queue
                    ClientHandler opponent = _waitingClients.Dequeue();
                    CreateMatch(client, opponent);
                }
                else
                {
                    // Enqueue and let them wait
                    _waitingClients.Enqueue(client);
                    client.SendEncryptedMessage("{\"Type\":\"MatchWaiting\"}");
                }
                Console.WriteLine($"[GameTcpServer] Waiting count after: {_waitingClients.Count}");
            }
        }

        /// <summary>
        /// Match two clients in a 1v1 scenario. 
        /// Tells each about the other via "MatchFound" message.
        /// </summary>
        private void CreateMatch(ClientHandler client1, ClientHandler client2)
        {
            client1.SetOpponent(client2);
            client2.SetOpponent(client1);

            client1.matchWaveIndex = 0;
            client2.matchWaveIndex = 0;

            // Build JSON with "OpponentId" = client2.UserId for client1
            // and "OpponentId" = client1.UserId for client2
            int? userId1 = client1.UserId;
            int? userId2 = client2.UserId;

            string msg1 = $"{{\"Type\":\"MatchFound\",\"Data\":{{\"OpponentId\":{(userId2 ?? -1)}}}}}";
            string msg2 = $"{{\"Type\":\"MatchFound\",\"Data\":{{\"OpponentId\":{(userId1 ?? -1)}}}}}";

            // Each client must know the userId of his opponent, mostly for the database game session creation with UserId of both users
            client1.SendEncryptedMessage(msg1);
            client2.SendEncryptedMessage(msg2);

            Console.WriteLine($"[GameTcpServer] Match created: user1={userId1} vs user2={userId2}");
        }
    }
}
