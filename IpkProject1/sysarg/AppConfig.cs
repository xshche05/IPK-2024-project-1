using IpkProject1.enums;

namespace IpkProject1.sysarg;

public record AppConfig()
{
    public int Port { get; init; } = 4567;
    public int Timeout { get; init; } = 250;
    public int Retries { get; init; } = 3;
    public string? Host { get; init; } = null;
    public ProtocolEnum? Protocol { get; init; } = null;
}