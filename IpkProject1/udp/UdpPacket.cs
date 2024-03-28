using System.Text;
using IpkProject1.enums;
using IpkProject1.interfaces;
using IpkProject1.user;

namespace IpkProject1.udp;

public class UdpPacket : IPacket
{
    private MessageTypeEnum _msgType;
    private byte[] _data;
    private UInt16 _msgId;
    
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
        if (_msgType == MessageTypeEnum.Reply)
        {
            var result = _data[3] != 0;
            var message = Encoding.ASCII.GetString(_data[6..^1]);
            Io.ErrorPrintLine((result ? "Success" : "Failure") + ": " + message);
        }
        else if (_msgType == MessageTypeEnum.Msg)
        {
            var data = Encoding.ASCII.GetString(_data[3..^1]);
            var dname = data.Split("\0")[0];
            var message = data.Split("\0")[1];
            Io.PrintLine(dname + ": " + message, ConsoleColor.Green);
        }
        else if (_msgType == MessageTypeEnum.Err)
        {
            var data = Encoding.ASCII.GetString(_data[3..^1]);
            var dname = data.Split("\0")[0];
            var message = data.Split("\0")[1];
            Io.ErrorPrintLine($"ERR FROM {dname}: {message}");
        }
    }
    
    public MessageTypeEnum GetMsgType()
    {
        return _msgType;
    }
}

public class UdpPacketBuilder : IPacketBuilder
{
    private static UInt16 _counter;
    
    private static UInt16 GetNextId()
    {
        return _counter++;
    }
    public static UdpPacket build_confirm(UInt16 msgId)
    {
        var type = MessageTypeEnum.Confirm;
        var msgTypeByte = Convert.ToByte((int)type);
        var msgIdBytes = BitConverter.GetBytes(msgId);
        var data = new byte[1 + 2];
        data[0] = msgTypeByte;
        Array.Copy(msgIdBytes, 0, data, 1, 2);
        return new UdpPacket(type, data);
    }
    public static UdpPacket build_auth(string login, string dname, string secret)
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
    
    public static UdpPacket build_msg(string dname, string msg)
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
    
    public static UdpPacket build_error(string dname, string msg)
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
    
    public static UdpPacket build_join(string channel, string dname)
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
    
    public static UdpPacket build_bye()
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