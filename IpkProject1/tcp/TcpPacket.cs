using System.Text;
using IpkProject1.enums;
using IpkProject1.interfaces;

namespace IpkProject1.tcp;

public static class TcpPacketBuilder
{
    // todo add checks for args format
    private const string Crlf = "\r\n";
    public static TcpPacket build_auth(string login, string dname, string secret)
    {
        var type = MessageTypeEnum.Auth;
        var data = $"AUTH {login} AS {dname} USING {secret}{Crlf}";
        return new TcpPacket(type, data);
    }
    
    public static TcpPacket build_msg(string dname, string msg)
    {
        var type = MessageTypeEnum.Msg;
        var data = $"MSG FROM {dname} IS {msg}{Crlf}";
        return new TcpPacket(type, data);
    }
    
    public static TcpPacket build_error(string dname, string msg)
    {
        var type = MessageTypeEnum.Err;
        var data = $"ERROR FROM {dname} IS {msg}{Crlf}";
        return new TcpPacket(type, data);
    }
    
    public static TcpPacket build_join(string channel, string dname)
    {
        var type = MessageTypeEnum.Join;
        var data = $"JOIN {channel} AS {dname}{Crlf}";
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
        string[] parts = data.ToUpper().Split(" ");
        switch (parts)
        {
            case ["REPLY", "OK", "IS", ..]:
                return new TcpPacket(MessageTypeEnum.Reply, data);
            case ["REPLY", "NOK", "IS", ..]:
                return new TcpPacket(MessageTypeEnum.Reply, data);
            case ["MSG", "FROM", _ , "IS", ..]:
                return new TcpPacket(MessageTypeEnum.Msg, data);
            case ["ERR", "FROM", _ , "IS", ..]:
                return new TcpPacket(MessageTypeEnum.Err, data);
            case ["BYE"]:
                return new TcpPacket(MessageTypeEnum.Bye, "Bye");
            default:
                Console.WriteLine(parts);
                break;
        }
        throw new Exception("Unknown incoming message type!");
    }
}

public class TcpPacket : IPacket
{
    private MessageTypeEnum _typeEnum;
    private string _data;

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
        string dname;
        string message;
        string state;
        switch (_typeEnum)
        {
            case MessageTypeEnum.Msg:
                string[] msg = _data.Split(" ", 5);
                dname = msg[2];
                message = msg[4].Trim();
                Console.Write($"{dname}: {message}\n");
                break;
            case MessageTypeEnum.Err:
                string[] err = _data.Split(" ", 5);
                dname = err[2];
                message = err[4].Trim();
                Console.Error.Write($"{dname}: {message}\n");
                break;
            case MessageTypeEnum.Reply:
                string[] reply = _data.Split(" ", 4);
                state = reply[1];
                message = reply[3].Trim();
                if (state == "OK")
                {
                    Console.Error.Write($"Success: {message}\n");
                }
                else if (state == "NOK")
                {
                    Console.Error.Write($"Failure: {message}\n");
                } else
                {
                    // todo error handling
                    Console.Error.Write($"Unknown state: {state}\n");
                }
                break;
            default:
                Console.WriteLine(_data.Trim());
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