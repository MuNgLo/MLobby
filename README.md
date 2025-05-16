# MLobby v1.0

Dropin lobby manager to create host and join to a host. Using standard ip:port connections withouth any punchthrough or other third party reliance.

# Install
put all the files in this repo into addons/MLobby
Make sure you do a compile before trying to add the Lobby Nodes.

# How to use

In your project, create LobbyManager Node. Make sure it is a MultiplayerSpawner Node with it self as spawn path and the first entry in the Auto Spawn List is the LobbyMmember scene from the DropinScenes folder.

Also create a LobbyEvents Node. Point the LobbyManger Node to that in its inspector.

To start stop host or join/leave a host call the methods in the lobbymanager instance.
```cs
Starthost(27015); // Pass which network port the host should listen for connections on
StopHost();

JoinHost(IPAddress, port);
LeaveHost();
```
You can try to use the built in UPNP for a port using the LobbyManager.ProbeNetworkForInfo(port). The returning string of ip:port should be the internet side IP and a port forwarded to the gameinstance device if it works.
If that looks fine it should be possible to connect to that for others. But no garantees.


# How it works
When a connection is made there is a validationstep you should use to validate that the connecting gameisntance is valid and correct. After that the Lobby raises LobbyEvents.OnLobbyMemberValidated event. At that time the connecting gameinstance should be considered fully valid and gamelogic can listen to the evnet to add them as a new player or do whatever the project needs.

The validation step is there but not doing anything out the box.

# The point of it
This MLobby is to seperate the connection related logic from the actual gamelogic. To make sure connections that are lost or failing to not cause issues and be a good drop in to build from whenever you want to make a multiplayer project.