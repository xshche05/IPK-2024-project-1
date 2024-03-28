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
    public static readonly CancellationTokenSource? CancellationTokenSource = new ();
    public static readonly CancellationToken CancellationToken = CancellationTokenSource.Token;
    private static IPacket? ProcessInput(string input)
    {
        IPacket? packet = null;
        var msgParts = input.Split(" ");
        switch (msgParts)
        {
            case ["/auth", var username, var secret, var displayName]:
                // check if the client is not already authenticated and if the input is in correct format
                if (!ClientFsm.IsCommandAllowed("auth")
                    || !GrammarChecker.CheckUserName(username)
                    || !GrammarChecker.CheckSecret(secret)
                    || !GrammarChecker.CheckDisplayName(displayName)
                    ) break;
                // if udp build UdpPacketBuilder, if tcp build TcpPacketBuilder
                packet = SysArgParser.GetAppConfig().Protocol switch
                {
                    ProtocolEnum.Udp => UdpPacketBuilder.build_auth(username, displayName, secret),
                    ProtocolEnum.Tcp => TcpPacketBuilder.build_auth(username, displayName, secret),
                    _ => null
                };
                // update current display name
                _currentDisplayName = displayName;
                break;
            case ["/join", var channel]:
                // check if the client is authenticated and if the input is in correct format
                if (ClientFsm.IsCommandAllowed("join") 
                    || !GrammarChecker.CheckChanelId(channel)
                    ) break;
                packet = SysArgParser.GetAppConfig().Protocol switch
                {
                    ProtocolEnum.Udp => UdpPacketBuilder.build_join(channel, _currentDisplayName),
                    ProtocolEnum.Tcp => TcpPacketBuilder.build_join(channel, _currentDisplayName),
                    _ => null
                };
                break;
            case ["/rename", var displayName]:
                // check if the client is authenticated and if the input is in correct format
                if (ClientFsm.IsCommandAllowed("rename") 
                    || !GrammarChecker.CheckDisplayName(displayName)
                    ) break;
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
                // check if the client is authenticated and if the input is in correct format
                if (ClientFsm.IsCommandAllowed("msg") 
                    || !GrammarChecker.CheckMsg(input)
                    ) break;
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

    public static async Task CmdReader()
    {
        Io.DebugPrintLine("CmdReader started...");
        while (ClientFsm.State != FsmStateEnum.End)
        {
            string? msg = null;
            Task getLine = Task.Run(() =>
            {
                msg = Io.ReadLine();
            });
            try
            {
                getLine.Wait(CancellationToken);
            }
            catch (Exception e) // todo fix Exception
            {
                Io.DebugPrintLine(e.Message);
                break;
            }
            if (msg == null) break;
            IpkProject1.AuthSem.WaitOne();
            IPacket? packet = ProcessInput(msg);
            IpkProject1.AuthSem.Release();
            if (packet != null) await IpkProject1.GetClient().AddPacketToSendQueue(packet);
        }
        Io.DebugPrintLine("CmdReader terminated...");
    }
}