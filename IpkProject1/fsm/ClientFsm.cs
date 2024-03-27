using System.Diagnostics;
using System.Net;
using IpkProject1.enums;
using IpkProject1.interfaces;
using IpkProject1.sysarg;
using IpkProject1.tcp;
using IpkProject1.udp;
using IpkProject1.user;

namespace IpkProject1.fsm;

public static class ClientFsm
{
    private static FsmStateEnum _state = FsmStateEnum.Start;
    
    public static void SetState(FsmStateEnum stateEnum)
    {
        var prevState = _state;
        _state = stateEnum;
        Debug.WriteLine($"State changed to: {_state}");
        if (_state == FsmStateEnum.End && !InputProcessor.CancellationToken.IsCancellationRequested)
        {
            InputProcessor.CancellationTokenSource?.Cancel();
            if (!IpkProject1.TimeoutCancellationTokenSource.IsCancellationRequested)
            {
                IPacket? byePacket = SysArgParser.GetAppConfig().Protocol switch
                {
                    ProtocolEnum.Udp => UdpPacketBuilder.build_bye(),
                    ProtocolEnum.Tcp => TcpPacketBuilder.build_bye(),
                    _ => null
                };
                if (byePacket != null)
                {
                    Debug.WriteLine("Bye packet sent...");
                    IpkProject1.GetClient().AddPacketToSendQueue(byePacket);
                }
            }
        }

        if (_state == FsmStateEnum.Open && prevState == FsmStateEnum.Auth)
        {
            IpkProject1.AuthSem.Release();
        }
        if (_state == FsmStateEnum.Auth && prevState == FsmStateEnum.Auth)
        {
            IpkProject1.AuthSem.Release();
        }
    }
    
    public static FsmStateEnum GetState()
    {
        return _state;
    }
}   