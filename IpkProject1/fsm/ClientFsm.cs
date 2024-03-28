#define DEBUG

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
    public static FsmStateEnum State => _state;
    
    private static bool _byeSent = false;
    
    private static object _lockObj = new();
    
    public static void SetState(FsmStateEnum stateEnum)
    {
        var prevState = _state;
        _state = stateEnum;
        
        Io.DebugPrintLine($"State changed to: {_state}");
        
        // Send bye packet if the state is changed to End and bye packet was not sent yet
        if (_state == FsmStateEnum.End)
        {
            lock (_lockObj)
            {
                if (_byeSent) return;
                IPacket? byePacket = SysArgParser.GetAppConfig().Protocol switch
                {
                    ProtocolEnum.Udp => UdpPacketBuilder.build_bye(),
                    ProtocolEnum.Tcp => TcpPacketBuilder.build_bye(),
                    _ => null
                };
                if (byePacket != null)
                {
                    Io.DebugPrintLine("LAST Bye packet sent...");
                    IpkProject1.GetClient().AddPacketToSendQueue(byePacket);
                    _byeSent = true;
                }
                IpkProject1.TerminationSem.Release();
                Io.DebugPrintLine("Termination semaphore released by END state...");
            }
        }
        // Release semaphore if the state is changed from Auth to Open (REPLY OK)
        // Release semaphore if the state is changed from Auth to Auth (REPLY NOK)
        else if (_state == FsmStateEnum.Open && prevState == FsmStateEnum.Auth
            || _state == FsmStateEnum.Auth && prevState == FsmStateEnum.Auth)
        {
            IpkProject1.AuthSem.Release();
        }
    }

    public static bool IsCommandAllowed(string command)
    {
        if (command == "auth")
        {
            if (State != FsmStateEnum.Auth && State != FsmStateEnum.Start)
            {
                Io.ErrorPrintLine("ERR: You are already authenticated, cannot authenticate again!");
                return false;
            }

            return true;
        }
        if (command == "join")
        {
            if (State != FsmStateEnum.Open)
            {
                Io.ErrorPrintLine("ERR: You are not authenticated, cannot join a channel!");
                return false;
            }
            return true;
        }
        if (command == "rename")
        {
            if (State != FsmStateEnum.Open)
            {
                Io.ErrorPrintLine("ERR: You are not authenticated, cannot rename!");
                return false;
            }
            return true;
        }
        if (command == "msg")
        {
            if (State != FsmStateEnum.Open)
            {
                Io.ErrorPrintLine("ERR: You are not authenticated, cannot send a message!");
                return false;
            }
            return true;
        }
        return false;
    }
}   