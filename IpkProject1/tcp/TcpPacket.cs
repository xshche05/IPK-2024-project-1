using System.Text;
using IpkProject1.Messages;

namespace IpkProject1.tcp;

public class TcpPacketBuilder
{
    // todo add checks for args format
    private const string CRLF = "\r\n";
    public static TcpPacket build_auth(string login, string dname, string secret)
    {
        var type = MessageType.Auth;
        var data = $"AUTH {login} AS {dname} USING {secret}{CRLF}";
        return new TcpPacket(type, data);
    }
    
    public static TcpPacket build_msg(string dname, string msg)
    {
        var type = MessageType.Msg;
        var data = $"MSG FROM {dname} IS {msg}{CRLF}";
        return new TcpPacket(type, data);
    }
    
    public static TcpPacket build_error(string dname, string msg)
    {
        var type = MessageType.Err;
        var data = $"ERROR FROM {dname} IS {msg}{CRLF}";
        return new TcpPacket(type, data);
    }
    
    public static TcpPacket build_join(string channel, string dname)
    {
        var type = MessageType.Join;
        var data = $"JOIN {channel} AS {dname}{CRLF}";
        return new TcpPacket(type, data);
    }
    
    public static TcpPacket build_bye()
    {
        var type = MessageType.Bye;
        var data = $"BYE{CRLF}";
        return new TcpPacket(type, data);
    }
}

public class TcpPacketParser
{
    public static TcpPacket Parse(string data)
    {
        string[] parts = data.ToUpper().Split(" ");
        switch (parts)
        {
            case ["REPLY", "OK", "IS", ..]:
                return new TcpPacket(MessageType.Reply, data);
            case ["REPLY", "NOK", "IS", ..]:
                return new TcpPacket(MessageType.NotReply, data);
            case ["MSG", "FROM", _ , "IS", ..]:
                return new TcpPacket(MessageType.Msg, data);
            case ["ERROR", "FROM", _ , "IS", ..]:
                return new TcpPacket(MessageType.Err, data);
            case ["BYE"]:
                return new TcpPacket(MessageType.Bye, "");
        }
        throw new Exception("Unknown incoming message type!");
    }
}

public class TcpPacket
{
    private MessageType _type;
    private string _data;

    public TcpPacket(MessageType type, string data)
    {
        _type = type;
        _data = data;
    }

    public void Print()
    {
        string dname;
        string message;
        string state;
        switch (_type)
        {
            case MessageType.Msg:
                string[] msg = _data.Split(" ", 5);
                dname = msg[2];
                message = msg[4].Trim();
                Console.Write($"{dname}: {message}\n");
                break;
            case MessageType.Err:
                string[] err = _data.Split(" ", 5);
                dname = err[2];
                message = err[4].Trim();
                Console.Error.Write($"{dname}: {message}\n");
                break;
            case MessageType.Reply:
                string[] reply = _data.Split(" ", 4);
                state = reply[1];
                message = reply[3].Trim();
                if (state == "OK")
                {
                    Console.Error.Write($"Success: {message}\n");
                }
                else
                {
                    Console.Error.Write($"Failure: {message}\n");
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

    public MessageType GetMsgType()
    {
        return _type;
    }
}