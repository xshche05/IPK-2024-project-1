using IpkProject1.enums;

namespace IpkProject1.sysarg;

public record AppConfig()
{
    // Server port (default 4567)
    public int Port { get; init; } = 4567;
    // Timeout for UPD retransmissions in milliseconds (default 250)
    public int Timeout { get; init; } = 250;
    // Number of retries for UDP before giving up (default 3)
    public int Retries { get; init; } = 3;
    // Server hostname or IP address
    public string? Host { get; init; } = null;
    // Protocol to use (TCP or UDP)
    public ProtocolEnum? Protocol { get; init; } = null;
}