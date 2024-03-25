using System.Diagnostics;
using IpkProject1.enums;
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
    
    public static void Main(string[] args)
    {
        // print debug messages to console
        Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
        Debug.AutoFlush = true;
        // Ctrl+C handler
        Console.CancelKeyPress += (sender, eventArgs) =>
        {
            eventArgs.Cancel = true;
            InputProcessor.CancellationTokenSource?.Cancel();
        };
        
        SysArgParser.ParseArgs(args); // Command line arguments parsing
        SetUpClient(); // Client setup according to the protocol, host and port
        
        // User input processing
        try
        {
            // Start cmd reader and wait for it to finish (end of input) or be cancelled by Ctrl+C
            InputProcessor.CmdReader().Wait(InputProcessor.CancellationToken);
            Debug.WriteLine("Connection terminated by end of input...");
        }
        catch (OperationCanceledException)
        {
            Debug.WriteLine("Connection aborted by Ctrl-C or server...");
        }
        finally
        {
            // Terminate the program, wait for all mandatory tasks to finish and close the connection
            Terminate();
        }
        // if debug 
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
        Environment.Exit(0);
    }

    private static void Terminate()
    {
        Debug.WriteLine("Terminating..."); 
        _lastSendTask?.Wait(); // Wait for the last message to be sent successfully
        IPacket byePacket = SysArgParser.GetAppConfig().Protocol switch
        {
            ProtocolEnum.Udp => UdpPacketBuilder.build_bye(),
            ProtocolEnum.Tcp => TcpPacketBuilder.build_bye(),
            _ => throw new ArgumentException("Invalid protocol")
        };
        _chatClient?.SendDataToServer(byePacket).Wait(); // Send bye message to the server
        _chatClient?.Disconnect(); // Disconnect from the server
        _readerTask?.Wait(); // Wait for the reader task to finish
        _printerTask?.Wait(); // Wait for the printer task to finish
    }
    
    private static void SetUpClient()
    {
        var config = SysArgParser.GetAppConfig(); // Get the configuration from the command line arguments
        
        // Initialize the client according to the protocol
        if (config.Protocol == ProtocolEnum.Tcp)
            _chatClient = new TcpChatClient(); 
        else if (config.Protocol == ProtocolEnum.Udp)
            _chatClient = new UdpChatClient();
        else
            throw new ArgumentException("Invalid protocol");
        
        // Check if the host is specified
        if (config.Host == null)
        {
            Console.Error.WriteLine("Host not specified");
            Environment.Exit(1);
        }
        _chatClient.Connect(config.Host, config.Port);
        _readerTask = _chatClient.Reader();
        _printerTask = _chatClient.Printer();
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
}

// /auth xshche05 7cf1aa40-5e37-4bdb-b19f-94f642970673 spagetik