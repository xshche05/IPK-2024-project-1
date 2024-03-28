using IpkProject1.enums;
using IpkProject1.fsm;
using IpkProject1.interfaces;
using IpkProject1.sysarg;
using IpkProject1.tcp;
using IpkProject1.udp;
using IpkProject1.user;

namespace IpkProject1;

internal static class IpkProject1
{
    private static IChatClient? _chatClient;
    private static Task? _readerTask;
    private static Task? _printerTask;
    private static Task? _lastSendTask;
    private static Task? _senderTask;
    public static Semaphore AuthSem { get; } = new (1, 1);
    public static Semaphore TerminationSem { get; } = new (0, 100);
    public static void Main(string[] args)
    {
        // print debug messages to console
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            Io.DebugPrintLine("Ctrl+C pressed...");
            ClientFsm.SetState(FsmStateEnum.End);
        };
        SysArgParser.ParseArgs(args); // Command line arguments parsing
        SetUpClient(); // Client setup according to the protocol, host and port
        // Start cmd reader
        InputProcessor.CmdReader();
        // Wait until termination requested
        Io.DebugPrintLine("Waiting for termination...");
        TerminationSem.WaitOne();
        Terminate();
        Environment.Exit(0);
    }

    private static void Terminate()
    {
        Io.DebugPrintLine("Terminating...");

        Task.Delay(500).Wait();
        
        _chatClient?.Disconnect();
        
        _chatClient?.Close();
    }
    
    private static void SetUpClient()
    {
        // Get the configuration from the command line arguments
        var config = SysArgParser.GetAppConfig();
        
        // Initialize the client according to the protocol or exit if the protocol is invalid
        if (config.Protocol == ProtocolEnum.Tcp)
            _chatClient = new TcpChatClient(); 
        else if (config.Protocol == ProtocolEnum.Udp)
            _chatClient = new UdpChatClient();
        else
        {
            Console.Error.WriteLine("Invalid protocol or not specified");
            Environment.Exit(1);
        }
        
        // Check if the host is specified
        if (config.Host == null)
        {
            Console.Error.WriteLine("Host not specified");
            Environment.Exit(1);
        }
        
        // Connect to the server or bind to the host
        _chatClient.Connect(config.Host, config.Port);
        
        // Start the reader (accepts packets) and printer (print packets) tasks
        _readerTask = _chatClient.Reader();
        _printerTask = _chatClient.Printer();
        _senderTask = _chatClient.Sender();
    }
    
    public static IChatClient GetClient()
    {
        if (_chatClient == null)
            throw new NullReferenceException("Chat client not initialized");
        return _chatClient;
    }
    
    public static void SetLastSendTask(Task? task)
    {
        _lastSendTask = task;
    }
    
    public static Task? GetLastSendTask()
    {
        return _lastSendTask;
    }
}

// /auth xshche05 7cf1aa40-5e37-4bdb-b19f-94f642970673 spagetik