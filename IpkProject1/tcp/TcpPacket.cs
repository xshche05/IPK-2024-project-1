using System.Text;
using IpkProject1.enums;
using IpkProject1.interfaces;
using IpkProject1.user;

namespace IpkProject1.tcp;

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
                    IpkProject1.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid display name!");
                    break;
                }
                message = msg[4][..^2]; // remove \r\n
                if (!GrammarChecker.CheckMsg(message, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid message!");
                    IpkProject1.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid message!");
                    break;
                }
                Io.PrintLine($"{displayName}: {message}");
                break;
            case MessageTypeEnum.Err: // print error
                var err = _data.Split(" ", 5);
                displayName = err[2]; // sender
                if (!GrammarChecker.CheckDisplayName(displayName, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid display name!");
                    IpkProject1.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid display name!");
                    break;
                }
                message = err[4][..^2]; // remove \r\n
                if (!GrammarChecker.CheckMsg(message, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid message!");
                    IpkProject1.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid message!");
                    break;
                }
                Io.ErrorPrintLine($"ERR FROM {displayName}: {message}");
                break;
            case MessageTypeEnum.Reply: // print reply
                var reply = _data.Split(" ", 4);
                var state = reply[1]; // OK or NOK
                message = reply[3][..^2]; // remove \r\n
                if (!GrammarChecker.CheckMsg(message, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid message!");
                    IpkProject1.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid message!");
                    break;
                }
                // print hexdump of message
                Io.DebugPrintLine($"{state}");
                Io.ErrorPrintLine((state == "OK" ? "Success" : "Failure") + $": {message}");
                break;
            case MessageTypeEnum.Bye: // do nothing
                Io.DebugPrintLine("Client disconnected (server send BYE)");
                break;
            case MessageTypeEnum.None:
                TcpPacket errNone = (TcpPacket)builder.build_error(InputProcessor.DisplayName, "Failed to parse packet!");
                IpkProject1.GetClient().AddPacketToSendQueue(errNone);
                Io.ErrorPrintLine($"ERR: {_data}");
                break;
            default: // unsupported packet
                TcpPacket errUnsupported = (TcpPacket)builder.build_error(InputProcessor.DisplayName, "Failed to parse packet!");
                IpkProject1.GetClient().AddPacketToSendQueue(errUnsupported);
                Io.ErrorPrintLine("ERR: Got packet unsupported by client!\n");
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