//using System;
//using System.Collections.Generic;
//using Newtonsoft.Json.Linq;
//using TcpServer; // So we can access ServerMessageHandler, ClientHandler, etc.

//// You can put this file anywhere in your server project or in a separate test project.
//// For a quick "manual" test harness, we use static methods you can call from Program.cs.

//public static class ServerWhiteBoxTests
//{
//    public static void RunAllTests()
//    {
//        Console.WriteLine("\n=== Starting Server White Box Tests ===");

//        TestMissingTypeField();
//        TestNoTokenDataWhenRequired();
//        TestSendBalloonForwarding();
//        TestUnknownMessageType();
//        TestLargeMessageHandling();

//        Console.WriteLine("=== All Server White Box Tests Completed ===\n");
//    }

//    /// <summary>
//    /// 1) If the incoming JSON has no "Type" property, the code calls:
//    ///    messageType = msgObj["Type"]?.ToString(); 
//    ///    => which becomes null => goes to default => logs "Unknown messageType".
//    ///    We check that the server does not crash and that no error message is sent to the client 
//    ///    (the default code just logs).
//    /// </summary>
//    private static void TestMissingTypeField()
//    {
//        Console.WriteLine("[TEST] TestMissingTypeField...");

//        var mockClient = new MockClientHandler();
//        var handler = new ServerMessageHandler(mockClient, null /* no DB needed */);

//        // JSON with no "Type"
//        string data = @"{ ""SomethingElse"": ""NoTypeHere"" }";

//        handler.HandleMessage(data);

//        // Expect: it logs "Unknown messageType =>" but doesn't crash or respond with an error 
//        // (the default case in the switch).
//        // We'll check that nothing got sent back to the client:
//        if (mockClient.SentMessages.Count != 0)
//        {
//            throw new Exception("Expected 0 messages sent to client when 'Type' is missing.");
//        }

//        Console.WriteLine(" -> PASS: Missing 'Type' caused no crash and no server response.");
//    }

//    /// <summary>
//    /// 2) For messages that are not "RegisterUser", "LoginUser", "AutoLogin", or "GameSnapshot",
//    ///    the server code expects a "TokenData" field with "AccessToken" / "RefreshToken".
//    ///    If it's missing, it sends an error: 
//    ///    {\"Type\":\"Error\",\"Data\":{\"Reason\":\"No TokenData in message.\"}}
//    /// </summary>
//    private static void TestNoTokenDataWhenRequired()
//    {
//        Console.WriteLine("[TEST] TestNoTokenDataWhenRequired...");

//        var mockClient = new MockClientHandler();
//        var handler = new ServerMessageHandler(mockClient, null);

//        // We'll send e.g. "MatchmakingRequest" but omit "TokenData"
//        string data = @"{ 
//            ""Type"": ""MatchmakingRequest"", 
//            ""Data"": {} 
//        }";

//        handler.HandleMessage(data);

//        // We expect the server to send an error message back
//        if (mockClient.SentMessages.Count == 0)
//        {
//            throw new Exception("Expected an error message, but none was sent.");
//        }

//        string response = mockClient.SentMessages[0];
//        // The code looks like: "{\"Type\":\"Error\",\"Data\":{\"Reason\":\"No TokenData in message.\"}}"
//        if (!response.Contains("\"Type\":\"Error\"")
//            || !response.Contains("No TokenData in message"))
//        {
//            throw new Exception("Did not see the expected 'No TokenData in message' error in server response.");
//        }

//        Console.WriteLine(" -> PASS: Missing TokenData triggered an error response as expected.");
//    }

//    /// <summary>
//    /// 3) For "SendBalloon", the server calls ForwardToOpponent(...), 
//    ///    which means it sends the exact data to the opponent's 'SendEncryptedMessage(...)'.
//    ///    We'll set an 'opponent' mock on the client to see if the data is forwarded.
//    /// </summary>
//    private static void TestSendBalloonForwarding()
//    {
//        Console.WriteLine("[TEST] TestSendBalloonForwarding...");

//        var mockClient = new MockClientHandler();
//        var mockOpponent = new MockClientHandler();
//        // Link them as opponents
//        mockClient.SetOpponent(mockOpponent);

//        // We also must pass "TokenData" because "SendBalloon" is not "RegisterUser"/"LoginUser".
//        string data = @"{
//            ""Type"": ""SendBalloon"",
//            ""TokenData"": { ""AccessToken"": ""abc"", ""RefreshToken"": ""def"" },
//            ""Data"": { ""BalloonHealth"": 10 }
//        }";

//        var handler = new ServerMessageHandler(mockClient, null);
//        handler.HandleMessage(data);

//        // The mockClient wouldn't send anything to itself, 
//        // but it should forward to the mockOpponent. So let's see what the opponent got:
//        if (mockOpponent.SentMessages.Count == 0)
//        {
//            throw new Exception("Expected the 'SendBalloon' message to be forwarded to the opponent, but got nothing.");
//        }

//        string forwarded = mockOpponent.SentMessages[0];
//        if (!forwarded.Contains("\"Type\":\"SendBalloon\""))
//        {
//            throw new Exception("Forwarded message does not contain 'SendBalloon' type. Possibly incorrect message.");
//        }

//        Console.WriteLine(" -> PASS: 'SendBalloon' was correctly forwarded to the opponent.");
//    }

//    /// <summary>
//    /// 4) If we have an unrecognized Type, the code goes to default and just logs 
//    ///    "[ServerMessageHandler] Unknown messageType => ...". 
//    ///    We'll confirm no crash and no server response is sent.
//    /// </summary>
//    private static void TestUnknownMessageType()
//    {
//        Console.WriteLine("[TEST] TestUnknownMessageType...");

//        var mockClient = new MockClientHandler();
//        var handler = new ServerMessageHandler(mockClient, null);

//        // Provide tokenData but an unknown type
//        string data = @"{
//            ""Type"": ""BlahBlahUnknown"",
//            ""TokenData"": { ""AccessToken"": ""xxx"", ""RefreshToken"": ""yyy"" },
//            ""Data"": {}
//        }";

//        handler.HandleMessage(data);

//        // The switch default logs a console line but does not respond to the client:
//        if (mockClient.SentMessages.Count != 0)
//        {
//            throw new Exception("Expected no message back for unknown Type.");
//        }

//        Console.WriteLine(" -> PASS: Unknown message type led to no server response and no crash.");
//    }

//    /// <summary>
//    /// 5) Large message test: see if the server can handle a Data field with many characters.
//    ///    This won't truly break your server unless you have a max length limit, 
//    ///    but let's confirm it doesn't throw an exception in parsing.
//    /// </summary>
//    private static void TestLargeMessageHandling()
//    {
//        Console.WriteLine("[TEST] TestLargeMessageHandling...");

//        var mockClient = new MockClientHandler();
//        var handler = new ServerMessageHandler(mockClient, null);

//        string bigString = new string('X', 5000); // e.g. 5000 chars
//        string data = @$"{{
//            ""Type"": ""MatchmakingRequest"",
//            ""TokenData"": {{ ""AccessToken"": ""token1"", ""RefreshToken"": ""token2"" }},
//            ""Data"": {{ ""BigStuff"": ""{bigString}"" }}
//        }}";

//        // This is a valid JSON but large. We expect no immediate error.
//        handler.HandleMessage(data);

//        // "MatchmakingRequest" should also attempt to call AddToMatchmaking(...) 
//        // which doesn't actually do much here because we have no real GameTcpServer.
//        // We do not test the queue logic in this mock scenario, just that it doesn't crash.

//        // Check if any error was sent:
//        if (mockClient.SentMessages.Count > 0)
//        {
//            // Possibly "No opponents" or "MatchWaiting"? 
//            // In real code, "MatchmakingRequest" triggers "client.Server.AddToMatchmaking(client);"
//            // but we have no real server. So we see no real effect unless we mock that too.
//            // For now, we can pass as long as it doesn't crash.
//        }

//        Console.WriteLine(" -> PASS: Large message was parsed successfully without crash.");
//    }
//}
