using IpkProject1.enums;
using IpkProject1.fsm;
using IpkProject1.tcp;

namespace IpkProject1.user;

public static class InputProcessor
{
    private static string CurrentDisplayName = "";
    private static TcpPacket? ProcessInput(string input)
    {
        TcpPacket? packet = null;
        var msgParts = input.Split(" ");
        switch (msgParts)
        {
            case ["/auth", var username, var secret, var displayName]:
                if (ClientFsm.GetState() != FsmStateEnum.Auth && ClientFsm.GetState() != FsmStateEnum.Start)
                {
                    Console.WriteLine("You are already authenticated, cannot authenticate again");
                    break;
                }
                packet = TcpPacketBuilder.build_auth(username, displayName, secret);
                CurrentDisplayName = displayName;
                break;
            case ["/join", var channel]:
                if (ClientFsm.GetState() != FsmStateEnum.Open)
                {
                    Console.WriteLine("You are not authenticated, cannot join channels");
                    break;
                }
                packet = TcpPacketBuilder.build_join(channel, CurrentDisplayName);
                break;
            case ["/rename", var displayName]:
                if (ClientFsm.GetState() != FsmStateEnum.Open)
                {
                    Console.WriteLine("You are not authenticated, cannot perform rename");
                    break;
                }
                CurrentDisplayName = displayName;
                break;
            case ["/help"]:
                Console.WriteLine("Commands:\n" +
                                  "/auth <username> <secret> <display_name>\n" +
                                  "/join <channel>\n" +
                                  "/rename <display_name>\n" +
                                  "/msg <message>\n");
                break;
            default:
                if (ClientFsm.GetState() != FsmStateEnum.Open)
                {
                    Console.WriteLine("You are not authenticated, cannot send messages");
                    break;
                }
                packet = TcpPacketBuilder.build_msg(CurrentDisplayName, input);
                break;
        }
        return packet;
    }
    
    public static CancellationTokenSource? CancellationTokenSource = new ();
    public static CancellationToken CancellationToken = CancellationTokenSource.Token;

    public static async Task CmdReader()
    {
        while (ClientFsm.GetState() != FsmStateEnum.End)
        {
            var msg = await Task.Run(() => Console.ReadLine());
            if (msg == null) break;
            var packet = ProcessInput(msg);
            if (packet != null) IpkProject1.SetLastSendTask(IpkProject1.GetClient()?.SendDataToServer(packet));
        }
    }
}