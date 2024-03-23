using System.Collections.Concurrent;
using IpkProject1.enums;
using IpkProject1.fsm;
using IpkProject1.sysarg;
using IpkProject1.tcp;

namespace IpkProject1;

class IpkProject1
{
    static void Main(string[] args)
    {
        string ip = "anton5.fit.vutbr.cz"; // anton5.fit.vutbr.cz
        int port = 4567;
        string dname = "default_name";
        SysArgParser.ParseArgs(args);
        // catch exceptions and print them to stderr and exit with error code 1 
        TcpChatClient chatClient = new ();
        chatClient.Connect(ip, port);
        Task readerTask = chatClient.Reader();
        Task printerTask = chatClient.Printer();
        Task? lastSendTask = null;
        int i = 0;
        while (i < 20)
        {
            string? msg = Console.ReadLine();
            TcpPacket? packet = null;
            if (msg == "stop" || msg == null)
            {
                break;
            }
            string[] msgParts = msg.Split(" ");
            switch (msgParts)
            {
                case ["/auth", var username, var secret, var displayName]:
                    if (ClientFsm.GetState() == FsmStateEnum.Start)
                    {
                        ClientFsm.SetState(FsmStateEnum.Auth);
                    }
                    else if (ClientFsm.GetState() != FsmStateEnum.Auth)
                    {
                        Console.WriteLine("You are already authenticated");
                        break;
                    }
                    packet = TcpPacketBuilder.build_auth(username, displayName, secret);
                    dname = displayName;
                    break;
                case ["/join", var channel]:
                    if (ClientFsm.GetState() != FsmStateEnum.Open)
                    {
                        Console.WriteLine("You are not authenticated");
                        break;
                    }
                    packet = TcpPacketBuilder.build_join(channel, dname);
                    break;
                case ["/rename", var displayName]:
                    if (ClientFsm.GetState() != FsmStateEnum.Open)
                    {
                        Console.WriteLine("You are not authenticated");
                        break;
                    }
                    dname = displayName;
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
                        Console.WriteLine("You are not authenticated");
                        break;
                    }
                    packet = TcpPacketBuilder.build_msg(dname, msg);
                    break;
            }
            if (packet != null)
            {
                lastSendTask = chatClient.SendDataToServer(packet);
            }
            i++;
        }
        if (lastSendTask != null)
        {
            lastSendTask.Wait();
        }
        chatClient.Disconnect();
        readerTask.Wait();
        printerTask.Wait();
    }
}

// /auth xshche05 7cf1aa40-5e37-4bdb-b19f-94f642970673 spagetik