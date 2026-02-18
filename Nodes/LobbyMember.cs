using System;
using Godot;
namespace MLobby;

[GlobalClass]
public partial class LobbyMember : MLobbyBaseNode
{
    [Export] private long peerID;
    [Export(PropertyHint.Enum)] private LOBBYMEMBERSTATENUM state = LOBBYMEMBERSTATENUM.NONE;
    internal LOBBYMEMBERSTATENUM State { get => state; set => SetState(value); }
    internal long PeerID { get => peerID; }

    public event EventHandler<LOBBYMEMBERSTATENUM> OnLobbyMemberStateChanged;

    internal void SetMemberInfo(long pID, LOBBYMEMBERSTATENUM pState)
    {
        peerID = pID;
        state = pState;
    }

    private void SetState(LOBBYMEMBERSTATENUM newState)
    {
        if (state != newState)
        {
            state = newState;
            OnLobbyMemberStateChanged?.Invoke(this, state);
        }
    }
}// EOF CLASS