//using System;
//using System.Collections.Generic;
//using TcpServer;

//public class MockClientHandler : IClientHandler
//{
//    // We'll track what messages the server sends to this "client"
//    public List<string> SentMessages { get; private set; } = new List<string>();

//    // We can store an "opponent" if needed
//    private MockClientHandler _opponent;

//    // If the server code sets a userId, we store it here
//    public int? UserId { get; private set; } = null;

//    public void SendEncryptedMessage(string plainJson)
//    {
//        // Instead of real encryption or network, we just store it
//        SentMessages.Add(plainJson);
//    }

//    public void SetOpponent(MockClientHandler opponent)
//    {
//        _opponent = opponent;
//    }

//    public MockClientHandler GetOpponent()
//    {
//        return _opponent;
//    }

//    public void SetUserId(int userId)
//    {
//        UserId = userId;
//    }

//    // If you want to simulate a "Server" property or method, you can add it or stub it out.
//    // If your code calls _client.Server.AddToMatchmaking(_client), we need to provide something:
//    public MockGameTcpServer Server { get; set; } = new MockGameTcpServer();

//    // Optionally implement a .ToString() for debug
//    public override string ToString()
//    {
//        return $"MockClientHandler(UserId={UserId})";
//    }
//}

///// <summary>
///// Minimal 'IClientHandler' interface so we can avoid inheriting the real ClientHandler (which needs a real NetworkStream).
///// 
///// If you prefer, you can do "public class MockClientHandler : ClientHandler { ... }" 
///// but that might require a real or fake NetworkStream. 
///// This interface approach is simpler for testing the message logic only.
///// </summary>
//public interface IClientHandler
//{
//    void SendEncryptedMessage(string plainJson);
//    void SetUserId(int userId);
//}

///// <summary>
///// A mock or dummy server if your code calls "this.Server.AddToMatchmaking(...)"
///// We store no real queue, just prevent errors.
///// </summary>
//public class MockGameTcpServer
//{
//    public void AddToMatchmaking(MockClientHandler client)
//    {
//        // For example, do nothing or set a flag "wasCalled = true"
//    }
//}
