using System.Text;
using IpkProject1.enums;
using IpkProject1.fsm;
using IpkProject1.interfaces;
using IpkProject1.sysarg;
using IpkProject1.tcp;
using IpkProject1.udp;

namespace IpkProject1.user;

public static class InputProcessor
{
    private static string _currentDisplayName = "NONE";
    public static string DisplayName => _currentDisplayName;
    private static IPacket? ProcessInput(string input)
    {
        IPacket? packet = null;
        var msgParts = input.Split(" ");
        switch (msgParts)
        {
            case ["/auth", var username, var secret, var displayName]:
                // check if the client is not already authenticated and if the input is in correct format
                if (!ClientFsm.IsCommandAllowed("auth") ||
                    !GrammarChecker.CheckUserName(username) ||
                    !GrammarChecker.CheckSecret(secret) ||
                    !GrammarChecker.CheckDisplayName(displayName)) break;
                // if udp build UdpPacketBuilder, if tcp build TcpPacketBuilder
                packet = SysArgParser.GetAppConfig().Protocol switch
                {
                    ProtocolEnum.Udp => UdpPacketBuilder.build_auth(username, displayName, secret),
                    ProtocolEnum.Tcp => TcpPacketBuilder.build_auth(username, displayName, secret),
                    _ => null
                };
                // update current display name
                _currentDisplayName = displayName;
                if (ClientFsm.State == FsmStateEnum.Start)
                {
                    ClientFsm.SetState(FsmStateEnum.Auth);
                }
                break;
            case ["/join", var channel]:
                // check if the client is authenticated and if the input is in correct format
                if (!ClientFsm.IsCommandAllowed("join") ||
                    !GrammarChecker.CheckChanelId(channel)) break;
                packet = SysArgParser.GetAppConfig().Protocol switch
                {
                    ProtocolEnum.Udp => UdpPacketBuilder.build_join(channel, _currentDisplayName),
                    ProtocolEnum.Tcp => TcpPacketBuilder.build_join(channel, _currentDisplayName),
                    _ => null
                };
                break;
            case ["/rename", var displayName]:
                // check if the client is authenticated and if the input is in correct format
                if (!ClientFsm.IsCommandAllowed("rename") ||
                    !GrammarChecker.CheckDisplayName(displayName)) break;
                _currentDisplayName = displayName;
                break;
            case ["/help"]:
                Io.PrintLine("Commands:\n" +
                              "/auth <username> <secret> <display_name>\n" +
                              "/join <channel>\n" +
                              "/rename <display_name>\n" +
                              "/msg <message>\n" +
                              "Other inputs or commands which are not in specified format will be treated as messages\n");
                break;
            default:
                // if starts with / it is not a message write error
                if (input.StartsWith('/'))
                {
                    Io.ErrorPrintLine("ERR: Invalid command, please check your input!");
                    break;
                }
                // check if the client is authenticated and if the input is in correct format
                if (!ClientFsm.IsCommandAllowed("msg") ||
                    !GrammarChecker.CheckMsg(input)) break;
                packet = SysArgParser.GetAppConfig().Protocol switch
                {
                    ProtocolEnum.Udp => UdpPacketBuilder.build_msg(_currentDisplayName, input),
                    ProtocolEnum.Tcp => TcpPacketBuilder.build_msg(_currentDisplayName, input),
                    _ => null
                };
                break;
        }
        return packet;
    }
    public static async void CmdReader()
    {
        Io.DebugPrintLine("CmdReader started...");
        string? msg = "";
        while (ClientFsm.State != FsmStateEnum.End)
        {
            IPacket? packet = null;
            Task getLine = Task.Run(() => msg = Io.ReadLine()); // Async read line
            await getLine; // Wait for user input
            IpkProject1.AuthSem.WaitOne(); // Wait if auth is in progress
            if (msg == null)
            {
                ClientFsm.SetState(FsmStateEnum.End); // Ctrl+D pressed
            }
            else
            {
                packet = ProcessInput(msg); // Process user input
                Io.DebugPrintLine($"Packet: {packet?.GetMsgType()}");
            }
            IpkProject1.AuthSem.Release();
            // packet is null in case of invalid input, rename, help
            if (packet != null) IpkProject1.GetClient().AddPacketToSendQueue(packet);
        }
        Io.DebugPrintLine("CmdReader terminated...");
    }
}