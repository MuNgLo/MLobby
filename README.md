# MLobby v1.2

Drop in lobby manager to create host and join to a host. Using standard ip:port connections without any punch through or other third party reliance.
Uses 2 MultiPlayerSpawners to spawn in nodes representing the connection (lobbyMember) and player (MLobbyPlayer).
The player object gets spawned in when the connection is validated by the LobbyManager.

Player object hold private and public data properties that are synced with synchronizers and result in corresponding events to be raised in the
MLobbyPlayerEvents static class.

# Install
put all the files in this repo into addons/MLobby
Make sure you do a compile before trying to add the Lobby Nodes.

# How to use

In your project, create LobbyManager Node. Make sure it is a MultiplayerSpawner Node with it self as spawn path and the first entry in the Auto Spawn List is the LobbyMember scene from the Drop in Scenes folder.

Also create a LobbyEvents Node. Point the LobbyManger Node to that in its inspector.

To start stop host or join/leave a host call the methods in the LobbyManager instance.
```cs
StartHost(27015, 2, string ip = "")// Pass which network port, max clients, IP is the internet side IP and to make connection possible the port needs to be forwarded
StopHost();

JoinHost(IPAddress, port);
LeaveHost();
```
You can try to use the built in UPNP for a port using the LobbyManager.ProbeNetworkForInfo(port). The returning string of ip:port should be the internet side IP and a port forwarded to the game instance device if it works.
If that looks fine it should be possible to connect to that for others. But no guarantees.


# How it works
When a connection is made there is a validation step you should use to validate that the connecting game instance is valid and correct. After that the Lobby raises LobbyEvents.OnLobbyMemberValidated event. At that time the connecting game instance should be considered fully valid and game logic can listen to the event to add them as a new player or do whatever the project needs.

The validation step is there but not doing anything out the box.

# The point of it
This MLobby is to separate the connection related logic from the actual game logic. To make sure connections that are lost or failing to not cause issues and be a good drop in to build from whenever you want to make a multiplayer project.
Also minimizing the amount of traffic needed when a client connects is a good thing. This aims to only deal with connection relevant data for the connection.