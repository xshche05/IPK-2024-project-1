using IpkProject1.enums;

namespace IpkProject1.interfaces;

public interface IPacket
{
    public byte[] ToBytes();
    public void Print();
    public MessageTypeEnum GetMsgType();
}