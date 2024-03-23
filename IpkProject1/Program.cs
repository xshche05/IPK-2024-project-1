using IpkProject1.sysarg;
using IpkProject1.tcp;
using IpkProject1.user;

namespace IpkProject1;

static class IpkProject1
{
    private static TcpChatClient chatClient = new ();
    private static Task? readerTask, printerTask;
    private static Task? lastSendTask;
    
    public static void Main(string[] args)
    {
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Terminate();
        };
        // todo check protocol
        SysArgParser.ParseArgs(args);
        var config = SysArgParser.GetAppConfig();
        if (config.Host == null)
        {
            Console.Error.WriteLine("Host is required");
            Environment.Exit(1);
        }
        chatClient.Connect(config.Host, config.Port); // todo catch exceptions and print them to stderr and exit with error code 1 
        readerTask = chatClient.Reader();
        printerTask = chatClient.Printer();
        int i = 0;
        while (i < 20)
        {
            var msg = Console.ReadLine();
            if (msg == null) break;
            var packet = InputProcessor.ProcessInput(msg);
            if (packet != null) lastSendTask = chatClient.SendDataToServer(packet);
            i++;
        }
        Terminate();
    }

    private static void Terminate()
    { ;
        Console.WriteLine("Terminating...");
        if (lastSendTask != null) lastSendTask.Wait();
        chatClient.Disconnect();
        readerTask?.Wait();
        printerTask?.Wait();
        Environment.Exit(0);
    }
}

// /auth xshche05 7cf1aa40-5e37-4bdb-b19f-94f642970673 spagetik