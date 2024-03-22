using System.Collections.Concurrent;
using IpkProject1.fsm;
using IpkProject1.sysarg;
using IpkProject1.tcp;

namespace IpkProject1;

class IpkProject1
{
    public static BlockingCollection<TcpPacket> gotPackets = new BlockingCollection<TcpPacket>();
    static void Main(string[] args)
    {
        string ip = "anton5.fit.vutbr.cz"; // anton5.fit.vutbr.cz
        int port = 4567;
        string dname = "default_name";
        SysArgParser.ParseArgs(args);
        // catch exceptions and print them to stderr and exit with error code 1 
        TcpChatClient.Connect(ip, port);
        Task reader = TcpChatClient.Reader(gotPackets);
        Task printer = TcpChatClient.Printer(gotPackets);
        Task? lastsender = null;
        int i = 0;
        while (i < 20)
        {
            string? msg = Console.ReadLine();
            TcpPacket? packet = null;
            if (msg == "stop" || msg == null)
            {
                break;
            }
            string[] msg_parts = msg.Split(" ");
            switch (msg_parts)
            {
                case ["/auth", string username, string secret, string display_name]:
                    if (ClientFsm.GetState() == FsmState.Start)
                    {
                        ClientFsm.SetState(FsmState.Auth);
                    }
                    else if (ClientFsm.GetState() != FsmState.Auth)
                    {
                        Console.WriteLine("You are already authenticated");
                        break;
                    }
                    packet = TcpPacketBuilder.build_auth(username, display_name, secret);
                    dname = display_name;
                    break;
                case ["/join", string channel]:
                    if (ClientFsm.GetState() != FsmState.Open)
                    {
                        Console.WriteLine("You are not authenticated");
                        break;
                    }
                    packet = TcpPacketBuilder.build_join(channel, dname);
                    break;
                case ["/rename", string display_name]:
                    if (ClientFsm.GetState() != FsmState.Open)
                    {
                        Console.WriteLine("You are not authenticated");
                        break;
                    }
                    dname = display_name;
                    break;
                case ["/help"]:
                    Console.WriteLine("Commands:\n" +
                                      "/auth <username> <secret> <display_name>\n" +
                                      "/join <channel>\n" +
                                      "/rename <display_name>\n" +
                                      "/msg <message>\n");
                    break;
                default:
                    if (ClientFsm.GetState() != FsmState.Open)
                    {
                        Console.WriteLine("You are not authenticated");
                        break;
                    }
                    packet = TcpPacketBuilder.build_msg(dname, msg);
                    break;
            }
            if (packet != null)
            {
                lastsender = TcpChatClient.SendDataToServer(packet);
            }
            i++;
        }
        if (lastsender != null)
        {
             lastsender.Wait();
        }
        TcpChatClient.Disconnect();
        reader.Wait();
        printer.Wait();
    }
}

// /auth xshche05 7cf1aa40-5e37-4bdb-b19f-94f642970673 spagetik