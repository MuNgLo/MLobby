using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MLobby;

/// <summary>
/// This class will spawn the player prefab under itself, on the network when the lobby raises the validated member event<br/>
/// Player scene holds nodes for private and public data and events resulting in less need of rpc and in the end, when done right, results<br/>
/// in clearer, easier to follow code
/// </summary>
[GlobalClass]
public partial class PlayerManager : MultiplayerSpawner
{
    [Export] LobbyManager lobby;
    /// <summary>
    /// Player scene that gets instantiated and added under the player manager node
    /// </summary>
    [Export] PackedScene playerPrefab;
    /// <summary>
    /// The collection of currently active player objects
    /// </summary>
    protected List<MLobbyPlayer> players;

    /// <summary>
    /// All current players
    /// </summary>
    public List<MLobbyPlayer> All => players;



    public override void _EnterTree()
    {
        players = new();
        SpawnFunction = new Callable(this, nameof(SpawnPlayerDataObject));
    }

    public override void _Ready()
    {
        ChildEnteredTree += WhenChildEnterTree;
        LobbyEvents.OnHostClosed += Cleanup;
        LobbyEvents.OnServerDisconnected += Cleanup;
        LobbyEvents.OnLeavingHost += Cleanup;
        LobbyEvents.OnLobbyMemberValidated += WhenLobbyMemberValidate;
    }

    private protected virtual void WhenChildEnterTree(Node node)
    {
        if (!Multiplayer.IsServer())
        {
            // Process node for client side player list
            if (node is MLobbyPlayer pl)
            {
                if (!players.Exists(p => p.PeerID == pl.PeerID))
                {
                    players.Add(pl);
                }
            }
        }
    }

    public virtual void Cleanup(object sender, EventArgs e)
    {
        GD.Print($"PlayerManager::Cleanup()");
    }

    public virtual bool GetPlayer(int peerID, out MLobbyPlayer player)
    {
        return GetPlayer((long)peerID, out player);
    }
    public virtual bool GetPlayer(long peerID, out MLobbyPlayer player)
    {
        player = null;
        if (players.Exists(p => p.PeerID == peerID))
        {
            player = players.Find(p => p.PeerID == peerID);
        }
        return player is not null;
    }

    #region Data Object Spawning
    public virtual void WhenLobbyMemberValidate(object sender, long peerID)
    {
        Godot.Collections.Dictionary<string, Variant> SpawnPlayerDataObjectArgs = new Godot.Collections.Dictionary<string, Variant>()
        {
            {"peerID", peerID },
            {"playerName", $"Player-{players.Count + 1}" }
        };
        players.Add(Spawn(SpawnPlayerDataObjectArgs) as MLobbyPlayer);
        Rpc(nameof(RPCRaisePlayersChanged));
    }
    public virtual MLobbyPlayer SpawnPlayerDataObject(Godot.Collections.Dictionary<string, Variant> args)
    {
        MLobbyPlayer player = playerPrefab.Instantiate() as MLobbyPlayer;
        player.SetPeerID(args["peerID"].AsInt32());
        player.PlayerName = args["playerName"].AsString();
        return player;
    }
    #endregion


    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RPCRaisePlayersChanged()
    {
        MLobbyPlayerEvents.RaiseOnPlayersChanged();
    }
 

    internal void SetNameOnPlayer(long peerID, string newName)
    {
        if (GetPlayer(peerID, out MLobbyPlayer player))
        {
            player.PlayerName = newName;
        }
    }
}// EOF CLASS
