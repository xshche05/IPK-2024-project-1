using IpkProject1.enums;
using IpkProject1.fsm;
using IpkProject1.interfaces;
using IpkProject1.sysarg;
using IpkProject1.tcp;
using IpkProject1.udp;

namespace IpkProject1.user;

public static class InputProcessor
{
    private static string CurrentDisplayName = "";
    public static readonly CancellationTokenSource? CancellationTokenSource = new ();
    public static readonly CancellationToken CancellationToken = CancellationTokenSource.Token;
    private static IPacket? ProcessInput(string input)
    {
        IPacket? packet = null;
        var msgParts = input.Split(" ");
        switch (msgParts)
        {
            case ["/auth", var username, var secret, var displayName]:
                // check if the client is not already authenticated
                if (ClientFsm.GetState() != FsmStateEnum.Auth && ClientFsm.GetState() != FsmStateEnum.Start)
                {
                    Io.PrintLine("WARNING: You are already authenticated, cannot authenticate again!", ConsoleColor.Yellow);
                    break;
                }
                // check if the input is valid
                if (!GrammarChecker.CheckUserName(username))
                {
                    Io.PrintLine("WARNING: Invalid username, please check your input!", ConsoleColor.Yellow);
                    break;
                }
                if (!GrammarChecker.CheckSecret(secret))
                {
                    Io.PrintLine("WARNING: Invalid secret, please check your input!", ConsoleColor.Yellow);
                    break;
                }
                if (!GrammarChecker.CheckDisplayName(displayName))
                {
                    Io.PrintLine("WARNING: Invalid display name, please check your input!", ConsoleColor.Yellow);
                    break;
                }
                // if udp build UdpPacketBuilder, if tcp build TcpPacketBuilder
                packet = SysArgParser.GetAppConfig().Protocol switch
                {
                    ProtocolEnum.Udp => UdpPacketBuilder.build_auth(username, displayName, secret),
                    ProtocolEnum.Tcp => TcpPacketBuilder.build_auth(username, displayName, secret),
                    _ => null
                };
                // update current display name
                CurrentDisplayName = displayName;
                break;
            case ["/join", var channel]:
                // check if the client is authenticated
                if (ClientFsm.GetState() != FsmStateEnum.Open)
                {
                    Io.PrintLine("WARNING: You are not authenticated, cannot join a channel!", ConsoleColor.Yellow);
                    break;
                }
                if (!GrammarChecker.CheckChanelId(channel))
                {
                    Io.PrintLine("WARNING: Invalid channel id, please check your input!", ConsoleColor.Yellow);
                    break;
                }
                packet = SysArgParser.GetAppConfig().Protocol switch
                {
                    ProtocolEnum.Udp => UdpPacketBuilder.build_join(channel, CurrentDisplayName),
                    ProtocolEnum.Tcp => TcpPacketBuilder.build_join(channel, CurrentDisplayName),
                    _ => null
                };
                break;
            case ["/rename", var displayName]:
                // check if the client is authenticated
                if (ClientFsm.GetState() != FsmStateEnum.Open)
                {
                    Io.PrintLine("WARNING: You are not authenticated, cannot rename!", ConsoleColor.Yellow);
                    break;
                }
                if (!GrammarChecker.CheckDisplayName(displayName))
                {
                    Io.PrintLine("WARNING: Invalid display name, please check your input!", ConsoleColor.Yellow);
                    break;
                }
                CurrentDisplayName = displayName;
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
                if (ClientFsm.GetState() != FsmStateEnum.Open)
                {
                    Io.PrintLine("WARNING: You are not authenticated, cannot send a message!", ConsoleColor.Yellow);
                    break;
                }
                if (!GrammarChecker.CheckMsg(input))
                {
                    Io.PrintLine("WARNING: Invalid message, please check your input!", ConsoleColor.Yellow);
                    break;
                }
                packet = SysArgParser.GetAppConfig().Protocol switch
                {
                    ProtocolEnum.Udp => UdpPacketBuilder.build_msg(CurrentDisplayName, input),
                    ProtocolEnum.Tcp => TcpPacketBuilder.build_msg(CurrentDisplayName, input),
                    _ => null
                };
                break;
        }
        return packet;
    }

    public static async Task CmdReader()
    {
        Io.DebugPrintLine("CmdReader started...");
        while (ClientFsm.GetState() != FsmStateEnum.End)
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
            IPacket? packet;
            IpkProject1.AuthSem.WaitOne();
            packet = ProcessInput(msg);
            IpkProject1.AuthSem.Release();
            if (packet != null) await IpkProject1.GetClient().AddPacketToSendQueue(packet);
        }
        Io.DebugPrintLine("CmdReader terminated...");
    }
}