using IpkProject1.enums;
using IpkProject1.fsm;
using IpkProject1.sysarg;
using IpkProject1.tcp;
using IpkProject1.user;

namespace IpkProject1;

static class IpkProject1
{
    public static void Main(string[] args)
    {
        // todo check protocol
        SysArgParser.ParseArgs(args);
        var config = SysArgParser.GetAppConfig();
        TcpChatClient chatClient = new ();
        if (config.Host == null)
        {
            Console.Error.WriteLine("Host is required");
            Environment.Exit(1);
        }
        chatClient.Connect(config.Host, config.Port); // todo catch exceptions and print them to stderr and exit with error code 1 
        Task readerTask = chatClient.Reader();
        Task printerTask = chatClient.Printer();
        Task? lastSendTask = null;
        int i = 0;
        while (i < 20)
        {
            var msg = Console.ReadLine();
            if (msg == null) break;
            var packet = InputProcessor.ProcessInput(msg);
            if (packet != null) lastSendTask = chatClient.SendDataToServer(packet);
            i++;
        }
        if (lastSendTask != null) lastSendTask.Wait();
        chatClient.Disconnect();
        readerTask.Wait();
        printerTask.Wait();
    }
}

// /auth xshche05 7cf1aa40-5e37-4bdb-b19f-94f642970673 spagetik