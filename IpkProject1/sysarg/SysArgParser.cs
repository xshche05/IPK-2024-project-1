using IpkProject1.enums;

namespace IpkProject1.sysarg;

public class SysArgParser
{
    private static AppConfig Config { get; set; } = new AppConfig();
    
    public static void ParseArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-t": 
                    ProtocolEnum protocolEnum = args[i + 1] switch
                    {
                        "tcp" => ProtocolEnum.Tcp,
                        "udp" => ProtocolEnum.Udp,
                        _ => throw new ArgumentException("Invalid protocol")
                    };
                    Config = Config with { ProtocolEnum = protocolEnum };
                    i++;
                    break;
                case "-s":
                    string host = args[i + 1];
                    Config = Config with { Host = host };
                    i++;
                    break;
                case "-p":
                    int port = int.Parse(args[i + 1]);
                    Config = Config with { Port = port };
                    i++;
                    break;
                case "-d":
                    int timeout = int.Parse(args[i + 1]);
                    Config = Config with { Timeout = timeout };
                    i++;
                    break;
                case "-r":
                    int retries = int.Parse(args[i + 1]);
                    Config = Config with { Retries = retries };
                    i++;
                    break;
                case "-h":
                    Console.WriteLine("Help");
                    // exit with zero code
                    Environment.Exit(0);
                    break;
            }
        }
    }
    
    public static AppConfig GetAppConfig()
    {
        return Config;
    }
}