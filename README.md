# TowerWardServer — TCP Game Server for *TowerWard*

**TowerWardServer** is the backend server for the Unity/C# tower‑defense game **TowerWard** (the client code is in the `TowerWard` repository). It manages multiplayer sessions, secure messaging, authentication, persistent data transfer and data storage via MySQL, and many other server-side functionalities.

## About the game
**TowerWard** is a **tower defense** game: defend yourself from enemy waves by placing and upgrading towers along the path.  
- **Single Player:** play on a map, deploy and upgrade various types of towers, and fight escalating waves of **enemies**. Win by destroying all waves.  
- **Multiplayer (2 players):** both players connect to the server and the server matches them for a game. Each plays on their **own map**, faces the natural waves, **and can send enemies to the opponent**. Players must manage money/resources and choose between attacking, reinforcing defenses, or using **special abilities**. Periodic **Snapshots** show the opponent’s state to help with decisions and strategy development. The goal is to be the last player standing.  
- **Extra game features (both modes):** special abilities, a **banking system**, and **mystery boxes** that provide additional depth and strategy to the game.  
- **Additional features:** settings management, a sound system and a tutorial for beginners.  

## What the server provides
- **Multiplayer functionality**: queue/matchmaking and a waiting room, starts matches in sync and relays periodic *snapshots* and *waves* between players, and more.
- **Secure networking**: RSA handshake to exchange an AES key, AES‑encrypted JSON messages, and JWT access/refresh tokens for authenticated requests (+auto-login).
- **Message protocol**: JSON messages format (e.g., `ServerPublicKey`, `AESKeyExchange`, `RegisterUser`, `RegisterSuccess`, plus many gameplay events).
- **Database & Structure**: MySQL + EF Core migrations, Models for users, sessions, global/user game stats, **and uses the repository/DTO/service layers**.
- **Scalable server loop and client management**: multithreaded TCP server with a separate `ClientHandler` per connection with safe access to shared state.

## Tech
- **.NET / C#**, **EF Core**, **MySQL**
- **TCP sockets** with AES/RSA encryption
- **JWT** authentication (access + refresh tokens) - also used for auto-login


## To make it clear
- The Unity client (in the `TowerWard` repository) connects to this server over TCP using a JSON-based protocol via sockets.
- View the `TowerWard` repository to understand more about the project.

## Notes
- Designed for learning/demo and easy extension.
