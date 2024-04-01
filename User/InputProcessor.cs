using IpkProject1.Enums;
using IpkProject1.Fsm;
using IpkProject1.Interfaces;
using IpkProject1.SysArg;
using IpkProject1.Tcp;
using IpkProject1.Udp;

namespace IpkProject1.User;

public static class InputProcessor
{
    private static string _currentDisplayName = "NONE";
    public static string DisplayName => _currentDisplayName;
    private static IPacket? ProcessInput(string input)
    {
        IPacket? packet = null;
        var msgParts = input.Split(" ");
        IPacketBuilder? builder = SysArgParser.Config.Protocol switch
        {
            ProtocolEnum.Udp => new UdpPacketBuilder(),
            ProtocolEnum.Tcp => new TcpPacketBuilder(),
            _ => null
        };
        if (builder == null) return null;
        switch (msgParts)
        {
            case ["/auth", var username, var secret, var displayName]:
                // check if the client is not already authenticated and if the input is in correct format
                if (!ClientFsm.IsCommandAllowed("auth") ||
                    !GrammarChecker.CheckUserName(username) ||
                    !GrammarChecker.CheckSecret(secret) ||
                    !GrammarChecker.CheckDisplayName(displayName)) break;
                // if udp build UdpPacketBuilder, if tcp build TcpPacketBuilder
                packet = builder.build_auth(username, displayName, secret);
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
                packet = builder.build_join(channel, _currentDisplayName);
                break;
            case ["/rename", var displayName]:
                // check if the client is authenticated and if the input is in correct format
                if (!ClientFsm.IsCommandAllowed("rename") ||
                    !GrammarChecker.CheckDisplayName(displayName)) break;
                _currentDisplayName = displayName;
                break;
            case ["/currentname"]:
                if (!ClientFsm.IsCommandAllowed("currentname")) break;
                Io.PrintLine($"Your current display name is: {_currentDisplayName}", ColorScheme.Info);
                break;
            case ["/help"]:
                Io.PrintLine("Commands:\n" +
                              "/auth <username> <secret> <display_name> - authentication command\n" +
                              "/join <channel>                          - join (create if not exists) to specified channel\n" +
                              "/rename <display_name>                   - changing the display name\n" +
                              "/currentname                             - prints your current display name\n" +
                              "\nOther inputs not started from slash is treated as messages\n", ColorScheme.Info);
                break;
            default:
                // if starts with / it is not a message write error
                if (input.StartsWith('/'))
                {
                    Io.ErrorPrintLine("ERR: Invalid command, please check your input!", ColorScheme.Warning);
                    break;
                }
                // check if the client is authenticated and if the input is in correct format
                if (!ClientFsm.IsCommandAllowed("msg") ||
                    !GrammarChecker.CheckMsg(input)) break;
                packet = builder.build_msg(_currentDisplayName, input);
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
                Io.DebugPrintLine($"Packet: {packet?.Type}");
            }
            IpkProject1.AuthSem.Release();
            // packet is null in case of invalid input, rename, help
            if (packet != null) IpkProject1.GetClient().AddPacketToSendQueue(packet);
        }
        Io.DebugPrintLine("CmdReader terminated...");
    }
}