using System.Diagnostics;
using IpkProject1.SysArg;

namespace IpkProject1.User;

public static class Io
{
    public static string LineTerminator { get; set; } = "\n";
    private static object _lock = new();

    public static string? ReadLine()
    {
        return Console.ReadLine();
    }

    public static void Print(string message, ConsoleColor? color)
    {
        lock (_lock)
        {
            if (color != null && SysArgParser.Config.Color)
                Console.ForegroundColor = (ConsoleColor)color;
            if (SysArgParser.Config.TimeStamp)
                Console.Error.Write(DateTime.Now.ToString("HH:mm:ss") + " ");
            Console.Write(message);
            Console.ResetColor();
            if (color != null && SysArgParser.Config.Color)
                Console.ResetColor();
        }
    }
    
    public static void PrintLine(string message, ConsoleColor? color)
    {
        Print(message + LineTerminator, color);
    }
    
    [Conditional("DEBUG")]
    public static void DebugPrint(string message)
    {
        lock (_lock)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.Error.Write(message);
            Console.ResetColor();
        }
    }
    
    [Conditional("DEBUG")]
    public static void DebugPrintLine(string message)
    {
        DebugPrint(message + LineTerminator);
    }
    
    public static void ErrorPrint(string message, ConsoleColor? color)
    {
        lock (_lock)
        {
            if (color != null && SysArgParser.Config.Color)
                Console.ForegroundColor = (ConsoleColor)color;
            if (SysArgParser.Config.TimeStamp)
                Console.Error.Write(DateTime.Now.ToString("HH:mm:ss") + " ");
            Console.Error.Write(message);
            Console.Error.Flush();
            if (color != null && SysArgParser.Config.Color)
                Console.ResetColor();   
        }
    }
    
    public static void ErrorPrintLine(string message, ConsoleColor? color)
    {
        ErrorPrint(message + LineTerminator, color);
    }
}