using System.Diagnostics;
using System.Text;
using IpkProject1.enums;
using IpkProject1.interfaces;
using IpkProject1.user;

namespace IpkProject1.tcp;

public class TcpPacketBuilder : IPacketBuilder
{
    private const string Crlf = "\r\n";
    public static TcpPacket build_auth(string login, string displayName, string secret)
    {
        var type = MessageTypeEnum.Auth;
        var data = $"AUTH {login} AS {displayName} USING {secret}{Crlf}";
        return new TcpPacket(type, data);
    }
    
    public static TcpPacket build_msg(string displayName, string msg)
    {
        var type = MessageTypeEnum.Msg;
        var data = $"MSG FROM {displayName} IS {msg}{Crlf}";
        return new TcpPacket(type, data);
    }
    
    public static TcpPacket build_error(string displayName, string msg)
    {
        var type = MessageTypeEnum.Err;
        var data = $"ERROR FROM {displayName} IS {msg}{Crlf}";
        return new TcpPacket(type, data);
    }
    
    public static TcpPacket build_join(string channel, string displayName)
    {
        var type = MessageTypeEnum.Join;
        var data = $"JOIN {channel} AS {displayName}{Crlf}";
        return new TcpPacket(type, data);
    }
    
    public static TcpPacket build_bye()
    {
        var type = MessageTypeEnum.Bye;
        var data = $"BYE{Crlf}";
        return new TcpPacket(type, data);
    }
}

public static class TcpPacketParser
{
    public static TcpPacket Parse(string data)
    {
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

public class TcpPacket : IPacket
{
    private readonly MessageTypeEnum _typeEnum;
    private readonly string _data;

    public TcpPacket(MessageTypeEnum typeEnum, string data) 
    {
        _typeEnum = typeEnum;
        _data = data;
    }
    
    public string GetMsgData()
    {
        return _data;
    }

    public void Print()
    {
        string displayName;
        string message;
        switch (_typeEnum)
        {
            case MessageTypeEnum.Msg: // print message
                var msg = _data.Split(" ", 5);
                displayName = msg[2]; // sender
                message = msg[4][..^2]; // remove \r\n
                Io.PrintLine($"{displayName}: {message}", ConsoleColor.Green);
                break;
            case MessageTypeEnum.Err: // print error
                var err = _data.Split(" ", 5);
                displayName = err[2]; // sender
                message = err[4][..^2]; // remove \r\n
                Io.ErrorPrintLine($"ERR FROM {displayName}: {message}");
                break;
            case MessageTypeEnum.Reply: // print reply
                var reply = _data.Split(" ", 4);
                var state = reply[1]; // OK or NOK
                message = reply[4][..^2]; // remove \r\n
                Io.ErrorPrintLine(state == "OK" ? "Success" : "Failure" + $": {message}\n");
                break;
            case MessageTypeEnum.Bye: // do nothing
                Io.DebugPrintLine("Client disconnected (server send BYE)");
                break;
            case MessageTypeEnum.None:
                // todo send error packet
                Io.ErrorPrintLine($"ERR: {_data}\n");
                break;
            default: // unsupported packet
                // todo send error packet
                Io.ErrorPrintLine("ERR: Got packet unsupported by client!\n");
                break;
        }
    }

    public byte[] ToBytes()
    {
        return Encoding.UTF8.GetBytes(_data);
    }

    public MessageTypeEnum GetMsgType()
    {
        return _typeEnum;
    }
}