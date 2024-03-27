using System.Diagnostics;

namespace IpkProject1.user;

public static class Io
{
    private static bool _inputRedirected;
    private static string[] inputBuffer;
    public static string LineTerminator { get; set; } = "\n";

    public static void InitRedirectedInputState()
    {
        _inputRedirected = Console.IsInputRedirected;
        if (_inputRedirected) 
            DebugPrintLine("Input is redirected...");
        else
            DebugPrintLine("Input via console...");
    }

    public static string? ReadLine()
    {
        return Console.ReadLine();
    }

    public static void Print(string message, ConsoleColor? color=null)
    {
        if (color != null)
            Console.ForegroundColor = (ConsoleColor)color;
        Console.Write(message);
        Console.ResetColor();
    }
    
    public static void PrintLine(string message, ConsoleColor? color=null)
    {
        Print(message + LineTerminator, color);
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
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.Write(message);
        Console.ResetColor();
    }
    
    public static void ErrorPrintLine(string message)
    {
        ErrorPrint(message + LineTerminator);
    }
}