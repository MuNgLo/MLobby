using Godot;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace MLobby;
/// <summary>
/// The LobbyManager is to usher new connections in and once they are ready
/// inform the gamelogic there is a new player ready to join the game.
/// </summary>
[GlobalClass]
internal partial class LobbyManager : MultiplayerSpawner
{
    // Running is an active host, connected is is a client instance of the lobby
    [Export] private bool debug = true;
    [Export] private LobbyEvents Events;
    [Export] private int maxPlayers = 10;

    private LOBBYSTATE state = LOBBYSTATE.OFFLINE;
    private List<LobbyMember> Members;
    private ENetMultiplayerPeer localPeer;
    private MultiplayerApi MP => Multiplayer;

    public override void _Ready()
    {
        // Initialize things
        MP.MultiplayerPeer = null;
        Members = new();
        // Hook up events
        MP.ConnectedToServer += OnConnectedToServer;
        MP.ConnectionFailed += OnConnectionFailed;
        MP.PeerConnected += OnPeerConnected;
        MP.PeerDisconnected += OnPeerDisconnected;
        MP.ServerDisconnected += OnServerDisconnected;
    }
    /// <summary>
    /// Fires on Client as it connects to a host
    /// </summary>
    private void OnConnectedToServer()
    {
        if (debug) { GD.Print($"LobbyManager::OnConnectedToServer()"); }
        Events.RaiseConnectedToServer();
    }
    /// <summary>
    /// Fires when a clients is started but timesout after ~34s
    /// Sets the MP Peer to NULL
    /// </summary>
    private void OnConnectionFailed()
    {
        if (debug) { GD.Print($"LobbyManager::OnConnectionFailed()"); }
        GetTree().GetMultiplayer().MultiplayerPeer = null; // Remove peer.
    }
    /// <summary>
    /// Fires on Client as server drops from network
    /// </summary>
    private void OnServerDisconnected()
    {
        if (debug) { GD.Print($"LobbyManager::OnServerDisconnected()"); }
        GetTree().GetMultiplayer().MultiplayerPeer.Close();
        MP.MultiplayerPeer = null;
        ClearAll();
        state = LOBBYSTATE.OFFLINE;
        Events.RaiseServerDisconnected();
    }
    /// <summary>
    /// Fires on all as a peer disconnect. Yes host too but not on the peer that disconnects
    /// </summary>
    /// <param name="id"></param>
    private void OnPeerDisconnected(long id)
    {
        if (debug) { GD.Print($"LobbyManager::PeerDisconnected({id})"); }
        if (MP.IsServer())
        {
            LobbyMember pl = Members.Find(p => p.PeerID == id);
            Members.Remove(pl);
            Events.RaiseMemberDisconnected(id);
            pl.QueueFree();
        }
    }
    /// <summary>
    /// Fires once on connecting client for the host and every existing client.
    /// Fires once on Host and existing Clients for the joining client.
    /// </summary>
    /// <param name="id"></param>
    private void OnPeerConnected(long id) // Seems connecting client has id as 1 and host has the random peer ID
    {
        if (debug) { GD.Print($"LobbyManager::OnPeerConnected({id})"); }
        AddMember(id, LOBBYMEMBERSTATENUM.CONNECTING);
    }
    /// <summary>
    /// If host, stop it!
    /// </summary>
    public void StopHost()
    {
        if (MP.IsServer())
        {
            GD.Print($"LobbyManager::StopHost()");
            MP.MultiplayerPeer.Close(); // OnServerDisconnected will fire on clients. NOT on host
            MP.MultiplayerPeer = null;
            ClearAll();
            Events.RaiseHostClosed();
            state = LOBBYSTATE.OFFLINE;
        }
    }
    /// <summary>
    /// Asynchrounously checks UPNP 
    /// Only fails if adress is in use
    /// When succeded, raises Events.NET.RaiseGameConnectedEvent (disabled now)
    /// </summary>
    public void StartHost(int currentPort)
    {
        if (state != LOBBYSTATE.OFFLINE) { return; }
        if (GetChildCount() > 0)
        {
            GD.Print($"LobbyManager::StartHost() Can't host, Manager Node has [{GetChildCount()}]Children left.");
            return;
        }
        if (debug) { GD.Print($"LobbyManager::StartHost() Launching host..."); }
        state = LOBBYSTATE.LAUNCHING;
        localPeer = new ENetMultiplayerPeer();
        Error err = localPeer.CreateServer(currentPort, maxPlayers);
        if (err != Error.Ok)
        {
            // Is another server running?
            GD.Print($"LobbyManager::StartHost() Can't host, address in use.");
            state = LOBBYSTATE.OFFLINE;
            localPeer = null;
            return;
        }
        GD.Print($"LobbyManager::StartHost() Running host with maxPlayers[{maxPlayers}]");
        GetTree().GetMultiplayer().MultiplayerPeer = localPeer;
        state = LOBBYSTATE.RUNNING;
        Members = new();
        AddMember(1, LOBBYMEMBERSTATENUM.CONNECTING);
        Events.RaiseHostSetupReady();
    }
    public void JoinHost(IPAddress ip, int port)
    {
        if (state != LOBBYSTATE.OFFLINE) { return; }
        state = LOBBYSTATE.LAUNCHING;

        localPeer = new ENetMultiplayerPeer();
        Error err = localPeer.CreateClient(ip.ToString(), port);
        if (err != Error.Ok)
        {
            GD.Print($"LobbyManager::JoinHost({ip}:{port}) Client creation error :: {err}", true);
            state = LOBBYSTATE.OFFLINE;
            return;
        }
        GetTree().GetMultiplayer().MultiplayerPeer = localPeer;
        if (debug) { GD.Print($"LobbyManager::JoinHost({ip}:{port}) Connecting..."); }
        state = LOBBYSTATE.CONNECTED;
    }
    /// <summary>
    /// Call this on client to leave the host
    /// Avoid calling this on a host
    /// </summary>
    internal void LeaveHost()
    {
        if (MP.HasMultiplayerPeer())
        {
            MP.MultiplayerPeer.Close();
            MP.MultiplayerPeer = null;
            state = LOBBYSTATE.OFFLINE;
            Events.RaiseLeavingHost();
        }
    }
    /// <summary>
    /// When Host starts up or when a client connects, this will be used to add them as a member of the lobby
    /// It will instantiate and add a copy of the member scene under lobby node. Lobby node being a spawner and memeber scene being the FIRST entry in the spawner's spawnable scenes
    /// will cause the member nodes to be replicated across network.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pState"></param>
    private void AddMember(long id, LOBBYMEMBERSTATENUM pState)
    {
        if (MP.IsServer())
        {
            if (Members.Exists(p => p.PeerID == id))
            {
                GD.PrintErr($"LobbyManager::AddMember({id}) peerID already exists! Handle This!");
                return;
            }
            string path = GetSpawnableScene(0);
            LobbyMember newMember = GD.Load<PackedScene>(path).Instantiate() as LobbyMember;
            newMember.SetMemberInfo(id, pState);
            Members.Add(newMember);
            newMember.Name = id.ToString();
            AddChild(newMember); // Remember this should be host authority synced over network with a networkspawner
            ValidateMember(id);
        }
    }
    /// <summary>
    /// Validate that given Peer is ready to join the gamelogic side of things
    /// What needs validation would depend on game. Is the client running same version would usally be good to check.
    /// </summary>
    /// <param name="id"></param>
    /// <exception cref="NotImplementedException"></exception>
    private async void ValidateMember(long id)
    {
        await Task.Delay(500);
        if (Members.Exists(p => p.PeerID == id))
        {
            Members.Find(p => p.PeerID == id).State = LOBBYMEMBERSTATENUM.CONNECTED;
        }
        Events.RaiseHostMemberValidated(id);
    }
    #region SingleFunction
    /// <summary>
    /// Make sure the Lobby is fully reset and any straggling memeber nodes is removed
    /// </summary>
    public void ClearAll()
    {
        if (Members is not null)
        {
            foreach (LobbyMember member in Members)
            {
                if (IsInstanceValid(member))
                {
                    member.QueueFree();
                }
            }
        }
        Members = new();
    }

    public async Task<string> ProbeNetworkForInfo(int currentPort){
        GD.Print($"LobbyManager::ProbeNetworkForInfo() Trying to resolve network info through UPNP");
        string result = await TryUPNP(currentPort);
        GD.Print($"LobbyManager::ProbeNetworkForInfo() result -> {result}");
        return result;
    }

    /// <summary>
    /// Tries to resolve the network with auto port forwarding and learn what outside IP the host will end up on.
    /// 
    /// </summary>
    /// <returns>ip:port</returns>
    private async Task<string> TryUPNP(int currentPort)
    {
        Upnp upnp = new();
        await Task.Run(() =>
        {
            try { upnp.Discover(); } catch (Exception e) { GD.Print(e.Message); }
        });
        // No UPNP device found so setting all read
        if (upnp.GetDeviceCount() < 1)
        {
            GD.Print($"LobbyManager::TryUPNP() No UPNP device found");
            return "";
        }

        await Task.Run(() =>
        {
            if ((Upnp.UpnpResult)upnp.AddPortMapping(currentPort) != Upnp.UpnpResult.Success)
            {
                GD.Print($"LobbyManager::TryUPNP() Failed to map Port[{currentPort}] It might already be forwarded though");
            }
        }
        );
        string ipString = "127.0.0.1";
        // resolve external IP
        await Task.Run(() =>
        {
            ipString = upnp.QueryExternalAddress();
        });
        return $"{ipString}:{currentPort}";
    }
    #endregion
}// EOF CLASS
