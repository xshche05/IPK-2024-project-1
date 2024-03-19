namespace IpkProject1.sysarg;

public class SysArgParser
{
    private static AppConfig _appConfig { get; set; } = new AppConfig();
    
    public static void ParseArgs(string[] args)
    {
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-t": 
                    Protocol protocol = args[i + 1] switch
                    {
                        "tcp" => Protocol.Tcp,
                        "udp" => Protocol.Udp,
                        _ => throw new ArgumentException("Invalid protocol")
                    };
                    _appConfig = _appConfig with { Protocol = protocol };
                    i++;
                    break;
                case "-s":
                    string host = args[i + 1];
                    _appConfig = _appConfig with { Host = host };
                    i++;
                    break;
                case "-p":
                    int port = int.Parse(args[i + 1]);
                    _appConfig = _appConfig with { Port = port };
                    i++;
                    break;
                case "-d":
                    int timeout = int.Parse(args[i + 1]);
                    _appConfig = _appConfig with { Timeout = timeout };
                    i++;
                    break;
                case "-r":
                    int retries = int.Parse(args[i + 1]);
                    _appConfig = _appConfig with { Retries = retries };
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
        return _appConfig;
    }
}