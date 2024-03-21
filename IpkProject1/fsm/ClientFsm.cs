using IpkProject1.Messages;

namespace IpkProject1.fsm;

public class ClientFsm
{
    private static FsmState _state = FsmState.Start;
    
    private static MessageType _lastClientMessage = MessageType.None;
    private static MessageType _lastServerMessage = MessageType.None;
    
    public static void SetState(FsmState state)
    {
        _state = state;
    }
    
    public static bool IsUserCommandAllowed(MessageType message)
    {
        // TODO: implement state machine
        return true;
    }
}   