using System.Text;
using IpkProject1.enums;
using IpkProject1.interfaces;
using IpkProject1.user;

namespace IpkProject1.udp;

public class UdpPacket : IPacket
{
    private readonly MessageTypeEnum _msgType;
    private readonly byte[] _data;
    private readonly UInt16 _msgId;
    public MessageTypeEnum Type => _msgType;
    
    public UdpPacket(MessageTypeEnum msgType, byte[] data)
    {
        _msgType = msgType;
        _data = data;
        _msgId = BitConverter.ToUInt16(data[1..3]);
    }
    public byte[] ToBytes()
    {
        return _data;
    }
    
    public int GetMsgId()
    {
        return _msgId;
    }
    
    public void Print()
    {
        Io.DebugPrintLine("UDP PACKET: " + _msgType);
        string data;
        string dname;
        string message;
        var builder = new UdpPacketBuilder();
        switch (_msgType)
        {
            case MessageTypeEnum.Reply:
                var result = _data[3] != 0;
                message = Encoding.ASCII.GetString(_data[6..^1]);
                if (!GrammarChecker.CheckMsg(message, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid message!");
                    IpkProject1.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid message!");
                    break;
                }
                Io.ErrorPrintLine((result ? "Success" : "Failure") + ": " + message);
                break;
            case MessageTypeEnum.Msg:
                data = Encoding.ASCII.GetString(_data[3..^1]);
                dname = data.Split("\0")[0];
                if (!GrammarChecker.CheckDisplayName(dname, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid display name!");
                    IpkProject1.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid display name!");
                    break;
                }
                message = data.Split("\0")[1];
                if (!GrammarChecker.CheckMsg(message, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid message!");
                    IpkProject1.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid message!");
                    break;
                }
                Io.PrintLine(dname + ": " + message);
                break;
            case MessageTypeEnum.Err:
                data = Encoding.ASCII.GetString(_data[3..^1]);
                dname = data.Split("\0")[0];
                if (!GrammarChecker.CheckDisplayName(dname, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid display name!");
                    IpkProject1.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid display name!");
                    break;
                }
                message = data.Split("\0")[1];
                if (!GrammarChecker.CheckMsg(message, true))
                {
                    var grammarErr = builder.build_error(InputProcessor.DisplayName, "Invalid message!");
                    IpkProject1.GetClient().AddPacketToSendQueue(grammarErr);
                    Io.ErrorPrintLine("ERR: Invalid message!");
                    break;
                }
                Io.ErrorPrintLine($"ERR FROM {dname}: {message}");
                break;
        }
    }

    public bool? ReplyState()
    {
        if (_msgType == MessageTypeEnum.Reply)
        {
            return _data[3] != 0;
        }
        return null;
    }
}

public class UdpPacketBuilder : IPacketBuilder
{
    private static UInt16 _counter;
    
    private static UInt16 GetNextId()
    {
        // convert to bytes swap and convert back
        var ret = BitConverter.ToUInt16(BitConverter.GetBytes(_counter).Reverse().ToArray());
        _counter++;
        return ret;
    }
    public UdpPacket build_confirm(UInt16 msgId)
    {
        var type = MessageTypeEnum.Confirm;
        var msgTypeByte = Convert.ToByte((int)type);
        var msgIdBytes = BitConverter.GetBytes(msgId);
        var data = new byte[1 + 2];
        data[0] = msgTypeByte;
        Array.Copy(msgIdBytes, 0, data, 1, 2);
        return new UdpPacket(type, data);
    }
    public UdpPacket build_auth(string login, string dname, string secret)
    {
        var type = MessageTypeEnum.Auth;
        var msgTypeByte = Convert.ToByte((int)type);
        var msgIdBytes = BitConverter.GetBytes(GetNextId());
        var usernameBytes = Encoding.ASCII.GetBytes(login);
        var dnameBytes = Encoding.ASCII.GetBytes(dname);
        var secretBytes = Encoding.ASCII.GetBytes(secret);
        var data = new byte[1 + 2 + usernameBytes.Length + 1 + dnameBytes.Length + 1 + secretBytes.Length + 1];
        data[0] = msgTypeByte;
        Array.Copy(msgIdBytes, 0, data, 1, 2);
        Array.Copy(usernameBytes, 0, data, 3, usernameBytes.Length);
        data[3 + usernameBytes.Length] = 0;
        Array.Copy(dnameBytes, 0, data, 3 + usernameBytes.Length + 1, dnameBytes.Length);
        data[3 + usernameBytes.Length + 1 + dnameBytes.Length] = 0;
        Array.Copy(secretBytes, 0, data, 3 + usernameBytes.Length + 1 + dnameBytes.Length + 1, secretBytes.Length);
        data[3 + usernameBytes.Length + 1 + dnameBytes.Length + 1 + secretBytes.Length] = 0;
        return new UdpPacket(type, data);
}
    
    public UdpPacket build_msg(string dname, string msg)
    {
        var type = MessageTypeEnum.Msg;
        var msgTypeByte = Convert.ToByte((int)type);
        var msgIdBytes = BitConverter.GetBytes(GetNextId());
        var dnameBytes = Encoding.ASCII.GetBytes(dname);
        var msgBytes = Encoding.ASCII.GetBytes(msg);
        var data = new byte[1 + 2 + dnameBytes.Length + 1 + msgBytes.Length + 1];
        data[0] = msgTypeByte;
        Array.Copy(msgIdBytes, 0, data, 1, 2);
        Array.Copy(dnameBytes, 0, data, 3, dnameBytes.Length);
        data[3 + dnameBytes.Length] = 0;
        Array.Copy(msgBytes, 0, data, 3 + dnameBytes.Length + 1, msgBytes.Length);
        data[3 + dnameBytes.Length + 1 + msgBytes.Length] = 0;
        return new UdpPacket(type, data);
    }
    
    public UdpPacket build_error(string dname, string msg)
    {
        var type = MessageTypeEnum.Err;
        var msgTypeByte = Convert.ToByte((int)type);
        var msgIdBytes = BitConverter.GetBytes(GetNextId());
        var dnameBytes = Encoding.ASCII.GetBytes(dname);
        var msgBytes = Encoding.ASCII.GetBytes(msg);
        var data = new byte[1 + 2 + dnameBytes.Length + 1 + msgBytes.Length + 1];
        data[0] = msgTypeByte;
        Array.Copy(msgIdBytes, 0, data, 1, 2);
        Array.Copy(dnameBytes, 0, data, 3, dnameBytes.Length);
        data[3 + dnameBytes.Length] = 0;
        Array.Copy(msgBytes, 0, data, 3 + dnameBytes.Length + 1, msgBytes.Length);
        data[3 + dnameBytes.Length + 1 + msgBytes.Length] = 0;
        return new UdpPacket(type, data);
    }
    
    public UdpPacket build_join(string channel, string dname)
    {
        var type = MessageTypeEnum.Join;
        var msgTypeByte = Convert.ToByte((int)type);
        var msgIdBytes = BitConverter.GetBytes(GetNextId());
        var channelBytes = Encoding.ASCII.GetBytes(channel);
        var dnameBytes = Encoding.ASCII.GetBytes(dname);
        var data = new byte[1 + 2 + channelBytes.Length + 1 + dnameBytes.Length + 1];
        data[0] = msgTypeByte;
        Array.Copy(msgIdBytes, 0, data, 1, 2);
        Array.Copy(channelBytes, 0, data, 3, channelBytes.Length);
        data[3 + channelBytes.Length] = 0;
        Array.Copy(dnameBytes, 0, data, 3 + channelBytes.Length + 1, dnameBytes.Length);
        data[3 + channelBytes.Length + 1 + dnameBytes.Length] = 0;
        return new UdpPacket(type, data);
    }
    
    public UdpPacket build_bye()
    {
        var type = MessageTypeEnum.Bye;
        var msgTypeByte = Convert.ToByte((int)type);
        var msgIdBytes = BitConverter.GetBytes(GetNextId());
        var data = new byte[1 + 2];
        data[0] = msgTypeByte;
        Array.Copy(msgIdBytes, 0, data, 1, 2);
        return new UdpPacket(type, data);
    }
}