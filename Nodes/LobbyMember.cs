using Godot;
namespace MLobby;

[GlobalClass]
public partial class LobbyMember : Node
{
    [Export] private long peerID;
    [Export(PropertyHint.Enum)] private LOBBYMEMBERSTATENUM state = LOBBYMEMBERSTATENUM.NONE;
    internal LOBBYMEMBERSTATENUM State { get => state; set => SetState(value); }
    internal long PeerID { get => peerID; }

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
        }
    }
}// EOF CLASS