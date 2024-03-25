using System.Text;
using IpkProject1.enums;
using IpkProject1.interfaces;

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
            bool result = _data[3] != 0;
            string message = Encoding.ASCII.GetString(_data[6..^1]);
            if (result)
            {
                Console.Error.WriteLine("Success: " + message);
            }
            else
            {
                Console.Error.WriteLine("Failure: " + message);
            }
        }
        else if (_msgType == MessageTypeEnum.Msg)
        {
            string data = Encoding.ASCII.GetString(_data[3..^1]);
            string dname = data.Split("\0")[0];
            string message = data.Split("\0")[1];
            Console.Error.WriteLine(dname + ": " + message);;
        }
        else if (_msgType == MessageTypeEnum.Err)
        {
            string data = Encoding.ASCII.GetString(_data[3..^1]);
            string dname = data.Split("\0")[0];
            string message = data.Split("\0")[1];
            Console.Error.WriteLine(dname + ": " + message);
        }
    }
    
    public MessageTypeEnum GetMsgType()
    {
        return _msgType;
    }
}

public class UdpPacketBuilder : IPacketBuilder
{
    public static UdpPacket build_confirm(UInt16 msg_id)
    {
        MessageTypeEnum type = MessageTypeEnum.Confirm;
        byte msg_type_byte = Convert.ToByte((int)type);
        byte[] msg_id_bytes = BitConverter.GetBytes(msg_id);
        byte[] data = new byte[1 + 2];
        data[0] = msg_type_byte;
        Array.Copy(msg_id_bytes, 0, data, 1, 2);
        return new UdpPacket(type, data);
    }
    public static UdpPacket build_auth(string login, string dname, string secret)
    {
        MessageTypeEnum type = MessageTypeEnum.Auth;
        byte msg_type_byte = Convert.ToByte((int)type);
        byte[] msg_id_bytes = BitConverter.GetBytes((UInt16)((UdpChatClient)IpkProject1.GetClient()).GetId());
        byte[] username_bytes = Encoding.ASCII.GetBytes(login);
        byte[] dname_bytes = Encoding.ASCII.GetBytes(dname);
        byte[] secret_bytes = Encoding.ASCII.GetBytes(secret);
        byte[] data = new byte[1 + 2 + username_bytes.Length + 1 + dname_bytes.Length + 1 + secret_bytes.Length + 1];
        data[0] = msg_type_byte;
        Array.Copy(msg_id_bytes, 0, data, 1, 2);
        Array.Copy(username_bytes, 0, data, 3, username_bytes.Length);
        data[3 + username_bytes.Length] = 0;
        Array.Copy(dname_bytes, 0, data, 3 + username_bytes.Length + 1, dname_bytes.Length);
        data[3 + username_bytes.Length + 1 + dname_bytes.Length] = 0;
        Array.Copy(secret_bytes, 0, data, 3 + username_bytes.Length + 1 + dname_bytes.Length + 1, secret_bytes.Length);
        data[3 + username_bytes.Length + 1 + dname_bytes.Length + 1 + secret_bytes.Length] = 0;
        return new UdpPacket(type, data);
}
    
    public static UdpPacket build_msg(string dname, string msg)
    {
        MessageTypeEnum type = MessageTypeEnum.Msg;
        byte msg_type_byte = Convert.ToByte((int)type);
        byte[] msg_id_bytes = BitConverter.GetBytes((UInt16)((UdpChatClient)IpkProject1.GetClient()).GetId());
        byte[] dname_bytes = Encoding.ASCII.GetBytes(dname);
        byte[] msg_bytes = Encoding.ASCII.GetBytes(msg);
        byte[] data = new byte[1 + 2 + dname_bytes.Length + 1 + msg_bytes.Length + 1];
        data[0] = msg_type_byte;
        Array.Copy(msg_id_bytes, 0, data, 1, 2);
        Array.Copy(dname_bytes, 0, data, 3, dname_bytes.Length);
        data[3 + dname_bytes.Length] = 0;
        Array.Copy(msg_bytes, 0, data, 3 + dname_bytes.Length + 1, msg_bytes.Length);
        data[3 + dname_bytes.Length + 1 + msg_bytes.Length] = 0;
        return new UdpPacket(type, data);
    }
    
    public static UdpPacket build_error(string dname, string msg)
    {
        throw new NotImplementedException();
    }
    
    public static UdpPacket build_join(string channel, string dname)
    {
        throw new NotImplementedException();
    }
    
    public static UdpPacket build_bye()
    {
        MessageTypeEnum type = MessageTypeEnum.Bye;
        byte msg_type_byte = Convert.ToByte((int)type);
        byte[] msg_id_bytes = BitConverter.GetBytes((UInt16)((UdpChatClient)IpkProject1.GetClient()).GetId());
        byte[] data = new byte[1 + 2];
        data[0] = msg_type_byte;
        Array.Copy(msg_id_bytes, 0, data, 1, 2);
        return new UdpPacket(type, data);
    }
}