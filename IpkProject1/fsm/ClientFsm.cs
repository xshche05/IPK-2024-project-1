using IpkProject1.enums;

namespace IpkProject1.fsm;

public static class ClientFsm
{
    private static FsmStateEnum _state = FsmStateEnum.Start;
    
    public static void SetState(FsmStateEnum stateEnum)
    {
        _state = stateEnum;
    }
    
    public static FsmStateEnum GetState()
    {
        return _state;
    }
}   