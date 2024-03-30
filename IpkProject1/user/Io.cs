using System.Diagnostics;

namespace IpkProject1.user;

public static class Io
{
    public static string LineTerminator { get; set; } = "\n";

    public static string? ReadLine()
    {
        return Console.ReadLine();
    }

    public static void Print(string message)
    {
        // if (color != null)
        //     Console.ForegroundColor = (ConsoleColor)color;
        Console.Write(message);
        Console.ResetColor();
    }
    
    public static void PrintLine(string message)
    {
        Print(message + LineTerminator);
    }
    
    [Conditional("DEBUG")]
    public static void DebugPrint(string message)
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Error.Write(message);
        Console.ResetColor();
    }
    
    [Conditional("DEBUG")]
    public static void DebugPrintLine(string message)
    {
        DebugPrint(message + LineTerminator);
    }
    
    public static void ErrorPrint(string message)
    {
        Console.Error.Write(message);
        Console.Error.Flush();
        Console.ResetColor();
    }
    
    public static void ErrorPrintLine(string message)
    {
        ErrorPrint(message + LineTerminator);
    }
}