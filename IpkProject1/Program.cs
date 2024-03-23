using IpkProject1.enums;
using IpkProject1.fsm;
using IpkProject1.interfaces;
using IpkProject1.sysarg;
using IpkProject1.tcp;
using IpkProject1.udp;
using IpkProject1.user;

namespace IpkProject1;

static class IpkProject1
{
    private static IChatClient? _chatClient;
    private static Task? _readerTask;
    private static Task? _printerTask;
    private static Task? _lastSendTask;
    
    public static void Main(string[] args)
    {
        // Ctrl+C handler
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            _chatClient?.SendDataToServer(TcpPacketBuilder.build_bye()).Wait();
        };
        // Command line arguments parsing
        SysArgParser.ParseArgs(args);
        // Client setup according to the protocol, host and port
        SetUpClient();
        // User input processing
        try
        {
            InputProcessor.CmdReader().Wait(InputProcessor.CancellationToken);
        }
        finally
        {
            Terminate();
        }
    }

    private static void Terminate()
    {
        Console.WriteLine("Terminating..."); 
        _lastSendTask?.Wait();
        _chatClient?.Disconnect();
        _readerTask?.Wait();
        _printerTask?.Wait();
    }
    
    private static void SetUpClient()
    {
        var config = SysArgParser.GetAppConfig();
        
        if (config.ProtocolEnum == ProtocolEnum.Tcp)
            _chatClient = new TcpChatClient();
        else if (config.ProtocolEnum == ProtocolEnum.Udp)
            _chatClient = new UdpChatClient();
        else
            throw new ArgumentException("Invalid protocol");

        if (config.Host == null)
        {
            Console.Error.WriteLine("Host not specified");
            Environment.Exit(1);
        }
        _chatClient.Connect(config.Host, config.Port);
        _readerTask = _chatClient.Reader();
        _printerTask = _chatClient.Printer();
    }
    
    public static IChatClient? GetClient()
    {
        return _chatClient;
    }
    
    public static void SetLastSendTask(Task? task)
    {
        _lastSendTask = task;
    }
}

// /auth xshche05 7cf1aa40-5e37-4bdb-b19f-94f642970673 spagetik