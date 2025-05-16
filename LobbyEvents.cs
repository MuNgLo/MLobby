using System;
using Godot;

namespace MLobby;
/// <summary>
/// GDLobby related events
/// The RPC side is handled inside the Lobby so these can be raised on all or specific client from there
/// </summary>
[GlobalClass]
public partial class LobbyEvents : Node
{
    /// <summary>
    /// Fires when Host has been setup completely and the host member has been added to the member list.
    /// Local to host
    /// </summary>
    public event EventHandler OnHostSetupReady;
    /// <summary>
    /// When host closed down this will fire.
    /// Local to host
    /// </summary>
    public event EventHandler OnHostClosed;
    /// <summary>
    /// When a connecting client gone through all the validation needed, the lobby will fire this event
    /// carrying the peerID of the validated lobby member
    /// </summary>
    public event EventHandler<long> OnLobbyMemberValidated;
    /// <summary>
    /// Raised on all peers left on network when one peer leaves.
    /// Yes on host too.
    /// </summary>
    public event EventHandler<long> OnLobbyMemberDisconnected;
    /// <summary>
    /// Raised after a client disconnects and leaves a host
    /// </summary>
    public event EventHandler OnLeavingHost;
    /// <summary>
    /// Raised when host/server just up and leaves
    /// Most likely due to crash or network outage
    /// </summary>
    public event EventHandler OnServerDisconnected;
    /// <summary>
    /// Fires on Client as it connected to a host
    /// </summary>
    public event EventHandler OnConnectedToServer;


    /// <summary>
    /// Local to host
    /// </summary>
    public void RaiseHostSetupReady()
    {
        GD.Print($"LobbyEvents::RaiseHostSetupReady()");
        EventHandler raiseEvent = OnHostSetupReady;
        if (raiseEvent != null)
        {
            raiseEvent(this, null);
        }
    }

    /// <summary>
    /// Local to host
    /// </summary>
    public void RaiseHostMemberValidated(long id)
    {
        GD.Print($"LobbyEvents::RaiseHostMemberValidated() New Lobby member PeerID[{id}]");
        EventHandler<long> raiseEvent = OnLobbyMemberValidated;
        if (raiseEvent != null)
        {
            raiseEvent(this, id);
        }
    }
    /// <summary>
    /// When a peer drops from network the member Node is removed from host but this fires on all peers left on network
    /// Including host
    /// </summary>
    public void RaiseMemberDisconnected(long peerID)
    {
        EventHandler<long> raiseEvent = OnLobbyMemberDisconnected;
        if (raiseEvent != null)
        {
            raiseEvent(this, peerID);
        }
    }
    /// <summary>
    /// Local to host
    /// </summary>
    public void RaiseHostClosed()
    {
        EventHandler raiseEvent = OnHostClosed;
        if (raiseEvent != null)
        {
            raiseEvent(this, null);
        }
    }

    internal void RaiseLeavingHost()
    {
        EventHandler raiseEvent = OnLeavingHost;
        if (raiseEvent != null)
        {
            raiseEvent(this, null);
        }
    }

    /// <summary>
    /// When server drops all connected clients fire this
    /// </summary>
    internal void RaiseServerDisconnected()
    {
        EventHandler raiseEvent = OnServerDisconnected;
        if (raiseEvent != null)
        {
            raiseEvent(this, null);
        }
    }
    /// <summary>
    /// Fires on Client as it connects to a host
    /// </summary>
    internal void RaiseConnectedToServer()
    {
        EventHandler raiseEvent = OnConnectedToServer;
        if (raiseEvent != null)
        {
            raiseEvent(this, null);
        }
    }
}// EOF CLASS