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
    public static object LockObj { get; } = new();
    public static Semaphore AuthSem { get; } = new (1, 1);
    public static object AuthSyncLockObj { get; } = new();
    
    public static CancellationTokenSource TimeoutCancellationTokenSource { get; } = new();
    private static CancellationToken TimeoutCancellationToken => TimeoutCancellationTokenSource.Token;

    public static void Main(string[] args)
    {
        // print debug messages to console
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            // Cancel the Ctrl+C signal
            eventArgs.Cancel = true;
            Io.DebugPrintLine("Ctrl+C pressed...");
            // Set FSM to END state
            lock (LockObj)
            {
                if (!InputProcessor.CancellationToken.IsCancellationRequested) ClientFsm.SetState(FsmStateEnum.End);
            }
        };
        Io.InitRedirectedInputState();
        SysArgParser.ParseArgs(args); // Command line arguments parsing
        SetUpClient(); // Client setup according to the protocol, host and port
        
        // User input processing
        // Start cmd reader and wait for it to finish (end of input) or be cancelled by Ctrl+C
        try
        {
            InputProcessor.CmdReader().Wait(InputProcessor.CancellationToken);
        }
        catch (Exception e)
        {
            Io.DebugPrintLine(e.Message);
        }
        
        Io.DebugPrintLine("Input processing finished...");
        // Set FSM to END state
        lock (LockObj)
        {
            if (!InputProcessor.CancellationToken.IsCancellationRequested) ClientFsm.SetState(FsmStateEnum.End);
        }
        // Wait for all mandatory tasks to finish
        Terminate();
        // Exit the program
        Environment.Exit(0);
    }

    private static void Terminate()
    {
        Io.DebugPrintLine("Terminating...");
        
        // Disconnect from the server
        _chatClient?.Disconnect();
        
        // Wait for the sender task to finish
        _senderTask?.Wait();
        
        _chatClient._isSenderTerminated = true;

        try
        {
            // Wait for the reader task to finish
            _readerTask?.Wait(TimeoutCancellationToken);

            // Wait for the printer task to finish
            _printerTask?.Wait(TimeoutCancellationToken);
        }
        catch (Exception e)
        {
            Io.DebugPrintLine(e.Message);
        }
        finally
        {
            // Close the client
            _chatClient.Close();
        }
        
        
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