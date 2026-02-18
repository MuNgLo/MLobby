using System;
using System.Net;
using Godot;

namespace MLobby;
/// <summary>
/// Lobby related events
/// The RPC side is handled inside the Lobby so these can be raised on all or specific client from there
/// </summary>
public static class LobbyEvents
{
    /// <summary>
    /// Fires when Host has been setup completely and the host member has been added to the member list.
    /// Local to host
    /// </summary>
    public static  event EventHandler<ConnectedEventArguments> OnHostSetupReady;
    /// <summary>
    /// When host closed down this will fire.
    /// Local to host
    /// </summary>
    public static event EventHandler OnHostClosed;
    /// <summary>
    /// When a connecting client gone through all the validation needed, the lobby will fire this event
    /// carrying the peerID of the validated lobby member
    /// </summary>
    public static event EventHandler<long> OnLobbyMemberValidated;
    /// <summary>
    /// Raised on all peers left on network when one peer leaves.
    /// Yes on host too.
    /// </summary>
    public static event EventHandler<long> OnLobbyMemberDisconnected;
    /// <summary>
    /// Raised after a client disconnects and leaves a host
    /// </summary>
    public static event EventHandler OnLeavingHost;
    /// <summary>
    /// Raised when host/server just up and leaves
    /// Most likely due to crash or network outage
    /// </summary>
    public static event EventHandler OnServerDisconnected;
    /// <summary>
    /// Fires on Client as it connected to a host
    /// </summary>
    public static event EventHandler<ConnectedEventArguments> OnConnectedToServer;
    /// <summary>
    /// Fires locally on a client after they connected to a host and they receive the host information.
    /// </summary>
    public static event EventHandler<HostInfo> OnHostInfoReceived;
    /// <summary>
    /// Fires on local client carrying the result of the validation
    /// </summary>
    public static event EventHandler OnLocalClientValidated;

    public static event EventHandler OnConnectionFailed;
    public static event EventHandler OnHostFailed;


    /// <summary>
    /// Local to host
    /// </summary>
    public static void RaiseHostSetupReady(IPAddress ip, int port, bool debug=false)
    {
        if (debug) { GD.Print($"LobbyEvents::RaiseHostSetupReady()"); }
        EventHandler<ConnectedEventArguments> raiseEvent = OnHostSetupReady;
        if (raiseEvent != null)
        {
            raiseEvent(null, new(){ip = ip, port = port});
        }
    }

    /// <summary>
    /// Local to clients
    /// Fires when they connected and the host send the host info to them
    /// </summary>
    public static void RaiseHostInfoReceived(HostInfo hostInfo, bool debug=false)
    {
        if (debug) { GD.Print($"LobbyEvents::RaiseHostInfoRecieved()"); }
        EventHandler<HostInfo> raiseEvent = OnHostInfoReceived;
        if (raiseEvent != null)
        {
            raiseEvent(null, hostInfo);
        }
    }

    /// <summary>
    /// Local to host
    /// As a peer has been ushered in on network and validated this fires as a last step allowing gamelogic to 
    /// step in and react.
    /// </summary>
    public static void RaiseHostMemberValidated(long peerID, bool debug=false)
    {
        if (debug) { GD.Print($"LobbyEvents::RaiseHostMemberValidated() New Lobby member PeerID[{peerID}]"); }
        EventHandler<long> raiseEvent = OnLobbyMemberValidated;
        if (raiseEvent != null)
        {
            raiseEvent(null, peerID);
        }
    }
    /// <summary>
    /// When a peer drops from network the member Node is removed from host but this fires on all peers left on network
    /// Including host
    /// </summary>
    public static void RaiseMemberDisconnected(long peerID, bool debug=false)
    {
        if (debug) { GD.Print($"LobbyEvents::RaiseMemberDisconnected({peerID})"); }
        EventHandler<long> raiseEvent = OnLobbyMemberDisconnected;
        if (raiseEvent != null)
        {
            raiseEvent(null, peerID);
        }
    }
    /// <summary>
    /// Local to host
    /// </summary>
    public static void RaiseHostClosed(bool debug=false)
    {
        if (debug) { GD.Print($"LobbyEvents::RaiseHostClosed()"); }
        EventHandler raiseEvent = OnHostClosed;
        if (raiseEvent != null)
        {
            raiseEvent(null, null);
        }
    }

    internal static void RaiseLeavingHost(bool debug=false)
    {
        if (debug) { GD.Print($"LobbyEvents::RaiseLeavingHost()"); }
        EventHandler raiseEvent = OnLeavingHost;
        if (raiseEvent != null)
        {
            raiseEvent(null, null);
        }
    }

    /// <summary>
    /// When server drops all connected clients fire this
    /// </summary>
    internal static void RaiseServerDisconnected(bool debug=false)
    {
        if (debug) { GD.Print($"LobbyEvents::RaiseServerDisconnected()"); }
        EventHandler raiseEvent = OnServerDisconnected;
        if (raiseEvent != null)
        {
            raiseEvent(null, null);
        }
    }
    /// <summary>
    /// Fires on Client as it connects to a host
    /// </summary>
    internal static void RaiseConnectedToServer(IPAddress ip, int port, bool debug=false)
    {
        if (debug) { GD.Print($"LobbyEvents::RaiseConnectedToServer()"); }
        EventHandler<ConnectedEventArguments> raiseEvent = OnConnectedToServer;
        if (raiseEvent != null)
        {
            raiseEvent(null, new(){ ip = ip, port = port });
        }
    }
    /// <summary>
    /// Raised on client when they get the validation result from host they connected to
    /// </summary>
    internal static void RaiseLocalClientValidated(bool debug=false)
    {
        if (debug) { GD.Print($"LobbyEvents::RaiseLocalClientValidated()"); }
        EventHandler raiseEvent = OnLocalClientValidated;
        if (raiseEvent != null)
        {
            raiseEvent(null, null);
        }
    }

    internal static void RaiseOnConnectionFailed(bool debug=false)
    {
        if (debug) { GD.Print($"LobbyEvents::RaiseOnConnectionFailed()"); }
        EventHandler raiseEvent = OnConnectionFailed;
        if (raiseEvent != null)
        {
            raiseEvent(null, null);
        }
    }
    internal static void RaiseOnHostFailed(bool debug=false)
    {
        if (debug) { GD.Print($"LobbyEvents::RaiseOnHostFailed()"); }
        EventHandler raiseEvent = OnHostFailed;
        if (raiseEvent != null)
        {
            raiseEvent(null, null);
        }
    }
}// EOF CLASS