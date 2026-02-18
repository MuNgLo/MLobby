//#define MLog // Uncomment if MLogging is available in the project
#if MLog
using MLobby;
#endif
using Godot;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace MLobby;
/// <summary>
/// The LobbyManager is to usher new connections in and once they are ready
/// inform the game logic there is a new player ready to join the game.
/// </summary>
[GlobalClass]
public partial class LobbyManager : MultiplayerSpawner
{
    // Running is an active host, connected is is a client instance of the lobby
    [Export] private bool debug = true;
    [Export] private PackedScene prefabLobbyMember;

    IPAddress ipAddress;
    int port;
    int maxClients;

    private LOBBYSTATE state = LOBBYSTATE.OFFLINE;
    private List<LobbyMember> members;
    public List<LobbyMember> Members => members;
    private ENetMultiplayerPeer localPeer;
    private MultiplayerApi MP => Multiplayer;

    public override void _Ready()
    {
        SpawnFunction = new Callable(this, nameof(SpawnMember));
        // Initialize things
        MP.MultiplayerPeer = null;
        members = new();
        // Hook up events
        MP.ConnectedToServer += OnConnectedToServer;
        MP.ConnectionFailed += OnConnectionFailed;
        MP.PeerConnected += OnPeerConnected;
        MP.PeerDisconnected += OnPeerDisconnected;
        MP.ServerDisconnected += OnServerDisconnected;
        // Set ip to local 127.0.0.1 as default
        if (IPAddress.TryParse("127.0.0.1", out System.Net.IPAddress ip))
        {
            ipAddress = ip;
        }
    }
    /// <summary>
    /// Fires on Client as it connects to a host
    /// </summary>
    private void OnConnectedToServer()
    {
        if (debug)
        {
#if MLog
            MLog.LogInfo($"LobbyManager::OnConnectedToServer()"); 
#endif
        }
        LobbyEvents.RaiseConnectedToServer(ipAddress, port);
    }
    /// <summary>
    /// Fires when a clients is started but times out after ~34s
    /// Sets the MP Peer to NULL
    /// </summary>
    private void OnConnectionFailed()
    {
#if MLog
        if (debug) { MLog.LogError($"LobbyManager::OnConnectionFailed()"); }
#endif
        GetTree().GetMultiplayer().MultiplayerPeer = null; // Remove peer.
        LobbyEvents.RaiseOnConnectionFailed();
    }
    /// <summary>
    /// Fires on Client as server drops from network
    /// </summary>
    private void OnServerDisconnected()
    {
#if MLog
        if (debug) { MLog.LogInfo($"LobbyManager::OnServerDisconnected()"); }
#endif
        GetTree().GetMultiplayer().MultiplayerPeer.Close();
        MP.MultiplayerPeer = null;
        ClearAll();
        state = LOBBYSTATE.OFFLINE;
        LobbyEvents.RaiseServerDisconnected();
    }
    /// <summary>
    /// Fires on all as a peer disconnect. Yes host too but not on the peer that disconnects
    /// </summary>
    /// <param name="id"></param>
    private void OnPeerDisconnected(long id)
    {
#if MLog
        if (debug) { MLog.LogInfo($"LobbyManager::PeerDisconnected({id})"); }
#endif
        if (MP.IsServer())
        {
            LobbyMember pl = Members.Find(p => p.PeerID == id);
            Members.Remove(pl);
            LobbyEvents.RaiseMemberDisconnected(id);
            pl.QueueFree();
        }
    }
    /// <summary>
    /// Fires once on connecting client for the host and every existing client.
    /// Fires once on Host and existing Clients for the joining client.
    /// </summary>
    /// <param name="id"></param>
    private void OnPeerConnected(long id)
    {
#if MLog
        if (debug) { MLog.LogInfo($"LobbyManager::OnPeerConnected({id})"); }
#endif
        AddMember(id, LOBBYMEMBERSTATENUM.CONNECTING);
        if (MP.IsServer()) { TellPeerHostInfo(id); }
    }

    private void TellPeerHostInfo(long id)
    {
        RpcId(id, nameof(RPCReceiveHostInfo), "Host", ipAddress.ToString(), port, maxClients);
    }
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RPCReceiveHostInfo(string hostName, string address, int port, int maxPlayers)
    {
        LobbyEvents.RaiseHostInfoReceived(new HostInfo(hostName, address, port, maxPlayers));
    }

    /// <summary>
    /// If host, stop it!
    /// </summary>
    public void StopHost()
    {
        if (MP.IsServer())
        {
#if MLog
            MLog.LogInfo($"LobbyManager::StopHost()");
#endif
            MP.MultiplayerPeer.Close(); // OnServerDisconnected will fire on clients. NOT on host
            MP.MultiplayerPeer = null;
            ClearAll();
            LobbyEvents.RaiseHostClosed();
            state = LOBBYSTATE.OFFLINE;
        }
    }
    /// <summary>
    /// Asynchronously checks UPNP 
    /// Only fails if address is in use
    /// When succeeded, raises Events.NET.RaiseGameConnectedEvent (disabled now)
    /// </summary>
    public async void StartHost(int usePort, int clientsMax, string ip = "")
    {
        if (state != LOBBYSTATE.OFFLINE)
        {
#if MLog
            MLog.LogError($"LobbyManager::StartHost() Failed since state is not Offline[{state}]");
#endif
            return;
        }
        if (GetChildCount() > 0)
        {
#if MLog
            MLog.LogError($"LobbyManager::StartHost() Can't host, Manager Node has [{GetChildCount()}]Children left.");
#endif
            return;
        }
#if MLog
        if (debug) { MLog.LogInfo($"LobbyManager::StartHost() Launching host..."); }
#endif

        //Events.RaiseOnLoadStateChanged(new Events.LoadStateArguments() { text = "Starting a game as host...", loadProgressNormal = 0.0f });

        state = LOBBYSTATE.LAUNCHING;
        localPeer = new ENetMultiplayerPeer();
        port = usePort;

        StartHostMonitor(localPeer, 5000);

        maxClients = clientsMax;
        Error err = localPeer.CreateServer(port, maxClients);
        if (err != Error.Ok)
        {
            // Is another server running?
#if MLog
            MLog.LogError($"LobbyManager::StartHost() Can't host, address in use.");
#endif
            state = LOBBYSTATE.OFFLINE;
            localPeer = null;
            return;
        }
#if MLog
        if (debug) { MLog.LogInfo($"LobbyManager::StartHost() Running host with maxPlayers[{maxClients}]"); }
#endif
        GetTree().GetMultiplayer().MultiplayerPeer = localPeer;
        state = LOBBYSTATE.RUNNING;
        members = new();
        AddMember(1, LOBBYMEMBERSTATENUM.CONNECTING);

        if (ip == "")
        {
            Upnp upnp = new();
            await Task.Run(() =>
            {
                try { upnp.Discover(); }
                catch (Exception e)
                {
#if MLog
                    MLog.LogError(e.Message); 
#else
                    GD.PrintErr(e.Message);

#endif
                }
            });
            // No UPNP device found so setting all read
            if (upnp.GetDeviceCount() > 0)
            {
                // resolve external IP
                await Task.Run(() =>
                {
                    ip = upnp.QueryExternalAddress();
                });
            }
        }
        //Verify its a valid IP
        if (IPAddress.TryParse(ip, out IPAddress address))
        {
            ipAddress = address;
            LobbyEvents.RaiseHostSetupReady(address, port);
        }
        else
        {
            LobbyEvents.RaiseHostSetupReady(null, port);
        }

    }


    private async void StartHostMonitor(MultiplayerPeer peer, int ms)
    {
        await Task.Delay(ms);
        if (peer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Connected)
        {
            peer.Close();
            MP.MultiplayerPeer = null;
            //MLog.LogError($"LobbyManager::ConnectingMonitor() Connection was not established");
            state = LOBBYSTATE.OFFLINE;
            LobbyEvents.RaiseOnHostFailed();
        }
    }


    public void JoinHost(IPAddress ip, int usePort)
    {
        if (state != LOBBYSTATE.OFFLINE)
        {
#if MLog
            MLog.LogError($"LobbyManager::JoinHost({ip}:{port}) Failed since state is not Offline[{state}]");
#endif
            return;
        }
#if MLog
        if (debug) { MLog.LogInfo($"LobbyManager::JoinHost() Joining host"); }
#endif

        //Events.RaiseOnLoadStateChanged(new Events.LoadStateArguments() { text = "Establishing connection...", loadProgressNormal = 0.0f });
        state = LOBBYSTATE.LAUNCHING;


        localPeer = new ENetMultiplayerPeer();
        ipAddress = ip;
        port = usePort;



        ConnectingMonitor(localPeer, 5000);
        Error err = localPeer.CreateClient(ipAddress.ToString(), port);
        if (err != Error.Ok)
        {
#if MLog
            MLog.LogError($"LobbyManager::JoinHost({ip}:{port}) Client creation error :: {err}");
#endif
            state = LOBBYSTATE.OFFLINE;
            return;
        }
        GetTree().GetMultiplayer().MultiplayerPeer = localPeer;
#if MLog
        if (debug) { MLog.LogInfo($"LobbyManager::JoinHost({ip}:{port}) Connecting..."); }
#endif
        state = LOBBYSTATE.CONNECTED;
    }


    private async void ConnectingMonitor(MultiplayerPeer peer, int ms)
    {
        await Task.Delay(ms);
        if (peer.GetConnectionStatus() != MultiplayerPeer.ConnectionStatus.Connected)
        {
            peer.Close();
            MP.MultiplayerPeer = null;
            //MLog.LogError($"LobbyManager::ConnectingMonitor() Connection was not established");
            state = LOBBYSTATE.OFFLINE;
            LobbyEvents.RaiseOnConnectionFailed();
        }
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
            LobbyEvents.RaiseLeavingHost();
        }
    }
    /// <summary>
    /// When Host starts up or when a client connects, this will be used to add them as a member of the lobby
    /// It will instantiate and add a copy of the member scene under lobby node. Lobby node being a spawner and member scene being the FIRST entry in the spawner's spawnable scenes
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
#if MLog
                MLog.LogError($"LobbyManager::AddMember({id}) peerID already exists! Handle This!");
#endif
                return;
            }
            LobbyMember newMember = Spawn(new Godot.Collections.Dictionary<string, Variant>() { { "id", id }, { "pState", (int)pState } }) as LobbyMember;
            Members.Add(newMember);
            ValidateMember(id);
        }
    }
    private LobbyMember SpawnMember(Godot.Collections.Dictionary<string, Variant> args)
    {
        LobbyMember newMember = prefabLobbyMember.Instantiate() as LobbyMember;
        newMember.SetMemberInfo(args["id"].AsInt32(), (LOBBYMEMBERSTATENUM)args["pState"].AsInt32());
        newMember.Name = args["id"].AsInt32().ToString();
        return newMember;
    }
    /// <summary>
    /// Validate that given Peer is ready to join the game logic side of things
    /// What needs validation would depend on game. Is the client running same version would usually be good to check.
    /// Also informs client if they passed or not
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
        LobbyEvents.RaiseHostMemberValidated(id);
        RpcId(id, nameof(RPCValidationResult), true);
    }
    #region SingleFunction
    /// <summary>
    /// Tell peer if they passed validation or not
    /// </summary>
    /// <param name="result"></param>
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RPCValidationResult(bool result)
    {
        if (!result)
        {
            // FAILED! Do fail things
        }
        LobbyEvents.RaiseLocalClientValidated();
    }

    /// <summary>
    /// Make sure the Lobby is fully reset and any straggling member nodes is removed
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
        members = new();
    }


    private bool isProbing = false;
    public async Task<string> ProbeNetworkForInfo(int currentPort)
    {
        if (isProbing) { return string.Empty; }
        isProbing = true;
        GD.Print($"LobbyManager::ProbeNetworkForInfo() Trying to resolve network info through UPNP");
        string result = await TryUPNP(currentPort);
        GD.Print($"LobbyManager::ProbeNetworkForInfo() result -> {result}");
        isProbing = false;
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
            try { upnp.Discover(); } catch (Exception e) { GD.PrintErr(e.Message); }
        });
        // No UPNP device found so setting all read
        if (upnp.GetDeviceCount() < 1)
        {
            GD.Print($"LobbyManager::TryUPNP() No UPNP device found");
            return "";
        }
        string ip = "";
        await Task.Run(() =>
        {
            if ((Upnp.UpnpResult)upnp.AddPortMapping(currentPort) != Upnp.UpnpResult.Success)
            {
                GD.PrintErr($"LobbyManager::TryUPNP() Failed to map Port[{currentPort}] It might already be forwarded though");
            }
        }
        );
        // resolve external IP
        await Task.Run(() =>
        {
            ip = upnp.QueryExternalAddress();
        });
        return $"{ip}:{currentPort}";
    }
    #endregion
}// EOF CLASS
