namespace IpkProject1.SysArg;

public record ColorScheme
{
    public static readonly ConsoleColor? Default  = null;
    public static readonly ConsoleColor? Message = ConsoleColor.Green;
    public static readonly ConsoleColor? Error = ConsoleColor.Red;
    public static readonly ConsoleColor? Warning = ConsoleColor.Yellow;
    public static readonly ConsoleColor? Info = ConsoleColor.Cyan;
    public static readonly ConsoleColor? Debug = ConsoleColor.Magenta;
}