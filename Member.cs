using Godot;
namespace MLobby;

[GlobalClass]
internal partial class Member : Node
{
    [Export] private long peerID;
    [Export(PropertyHint.Enum)] private LOBBYMEMBERSTATENUM state = LOBBYMEMBERSTATENUM.NONE;
    internal LOBBYMEMBERSTATENUM State { get => state; }
    internal long PeerID { get => peerID; }

    internal void SetMemberInfo(long pID, LOBBYMEMBERSTATENUM pState)
    {
        peerID = pID;
        state = pState;
    }

    internal void SetState(LOBBYMEMBERSTATENUM newState)
    {
        if (state != newState)
        {
            state = newState;
        }
    }
}// EOF CLASS