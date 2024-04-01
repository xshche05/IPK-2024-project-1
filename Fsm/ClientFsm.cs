using IpkProject1.Enums;
using IpkProject1.Interfaces;
using IpkProject1.SysArg;
using IpkProject1.Tcp;
using IpkProject1.Udp;
using IpkProject1.User;

namespace IpkProject1.Fsm;


public static class ClientFsm
{
    // Current state of the client's FSM, initially set to Start
    private static FsmStateEnum _state = FsmStateEnum.Start;
    // Property to access the current state of the client's FSM
    public static FsmStateEnum State => _state;
    // Flag to determine if the bye packet was already sent
    public static bool NeedByeSendFlag { get; set; } = true;

    // Lock object for the End state
    private static readonly object FsmLockObj = new();
    
    // Method to set the state of the client's FSM
    public static void SetState(FsmStateEnum stateEnum)
    {
        lock (FsmLockObj) // Set state can be called from multiple tasks
        {
            var prevState = _state;
            _state = stateEnum;
        
            Io.DebugPrintLine($"Previous state: {prevState}");
            Io.DebugPrintLine($"State changed to: {_state}");
        
            // Send bye packet if the state is changed to End and bye packet was not sent yet
            if (_state == FsmStateEnum.End && prevState != FsmStateEnum.End)
            {
                IPacket? byePacket = SysArgParser.Config.Protocol switch
                {
                    ProtocolEnum.Udp => new UdpPacketBuilder().build_bye(),
                    ProtocolEnum.Tcp => new TcpPacketBuilder().build_bye(),
                    _ => null
                };
                if (byePacket != null && NeedByeSendFlag)
                {
                    Io.DebugPrintLine("LAST Bye packet sent...");
                    Program.GetClient().AddPacketToSendQueue(byePacket);
                    NeedByeSendFlag = false;
                }

                Program.TerminationSem.Release();
                Io.DebugPrintLine("Termination semaphore released by END state...");
            }
            else if (_state == FsmStateEnum.Open && prevState == FsmStateEnum.Auth
                     || _state == FsmStateEnum.Auth && prevState == FsmStateEnum.Auth)
            {
                // Release semaphore if the state is changed from Auth to Open (REPLY OK) or Auth to Auth (REPLY NOK)
                Io.DebugPrintLine("Auth semaphore released...");
                Program.AuthSem.Release();
            }
            else if (_state == FsmStateEnum.Error)
            {
                Program.ExitCode = 1;
                SetState(FsmStateEnum.End);
            }
        }
    }
    
    // Method to check if a command (message) is allowed in the current state of the client's FSM
    public static bool IsCommandAllowed(string command)
    {
        bool flag = false;
        switch (command)
        {
            case "auth": // Check if the command is allowed in the current state
                flag = State == FsmStateEnum.Auth || State == FsmStateEnum.Start;
                if (!flag) Io.ErrorPrintLine("ERR: You was already authenticated!", ColorScheme.Warning);
                return flag;
            case "join":
                flag = State == FsmStateEnum.Open; // Check if the command is allowed in the current state
                if (!flag) Io.ErrorPrintLine("ERR: You must be authenticated to join a chat room!", ColorScheme.Warning);
                return flag;
            case "rename":
                flag = State == FsmStateEnum.Open; // Check if the command is allowed in the current state
                if (!flag) Io.ErrorPrintLine("ERR: You must be authenticated to rename yourself!", ColorScheme.Warning);
                return flag;
            case "msg":
                flag = State == FsmStateEnum.Open; // Check if the command is allowed in the current state
                if (!flag) Io.ErrorPrintLine("ERR: You must be authenticated to send a message!", ColorScheme.Warning);
                return flag;
            case "currentname":
                flag = State == FsmStateEnum.Open; // Check if the command is allowed in the current state
                if (!flag) Io.ErrorPrintLine("ERR: You must be authenticated to check your current name!", ColorScheme.Warning);
                return flag;
            default:
                return flag;
        }
    }
    
    // Method to update the client's FSM according to the received packet
    public static void FsmUpdate(IPacket p)
    {
        switch (State)
        {
            case FsmStateEnum.Auth:
                if (p.Type == MessageTypeEnum.Err)
                {
                    SetState(FsmStateEnum.End);
                }
                else if (p.Type == MessageTypeEnum.Reply && p.ReplyState() == true)
                {
                    SetState(FsmStateEnum.Open);
                }
                else if (p.Type == MessageTypeEnum.Reply && p.ReplyState() == false)
                {
                    SetState(FsmStateEnum.Auth);
                }
                break;
            case FsmStateEnum.Open:
                if (p.Type == MessageTypeEnum.Err)
                {
                    SetState(FsmStateEnum.End);
                }
                else if (p.Type == MessageTypeEnum.Bye)
                {
                    NeedByeSendFlag = false;
                    SetState(FsmStateEnum.End);
                }
                else if (p.Type == MessageTypeEnum.Msg || p.Type == MessageTypeEnum.Reply)
                { 
                    // no state change
                }
                else
                {
                    SetState(FsmStateEnum.Error);
                }
                break;
            default:
                Io.DebugPrintLine("Unknown state...");
                break;
        }
        // Additional check for the Bye packet, if got in any state
        if (p.Type == MessageTypeEnum.Bye)
        {
            NeedByeSendFlag = false;
            SetState(FsmStateEnum.End);
        }
    }
}   