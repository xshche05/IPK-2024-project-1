using System.Text;
using IpkProject1.Enums;
using IpkProject1.Interfaces;
using IpkProject1.SysArg;
using IpkProject1.User;

namespace IpkProject1.Tcp;

public class TcpPacket : IPacket
{
    private readonly MessageTypeEnum _type;
    private readonly string _data;
    public MessageTypeEnum Type => _type;

    public TcpPacket(MessageTypeEnum type, string data) 
    {
        _type = type;
        _data = data;
    }
    
    public void Print()
    {
        Io.DebugPrintLine($"TCP packet : {_type}");
        string displayName;
        string message;
        var builder = new TcpPacketBuilder();
        switch (_type)
        {
            case MessageTypeEnum.Msg: // print message
                var msg = _data.Split(" ", 5);
                displayName = msg[2]; // sender
                if (!GrammarChecker.CheckDisplayName(displayName, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid display name!");
                    Program.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid display name!", ColorScheme.Error);
                    break;
                }
                message = msg[4][..^2]; // remove \r\n
                if (!GrammarChecker.CheckMsg(message, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid message!");
                    Program.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid message!", ColorScheme.Error);
                    break;
                }
                Io.PrintLine($"{displayName}: {message}", ColorScheme.Message);
                break;
            case MessageTypeEnum.Err: // print error
                var err = _data.Split(" ", 5);
                displayName = err[2]; // sender
                if (!GrammarChecker.CheckDisplayName(displayName, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid display name!");
                    Program.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid display name!", ColorScheme.Error);
                    break;
                }
                message = err[4][..^2]; // remove \r\n
                if (!GrammarChecker.CheckMsg(message, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid message!");
                    Program.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid message!", ColorScheme.Error);
                    break;
                }
                Io.ErrorPrintLine($"ERR FROM {displayName}: {message}", ColorScheme.Error);
                break;
            case MessageTypeEnum.Reply: // print reply
                var reply = _data.Split(" ", 4);
                var state = reply[1]; // OK or NOK
                message = reply[3][..^2]; // remove \r\n
                if (!GrammarChecker.CheckMsg(message, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid message!");
                    Program.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid message!", ColorScheme.Error);
                    break;
                }
                // print hexdump of message
                Io.DebugPrintLine($"{state}");
                Io.ErrorPrintLine((state.ToUpper() == "OK" ? "Success" : "Failure") + $": {message}",
                    state == "OK" ? ColorScheme.Info : ColorScheme.Error);
                break;
            case MessageTypeEnum.Bye: // do nothing
                Io.DebugPrintLine("Client disconnected (server send BYE)");
                break;
            case MessageTypeEnum.None:
            default: // unsupported packet
                TcpPacket errUnsupported = (TcpPacket)builder.build_error(InputProcessor.DisplayName, "Failed to parse packet!");
                Program.GetClient().AddPacketToSendQueue(errUnsupported);
                Io.ErrorPrintLine("ERR: Got packet unsupported by client!\n", ColorScheme.Error);
                break;
        }
    }
    
    public byte[] ToBytes()
    {
        return Encoding.ASCII.GetBytes(_data);
    }
    
    public bool? ReplyState()
    {
        if (_type == MessageTypeEnum.Reply)
        {
            return _data.Split(" ")[1] == "OK";
        }
        return null;
    }
}

public class TcpPacketBuilder : IPacketBuilder
{
    private const string Crlf = "\r\n";
    public IPacket build_auth(string login, string displayName, string secret)
    {
        var type = MessageTypeEnum.Auth;
        var data = $"AUTH {login} AS {displayName} USING {secret}{Crlf}";
        return new TcpPacket(type, data);
    }
    
    public IPacket build_msg(string displayName, string msg)
    {
        var type = MessageTypeEnum.Msg;
        var data = $"MSG FROM {displayName} IS {msg}{Crlf}";
        return new TcpPacket(type, data);
    }
    
    public IPacket build_error(string displayName, string msg)
    {
        var type = MessageTypeEnum.Err;
        var data = $"ERR FROM {displayName} IS {msg}{Crlf}";
        return new TcpPacket(type, data);
    }
    
    public IPacket build_join(string channel, string displayName)
    {
        var type = MessageTypeEnum.Join;
        var data = $"JOIN {channel} AS {displayName}{Crlf}";
        return new TcpPacket(type, data);
    }
    
    public IPacket build_bye()
    {
        var type = MessageTypeEnum.Bye;
        var data = $"BYE{Crlf}";
        return new TcpPacket(type, data);
    }
}

// Parser for TCP packets
public static class TcpPacketParser
{
    public static TcpPacket Parse(string data)
    {
        // Convert the data to upper case and split it by spaces, to ignore case
        var parts = data.ToUpper().Split(" ");
        return parts switch
        {
            ["REPLY", "OK", "IS", ..] => new TcpPacket(MessageTypeEnum.Reply, data),
            ["REPLY", "NOK", "IS", ..] => new TcpPacket(MessageTypeEnum.Reply, data),
            ["MSG", "FROM", _, "IS", ..] => new TcpPacket(MessageTypeEnum.Msg, data),
            ["ERR", "FROM", _, "IS", ..] => new TcpPacket(MessageTypeEnum.Err, data),
            ["BYE"] => new TcpPacket(MessageTypeEnum.Bye, data),
            _ => new TcpPacket(MessageTypeEnum.None, "Failed to parse packet!")
        };
    }
}