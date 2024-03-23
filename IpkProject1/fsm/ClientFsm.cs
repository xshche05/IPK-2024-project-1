using IpkProject1.enums;
using IpkProject1.user;

namespace IpkProject1.fsm;

public static class ClientFsm
{
    private static FsmStateEnum _state = FsmStateEnum.Start;
    
    public static void SetState(FsmStateEnum stateEnum)
    {
        _state = stateEnum;
        if (_state == FsmStateEnum.End)
        {
            InputProcessor.CancellationTokenSource?.Cancel();
        }
    }
    
    public static FsmStateEnum GetState()
    {
        return _state;
    }
}   