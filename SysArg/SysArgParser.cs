using IpkProject1.Enums;
using IpkProject1.User;

namespace IpkProject1.SysArg;

public static class SysArgParser
{
    private static AppConfig _config = new ();
    // Property to access the configuration
    public static AppConfig Config => _config;
    
    // Help message
    const string Help = "Usage: ./ipk24chat-client -t <tcp|udp> -s <host> [-p <port>] [-d <timeout>] [-r <retries>] [-h]"
        + "\n\nOptions:"
        + "\n  -t <tcp|udp>    Protocol to use (TCP or UDP)"
        + "\n  -s <host>       Server hostname or IP address"
        + "\n  -p <port>       Server port (default 4567)"
        + "\n  -d <timeout>    Timeout for retransmissions in milliseconds (default 250)"
        + "\n  -r <retries>    Number of retries before giving up (default 3)"
        + "\n  -h              Display this help message";
    
    // Method to parse command line arguments
    public static void ParseArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            try
            {
                switch (args[i])
                {
                    case "-t":
                        ProtocolEnum? protocolEnum = args[i + 1].ToLower() switch
                        {
                            "tcp" => ProtocolEnum.Tcp,
                            "udp" => ProtocolEnum.Udp,
                            _ => null
                        };
                        _config = _config with { Protocol = protocolEnum };
                        i++;
                        break;
                    case "-s":
                        string host = args[i + 1];
                        _config = _config with { Host = host };
                        i++;
                        break;
                    case "-p":
                        int port = int.Parse(args[i + 1]);
                        _config = _config with { Port = port };
                        i++;
                        break;
                    case "-d":
                        int timeout = int.Parse(args[i + 1]);
                        _config = _config with { Timeout = timeout };
                        i++;
                        break;
                    case "-r":
                        int retries = int.Parse(args[i + 1]);
                        _config = _config with { Retries = retries };
                        i++;
                        break;
                    case "-c":
                        _config = _config with { Color = true };
                        break;
                    case "--ts":
                        _config = _config with { TimeStamp = true };
                        break;
                    case "-h":
                        Io.PrintLine(Help, null);
                        Environment.Exit(0);
                        break;
                }
            } catch (FormatException)
            {
                // Catch the exception if the argument format is invalid
                Io.ErrorPrintLine("Invalid argument format!", ColorScheme.Error);
                Io.PrintLine(Help, null);
                Environment.Exit(1);
            }
        }
        // Check if the required arguments are present
        if (_config.Protocol == null || _config.Host == null)
        {
            Io.ErrorPrintLine("Missing required arguments!", ColorScheme.Error);
            Io.PrintLine(Help, null);
            Environment.Exit(1);
        }
    }
}